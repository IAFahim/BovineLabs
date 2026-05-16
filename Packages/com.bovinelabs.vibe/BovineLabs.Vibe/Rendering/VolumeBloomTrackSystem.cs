// <copyright file="VolumeBloomTrackSystem.cs" company="BovineLabs">
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
    /// Blends volume bloom clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeBloomTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeBloom, VolumeBloomInitial> lifeImpl;
        private TrackBlendImpl<VolumeBloomBlend, VolumeBloomAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeBloomClipData>();
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
                .WithAllRW<VolumeBloomAnimated>()
                .WithAll<VolumeBloomClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeBloomAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeBloomClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeBloomInitial>(true),
                Blooms = SystemAPI.GetComponentLookup<VolumeBloom>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Blooms = SystemAPI.GetComponentLookup<VolumeBloom>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeBloomBlend CreateBlend(in VolumeBloom data, bool useOverrides)
        {
            return new VolumeBloomBlend
            {
                Threshold = data.Threshold,
                Intensity = data.Intensity,
                Scatter = data.Scatter,
                Clamp = data.Clamp,
                DirtIntensity = data.DirtIntensity,
                Tint = new float4(data.Tint.r, data.Tint.g, data.Tint.b, data.Tint.a),
                ThresholdOverride = useOverrides && data.ThresholdOverride,
                IntensityOverride = useOverrides && data.IntensityOverride,
                ScatterOverride = useOverrides && data.ScatterOverride,
                ClampOverride = useOverrides && data.ClampOverride,
                DirtIntensityOverride = useOverrides && data.DirtIntensityOverride,
                TintOverride = useOverrides && data.TintOverride,
            };
        }

        private static VolumeBloomBlend CreateBlend(in VolumeBloomConstantData data)
        {
            return new VolumeBloomBlend
            {
                Threshold = data.Threshold,
                Intensity = data.Intensity,
                Scatter = data.Scatter,
                Clamp = data.Clamp,
                DirtIntensity = data.DirtIntensity,
                Tint = data.Tint,
                ThresholdOverride = data.ThresholdOverride,
                IntensityOverride = data.IntensityOverride,
                ScatterOverride = data.ScatterOverride,
                ClampOverride = data.ClampOverride,
                DirtIntensityOverride = data.DirtIntensityOverride,
                TintOverride = data.TintOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeBloomAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeBloomClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeBloomInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeBloom> Blooms;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeBloomAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeBloomClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeBloomClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitialOverrides(binding.Value, in initial, ref this.Blooms);
                            break;
                        case VolumeBloomClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyConstantOverrides(binding.Value, in clipBlob.Constant, clipData.DirtTexture, ref this.Blooms);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyConstantOverrides(
                Entity entity, in VolumeBloomConstantData data, UnityObjectRef<Texture> dirtTexture, ref ComponentLookup<VolumeBloom> blooms)
            {
                if (!blooms.TryGetRefRW(entity, out var bloom))
                {
                    return;
                }

                ref var value = ref bloom.ValueRW;
                value.Active = data.Active;

                if (data.HighQualityFilteringOverride)
                {
                    value.HighQualityFilteringOverride = true;
                    value.HighQualityFiltering = data.HighQualityFiltering;
                }

                if (data.FilterOverride)
                {
                    value.FilterOverride = true;
                    value.Filter = data.Filter;
                }

                if (data.DownscaleOverride)
                {
                    value.DownscaleOverride = true;
                    value.Downscale = data.Downscale;
                }

                if (data.MaxIterationsOverride)
                {
                    value.MaxIterationsOverride = true;
                    value.MaxIterations = data.MaxIterations;
                }

                if (data.DirtTextureOverride)
                {
                    value.DirtTextureOverride = true;
                    value.DirtTexture = dirtTexture;
                }
            }

            private static void ApplyInitialOverrides(Entity entity, in VolumeBloom initial, ref ComponentLookup<VolumeBloom> blooms)
            {
                if (!blooms.TryGetRefRW(entity, out var bloom))
                {
                    return;
                }

                ref var value = ref bloom.ValueRW;
                value.Threshold = initial.Threshold;
                value.ThresholdOverride = initial.ThresholdOverride;
                value.Intensity = initial.Intensity;
                value.IntensityOverride = initial.IntensityOverride;
                value.Scatter = initial.Scatter;
                value.ScatterOverride = initial.ScatterOverride;
                value.Clamp = initial.Clamp;
                value.ClampOverride = initial.ClampOverride;
                value.Tint = initial.Tint;
                value.TintOverride = initial.TintOverride;
                value.DirtIntensity = initial.DirtIntensity;
                value.DirtIntensityOverride = initial.DirtIntensityOverride;
                value.Active = initial.Active;
                value.HighQualityFiltering = initial.HighQualityFiltering;
                value.HighQualityFilteringOverride = initial.HighQualityFilteringOverride;
                value.Filter = initial.Filter;
                value.FilterOverride = initial.FilterOverride;
                value.Downscale = initial.Downscale;
                value.DownscaleOverride = initial.DownscaleOverride;
                value.MaxIterations = initial.MaxIterations;
                value.MaxIterationsOverride = initial.MaxIterationsOverride;
                value.DirtTexture = initial.DirtTexture;
                value.DirtTextureOverride = initial.DirtTextureOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeBloomBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeBloom> Blooms;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Blooms.TryGetRefRW(entity, out var bloom))
                {
                    return;
                }

                ref var value = ref bloom.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeBloomMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeBloom bloom, in VolumeBloomBlend blend)
            {
                if (blend.ThresholdOverride)
                {
                    bloom.ThresholdOverride = true;
                    bloom.Threshold = blend.Threshold;
                }

                if (blend.IntensityOverride)
                {
                    bloom.IntensityOverride = true;
                    bloom.Intensity = blend.Intensity;
                }

                if (blend.ScatterOverride)
                {
                    bloom.ScatterOverride = true;
                    bloom.Scatter = blend.Scatter;
                }

                if (blend.ClampOverride)
                {
                    bloom.ClampOverride = true;
                    bloom.Clamp = blend.Clamp;
                }

                if (blend.TintOverride)
                {
                    bloom.TintOverride = true;
                    bloom.Tint = new Color(blend.Tint.x, blend.Tint.y, blend.Tint.z, blend.Tint.w);
                }

                if (blend.DirtIntensityOverride)
                {
                    bloom.DirtIntensityOverride = true;
                    bloom.DirtIntensity = blend.DirtIntensity;
                }
            }
        }

        private struct VolumeBloomMixer : IMixer<VolumeBloomBlend>
        {
            public VolumeBloomBlend Lerp(in VolumeBloomBlend a, in VolumeBloomBlend b, in float s)
            {
                return new VolumeBloomBlend
                {
                    Threshold = MixUtil.LerpFloat(a.Threshold, b.Threshold, s, a.ThresholdOverride, b.ThresholdOverride),
                    Intensity = MixUtil.LerpFloat(a.Intensity, b.Intensity, s, a.IntensityOverride, b.IntensityOverride),
                    Scatter = MixUtil.LerpFloat(a.Scatter, b.Scatter, s, a.ScatterOverride, b.ScatterOverride),
                    Clamp = MixUtil.LerpFloat(a.Clamp, b.Clamp, s, a.ClampOverride, b.ClampOverride),
                    DirtIntensity = MixUtil.LerpFloat(a.DirtIntensity, b.DirtIntensity, s, a.DirtIntensityOverride, b.DirtIntensityOverride),
                    Tint = MixUtil.LerpFloat4(a.Tint, b.Tint, s, a.TintOverride, b.TintOverride),
                    ThresholdOverride = a.ThresholdOverride || b.ThresholdOverride,
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                    ScatterOverride = a.ScatterOverride || b.ScatterOverride,
                    ClampOverride = a.ClampOverride || b.ClampOverride,
                    DirtIntensityOverride = a.DirtIntensityOverride || b.DirtIntensityOverride,
                    TintOverride = a.TintOverride || b.TintOverride,
                };
            }

            public VolumeBloomBlend Add(in VolumeBloomBlend a, in VolumeBloomBlend b)
            {
                return new VolumeBloomBlend
                {
                    Threshold = MixUtil.AddFloat(a.Threshold, b.Threshold, a.ThresholdOverride, b.ThresholdOverride),
                    Intensity = MixUtil.AddFloat(a.Intensity, b.Intensity, a.IntensityOverride, b.IntensityOverride),
                    Scatter = MixUtil.AddFloat(a.Scatter, b.Scatter, a.ScatterOverride, b.ScatterOverride),
                    Clamp = MixUtil.AddFloat(a.Clamp, b.Clamp, a.ClampOverride, b.ClampOverride),
                    DirtIntensity = MixUtil.AddFloat(a.DirtIntensity, b.DirtIntensity, a.DirtIntensityOverride, b.DirtIntensityOverride),
                    Tint = MixUtil.AddFloat4(a.Tint, b.Tint, a.TintOverride, b.TintOverride),
                    ThresholdOverride = a.ThresholdOverride || b.ThresholdOverride,
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                    ScatterOverride = a.ScatterOverride || b.ScatterOverride,
                    ClampOverride = a.ClampOverride || b.ClampOverride,
                    DirtIntensityOverride = a.DirtIntensityOverride || b.DirtIntensityOverride,
                    TintOverride = a.TintOverride || b.TintOverride,
                };
            }
        }
    }
}
#endif
