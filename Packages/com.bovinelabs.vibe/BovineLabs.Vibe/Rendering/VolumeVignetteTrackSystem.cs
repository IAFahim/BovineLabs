// <copyright file="VolumeVignetteTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Volume;
    using BovineLabs.Core.Jobs;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Volume;
    using BovineLabs.Vibe.Jobs;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Blends vignette clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeVignetteTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeVignette, VolumeVignetteInitial> lifeImpl;
        private TrackBlendImpl<VolumeVignetteBlend, VolumeVignetteAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeVignetteClipData>();
            this.lifeImpl.OnCreate(ref state);
            this.impl.OnCreate(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            this.impl.OnDestroy(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.lifeImpl.OnUpdate(ref state);

            var clipActivateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<VolumeVignetteAnimated>()
                .WithAll<VolumeVignetteClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeVignetteAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeVignetteClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeVignetteInitial>(true),
                Vignettes = SystemAPI.GetComponentLookup<VolumeVignette>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Vignettes = SystemAPI.GetComponentLookup<VolumeVignette>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeVignetteBlend CreateBlend(in VolumeVignette data, bool useOverrides)
        {
            return new VolumeVignetteBlend
            {
                Color = new float4(data.Color.r, data.Color.g, data.Color.b, data.Color.a),
                Center = new float2(data.Center.x, data.Center.y),
                Intensity = data.Intensity,
                Smoothness = data.Smoothness,
                ColorOverride = useOverrides && data.ColorOverride,
                CenterOverride = useOverrides && data.CenterOverride,
                IntensityOverride = useOverrides && data.IntensityOverride,
                SmoothnessOverride = useOverrides && data.SmoothnessOverride,
            };
        }

        private static VolumeVignetteBlend CreateBlend(in VolumeVignetteConstantData data)
        {
            return new VolumeVignetteBlend
            {
                Color = data.Color,
                Center = data.Center,
                Intensity = data.Intensity,
                Smoothness = data.Smoothness,
                ColorOverride = data.ColorOverride,
                CenterOverride = data.CenterOverride,
                IntensityOverride = data.IntensityOverride,
                SmoothnessOverride = data.SmoothnessOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeVignetteAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeVignetteClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeVignetteInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeVignette> Vignettes;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeVignetteAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeVignetteClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var bindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk];
                    ref var clipBlob = ref clipData.Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    ref readonly var binding = ref bindings[entityIndexInChunk];

                    var initial = this.Initials.TryGetComponent(clip.Track, out var initialData)
                        ? initialData.Value
                        : default;

                    switch (clipBlob.Type)
                    {
                        case VolumeVignetteClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Vignettes);
                            break;
                        case VolumeVignetteClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyConstantOverrides(binding.Value, in clipBlob.Constant, ref this.Vignettes);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyConstantOverrides(Entity entity, in VolumeVignetteConstantData data, ref ComponentLookup<VolumeVignette> vignettes)
            {
                if (!vignettes.TryGetRefRW(entity, out var vignette))
                {
                    return;
                }

                ref var value = ref vignette.ValueRW;
                value.Active = data.Active;

                if (data.RoundedOverride)
                {
                    value.RoundedOverride = true;
                    value.Rounded = data.Rounded;
                }
            }

            private static void ApplyInitial(Entity entity, in VolumeVignette initial, ref ComponentLookup<VolumeVignette> vignettes)
            {
                if (!vignettes.TryGetRefRW(entity, out var vignette))
                {
                    return;
                }

                ref var value = ref vignette.ValueRW;
                value.Color = initial.Color;
                value.Center = initial.Center;
                value.Intensity = initial.Intensity;
                value.Smoothness = initial.Smoothness;
                value.Rounded = initial.Rounded;
                value.Active = initial.Active;
                value.ColorOverride = initial.ColorOverride;
                value.CenterOverride = initial.CenterOverride;
                value.IntensityOverride = initial.IntensityOverride;
                value.SmoothnessOverride = initial.SmoothnessOverride;
                value.RoundedOverride = initial.RoundedOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeVignetteBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeVignette> Vignettes;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Vignettes.TryGetRefRW(entity, out var vignette))
                {
                    return;
                }

                ref var value = ref vignette.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeVignetteMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeVignette vignette, in VolumeVignetteBlend blend)
            {
                if (blend.ColorOverride)
                {
                    vignette.ColorOverride = true;
                    vignette.Color = new Color(blend.Color.x, blend.Color.y, blend.Color.z, blend.Color.w);
                }

                if (blend.CenterOverride)
                {
                    vignette.CenterOverride = true;
                    vignette.Center = new Vector2(blend.Center.x, blend.Center.y);
                }

                if (blend.IntensityOverride)
                {
                    vignette.IntensityOverride = true;
                    vignette.Intensity = blend.Intensity;
                }

                if (blend.SmoothnessOverride)
                {
                    vignette.SmoothnessOverride = true;
                    vignette.Smoothness = blend.Smoothness;
                }
            }
        }

        private struct VolumeVignetteMixer : IMixer<VolumeVignetteBlend>
        {
            public VolumeVignetteBlend Lerp(in VolumeVignetteBlend a, in VolumeVignetteBlend b, in float s)
            {
                return new VolumeVignetteBlend
                {
                    Color = MixUtil.LerpFloat4(a.Color, b.Color, s, a.ColorOverride, b.ColorOverride),
                    Center = MixUtil.LerpFloat2(a.Center, b.Center, s, a.CenterOverride, b.CenterOverride),
                    Intensity = MixUtil.LerpFloat(a.Intensity, b.Intensity, s, a.IntensityOverride, b.IntensityOverride),
                    Smoothness = MixUtil.LerpFloat(a.Smoothness, b.Smoothness, s, a.SmoothnessOverride, b.SmoothnessOverride),
                    ColorOverride = a.ColorOverride || b.ColorOverride,
                    CenterOverride = a.CenterOverride || b.CenterOverride,
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                    SmoothnessOverride = a.SmoothnessOverride || b.SmoothnessOverride,
                };
            }

            public VolumeVignetteBlend Add(in VolumeVignetteBlend a, in VolumeVignetteBlend b)
            {
                return new VolumeVignetteBlend
                {
                    Color = MixUtil.AddFloat4(a.Color, b.Color, a.ColorOverride, b.ColorOverride),
                    Center = MixUtil.AddFloat2(a.Center, b.Center, a.CenterOverride, b.CenterOverride),
                    Intensity = MixUtil.AddFloat(a.Intensity, b.Intensity, a.IntensityOverride, b.IntensityOverride),
                    Smoothness = MixUtil.AddFloat(a.Smoothness, b.Smoothness, a.SmoothnessOverride, b.SmoothnessOverride),
                    ColorOverride = a.ColorOverride || b.ColorOverride,
                    CenterOverride = a.CenterOverride || b.CenterOverride,
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                    SmoothnessOverride = a.SmoothnessOverride || b.SmoothnessOverride,
                };
            }
        }
    }
}
#endif
