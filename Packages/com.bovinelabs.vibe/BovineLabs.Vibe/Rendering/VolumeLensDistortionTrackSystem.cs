// <copyright file="VolumeLensDistortionTrackSystem.cs" company="BovineLabs">
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
    using UnityEngine;

    /// <summary>
    /// Blends lens distortion clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeLensDistortionTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeLensDistortion, VolumeLensDistortionInitial> lifeImpl;
        private TrackBlendImpl<VolumeLensDistortionBlend, VolumeLensDistortionAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeLensDistortionClipData>();
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
                .WithAllRW<VolumeLensDistortionAnimated>()
                .WithAll<VolumeLensDistortionClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeLensDistortionAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeLensDistortionClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeLensDistortionInitial>(true),
                Distortions = SystemAPI.GetComponentLookup<VolumeLensDistortion>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Distortions = SystemAPI.GetComponentLookup<VolumeLensDistortion>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeLensDistortionBlend CreateBlend(in VolumeLensDistortion data, bool useOverrides)
        {
            return new VolumeLensDistortionBlend
            {
                Intensity = data.Intensity,
                XMultiplier = data.XMultiplier,
                YMultiplier = data.YMultiplier,
                Center = new Unity.Mathematics.float2(data.Center.x, data.Center.y),
                Scale = data.Scale,
                IntensityOverride = useOverrides && data.IntensityOverride,
                XMultiplierOverride = useOverrides && data.XMultiplierOverride,
                YMultiplierOverride = useOverrides && data.YMultiplierOverride,
                CenterOverride = useOverrides && data.CenterOverride,
                ScaleOverride = useOverrides && data.ScaleOverride,
            };
        }

        private static VolumeLensDistortionBlend CreateBlend(in VolumeLensDistortionConstantData data)
        {
            return new VolumeLensDistortionBlend
            {
                Intensity = data.Intensity,
                XMultiplier = data.XMultiplier,
                YMultiplier = data.YMultiplier,
                Center = data.Center,
                Scale = data.Scale,
                IntensityOverride = data.IntensityOverride,
                XMultiplierOverride = data.XMultiplierOverride,
                YMultiplierOverride = data.YMultiplierOverride,
                CenterOverride = data.CenterOverride,
                ScaleOverride = data.ScaleOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeLensDistortionAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeLensDistortionClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeLensDistortionInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeLensDistortion> Distortions;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeLensDistortionAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeLensDistortionClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeLensDistortionClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Distortions);
                            break;
                        case VolumeLensDistortionClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyActive(binding.Value, clipBlob.Constant.Active, ref this.Distortions);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyActive(Entity entity, bool active, ref ComponentLookup<VolumeLensDistortion> distortions)
            {
                if (!distortions.TryGetRefRW(entity, out var distortion))
                {
                    return;
                }

                distortion.ValueRW.Active = active;
            }

            private static void ApplyInitial(Entity entity, in VolumeLensDistortion initial, ref ComponentLookup<VolumeLensDistortion> distortions)
            {
                if (!distortions.TryGetRefRW(entity, out var distortion))
                {
                    return;
                }

                ref var value = ref distortion.ValueRW;
                value.Intensity = initial.Intensity;
                value.XMultiplier = initial.XMultiplier;
                value.YMultiplier = initial.YMultiplier;
                value.Center = initial.Center;
                value.Scale = initial.Scale;
                value.Active = initial.Active;
                value.IntensityOverride = initial.IntensityOverride;
                value.XMultiplierOverride = initial.XMultiplierOverride;
                value.YMultiplierOverride = initial.YMultiplierOverride;
                value.CenterOverride = initial.CenterOverride;
                value.ScaleOverride = initial.ScaleOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeLensDistortionBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeLensDistortion> Distortions;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Distortions.TryGetRefRW(entity, out var distortion))
                {
                    return;
                }

                ref var value = ref distortion.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeLensDistortionMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeLensDistortion distortion, in VolumeLensDistortionBlend blend)
            {
                if (blend.IntensityOverride)
                {
                    distortion.IntensityOverride = true;
                    distortion.Intensity = blend.Intensity;
                }

                if (blend.XMultiplierOverride)
                {
                    distortion.XMultiplierOverride = true;
                    distortion.XMultiplier = blend.XMultiplier;
                }

                if (blend.YMultiplierOverride)
                {
                    distortion.YMultiplierOverride = true;
                    distortion.YMultiplier = blend.YMultiplier;
                }

                if (blend.CenterOverride)
                {
                    distortion.CenterOverride = true;
                    distortion.Center = new Vector2(blend.Center.x, blend.Center.y);
                }

                if (blend.ScaleOverride)
                {
                    distortion.ScaleOverride = true;
                    distortion.Scale = blend.Scale;
                }
            }
        }

        private struct VolumeLensDistortionMixer : IMixer<VolumeLensDistortionBlend>
        {
            public VolumeLensDistortionBlend Lerp(in VolumeLensDistortionBlend a, in VolumeLensDistortionBlend b, in float s)
            {
                return new VolumeLensDistortionBlend
                {
                    Intensity = MixUtil.LerpFloat(a.Intensity, b.Intensity, s, a.IntensityOverride, b.IntensityOverride),
                    XMultiplier = MixUtil.LerpFloat(a.XMultiplier, b.XMultiplier, s, a.XMultiplierOverride, b.XMultiplierOverride),
                    YMultiplier = MixUtil.LerpFloat(a.YMultiplier, b.YMultiplier, s, a.YMultiplierOverride, b.YMultiplierOverride),
                    Center = MixUtil.LerpFloat2(a.Center, b.Center, s, a.CenterOverride, b.CenterOverride),
                    Scale = MixUtil.LerpFloat(a.Scale, b.Scale, s, a.ScaleOverride, b.ScaleOverride),
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                    XMultiplierOverride = a.XMultiplierOverride || b.XMultiplierOverride,
                    YMultiplierOverride = a.YMultiplierOverride || b.YMultiplierOverride,
                    CenterOverride = a.CenterOverride || b.CenterOverride,
                    ScaleOverride = a.ScaleOverride || b.ScaleOverride,
                };
            }

            public VolumeLensDistortionBlend Add(in VolumeLensDistortionBlend a, in VolumeLensDistortionBlend b)
            {
                return new VolumeLensDistortionBlend
                {
                    Intensity = MixUtil.AddFloat(a.Intensity, b.Intensity, a.IntensityOverride, b.IntensityOverride),
                    XMultiplier = MixUtil.AddFloat(a.XMultiplier, b.XMultiplier, a.XMultiplierOverride, b.XMultiplierOverride),
                    YMultiplier = MixUtil.AddFloat(a.YMultiplier, b.YMultiplier, a.YMultiplierOverride, b.YMultiplierOverride),
                    Center = MixUtil.AddFloat2(a.Center, b.Center, a.CenterOverride, b.CenterOverride),
                    Scale = MixUtil.AddFloat(a.Scale, b.Scale, a.ScaleOverride, b.ScaleOverride),
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                    XMultiplierOverride = a.XMultiplierOverride || b.XMultiplierOverride,
                    YMultiplierOverride = a.YMultiplierOverride || b.YMultiplierOverride,
                    CenterOverride = a.CenterOverride || b.CenterOverride,
                    ScaleOverride = a.ScaleOverride || b.ScaleOverride,
                };
            }
        }
    }
}
#endif
