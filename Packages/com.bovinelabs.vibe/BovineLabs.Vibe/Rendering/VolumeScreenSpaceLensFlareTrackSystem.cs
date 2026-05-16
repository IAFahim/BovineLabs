// <copyright file="VolumeScreenSpaceLensFlareTrackSystem.cs" company="BovineLabs">
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
    /// Blends screen space lens flare clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeScreenSpaceLensFlareTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeScreenSpaceLensFlare, VolumeScreenSpaceLensFlareInitial> lifeImpl;
        private TrackBlendImpl<VolumeScreenSpaceLensFlareBlend, VolumeScreenSpaceLensFlareAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeScreenSpaceLensFlareClipData>();
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
                .WithAllRW<VolumeScreenSpaceLensFlareAnimated>()
                .WithAll<VolumeScreenSpaceLensFlareClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeScreenSpaceLensFlareAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeScreenSpaceLensFlareClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeScreenSpaceLensFlareInitial>(true),
                Flares = SystemAPI.GetComponentLookup<VolumeScreenSpaceLensFlare>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Flares = SystemAPI.GetComponentLookup<VolumeScreenSpaceLensFlare>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeScreenSpaceLensFlareBlend CreateBlend(in VolumeScreenSpaceLensFlare data, bool useOverrides)
        {
            return new VolumeScreenSpaceLensFlareBlend
            {
                Intensity = data.Intensity,
                TintColor = new float4(data.TintColor.r, data.TintColor.g, data.TintColor.b, data.TintColor.a),
                FirstFlareIntensity = data.FirstFlareIntensity,
                SecondaryFlareIntensity = data.SecondaryFlareIntensity,
                WarpedFlareIntensity = data.WarpedFlareIntensity,
                WarpedFlareScale = new float2(data.WarpedFlareScale.x, data.WarpedFlareScale.y),
                SampleDimmer = data.SampleDimmer,
                VignetteEffect = data.VignetteEffect,
                StartingPosition = data.StartingPosition,
                Scale = data.Scale,
                StreaksIntensity = data.StreaksIntensity,
                StreaksLength = data.StreaksLength,
                StreaksOrientation = data.StreaksOrientation,
                StreaksThreshold = data.StreaksThreshold,
                ChromaticAbberationIntensity = data.ChromaticAbberationIntensity,
                IntensityOverride = useOverrides && data.IntensityOverride,
                TintColorOverride = useOverrides && data.TintColorOverride,
                FirstFlareIntensityOverride = useOverrides && data.FirstFlareIntensityOverride,
                SecondaryFlareIntensityOverride = useOverrides && data.SecondaryFlareIntensityOverride,
                WarpedFlareIntensityOverride = useOverrides && data.WarpedFlareIntensityOverride,
                WarpedFlareScaleOverride = useOverrides && data.WarpedFlareScaleOverride,
                SampleDimmerOverride = useOverrides && data.SampleDimmerOverride,
                VignetteEffectOverride = useOverrides && data.VignetteEffectOverride,
                StartingPositionOverride = useOverrides && data.StartingPositionOverride,
                ScaleOverride = useOverrides && data.ScaleOverride,
                StreaksIntensityOverride = useOverrides && data.StreaksIntensityOverride,
                StreaksLengthOverride = useOverrides && data.StreaksLengthOverride,
                StreaksOrientationOverride = useOverrides && data.StreaksOrientationOverride,
                StreaksThresholdOverride = useOverrides && data.StreaksThresholdOverride,
                ChromaticAbberationIntensityOverride = useOverrides && data.ChromaticAbberationIntensityOverride,
            };
        }

        private static VolumeScreenSpaceLensFlareBlend CreateBlend(in VolumeScreenSpaceLensFlareConstantData data)
        {
            return new VolumeScreenSpaceLensFlareBlend
            {
                Intensity = data.Intensity,
                TintColor = data.TintColor,
                FirstFlareIntensity = data.FirstFlareIntensity,
                SecondaryFlareIntensity = data.SecondaryFlareIntensity,
                WarpedFlareIntensity = data.WarpedFlareIntensity,
                WarpedFlareScale = data.WarpedFlareScale,
                SampleDimmer = data.SampleDimmer,
                VignetteEffect = data.VignetteEffect,
                StartingPosition = data.StartingPosition,
                Scale = data.Scale,
                StreaksIntensity = data.StreaksIntensity,
                StreaksLength = data.StreaksLength,
                StreaksOrientation = data.StreaksOrientation,
                StreaksThreshold = data.StreaksThreshold,
                ChromaticAbberationIntensity = data.ChromaticAbberationIntensity,
                IntensityOverride = data.IntensityOverride,
                TintColorOverride = data.TintColorOverride,
                FirstFlareIntensityOverride = data.FirstFlareIntensityOverride,
                SecondaryFlareIntensityOverride = data.SecondaryFlareIntensityOverride,
                WarpedFlareIntensityOverride = data.WarpedFlareIntensityOverride,
                WarpedFlareScaleOverride = data.WarpedFlareScaleOverride,
                SampleDimmerOverride = data.SampleDimmerOverride,
                VignetteEffectOverride = data.VignetteEffectOverride,
                StartingPositionOverride = data.StartingPositionOverride,
                ScaleOverride = data.ScaleOverride,
                StreaksIntensityOverride = data.StreaksIntensityOverride,
                StreaksLengthOverride = data.StreaksLengthOverride,
                StreaksOrientationOverride = data.StreaksOrientationOverride,
                StreaksThresholdOverride = data.StreaksThresholdOverride,
                ChromaticAbberationIntensityOverride = data.ChromaticAbberationIntensityOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeScreenSpaceLensFlareAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeScreenSpaceLensFlareClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeScreenSpaceLensFlareInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeScreenSpaceLensFlare> Flares;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeScreenSpaceLensFlareAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeScreenSpaceLensFlareClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeScreenSpaceLensFlareClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Flares);
                            break;
                        case VolumeScreenSpaceLensFlareClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyConstantOverrides(binding.Value, in clipBlob.Constant, ref this.Flares);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyConstantOverrides(
                Entity entity, in VolumeScreenSpaceLensFlareConstantData data, ref ComponentLookup<VolumeScreenSpaceLensFlare> flares)
            {
                if (!flares.TryGetRefRW(entity, out var flare))
                {
                    return;
                }

                ref var value = ref flare.ValueRW;
                value.Active = data.Active;

                if (data.BloomMipOverride)
                {
                    value.BloomMipOverride = true;
                    value.BloomMip = data.BloomMip;
                }

                if (data.SamplesOverride)
                {
                    value.SamplesOverride = true;
                    value.Samples = data.Samples;
                }

                if (data.ResolutionOverride)
                {
                    value.ResolutionOverride = true;
                    value.Resolution = data.Resolution;
                }
            }

            private static void ApplyInitial(
                Entity entity, in VolumeScreenSpaceLensFlare initial, ref ComponentLookup<VolumeScreenSpaceLensFlare> flares)
            {
                if (!flares.TryGetRefRW(entity, out var flare))
                {
                    return;
                }

                ref var value = ref flare.ValueRW;
                value.Intensity = initial.Intensity;
                value.TintColor = initial.TintColor;
                value.BloomMip = initial.BloomMip;
                value.FirstFlareIntensity = initial.FirstFlareIntensity;
                value.SecondaryFlareIntensity = initial.SecondaryFlareIntensity;
                value.WarpedFlareIntensity = initial.WarpedFlareIntensity;
                value.WarpedFlareScale = initial.WarpedFlareScale;
                value.Samples = initial.Samples;
                value.SampleDimmer = initial.SampleDimmer;
                value.VignetteEffect = initial.VignetteEffect;
                value.StartingPosition = initial.StartingPosition;
                value.Scale = initial.Scale;
                value.StreaksIntensity = initial.StreaksIntensity;
                value.StreaksLength = initial.StreaksLength;
                value.StreaksOrientation = initial.StreaksOrientation;
                value.StreaksThreshold = initial.StreaksThreshold;
                value.Resolution = initial.Resolution;
                value.ChromaticAbberationIntensity = initial.ChromaticAbberationIntensity;
                value.Active = initial.Active;
                value.IntensityOverride = initial.IntensityOverride;
                value.TintColorOverride = initial.TintColorOverride;
                value.BloomMipOverride = initial.BloomMipOverride;
                value.FirstFlareIntensityOverride = initial.FirstFlareIntensityOverride;
                value.SecondaryFlareIntensityOverride = initial.SecondaryFlareIntensityOverride;
                value.WarpedFlareIntensityOverride = initial.WarpedFlareIntensityOverride;
                value.WarpedFlareScaleOverride = initial.WarpedFlareScaleOverride;
                value.SamplesOverride = initial.SamplesOverride;
                value.SampleDimmerOverride = initial.SampleDimmerOverride;
                value.VignetteEffectOverride = initial.VignetteEffectOverride;
                value.StartingPositionOverride = initial.StartingPositionOverride;
                value.ScaleOverride = initial.ScaleOverride;
                value.StreaksIntensityOverride = initial.StreaksIntensityOverride;
                value.StreaksLengthOverride = initial.StreaksLengthOverride;
                value.StreaksOrientationOverride = initial.StreaksOrientationOverride;
                value.StreaksThresholdOverride = initial.StreaksThresholdOverride;
                value.ResolutionOverride = initial.ResolutionOverride;
                value.ChromaticAbberationIntensityOverride = initial.ChromaticAbberationIntensityOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeScreenSpaceLensFlareBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeScreenSpaceLensFlare> Flares;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Flares.TryGetRefRW(entity, out var flare))
                {
                    return;
                }

                ref var value = ref flare.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeScreenSpaceLensFlareMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeScreenSpaceLensFlare flare, in VolumeScreenSpaceLensFlareBlend blend)
            {
                if (blend.IntensityOverride)
                {
                    flare.IntensityOverride = true;
                    flare.Intensity = blend.Intensity;
                }

                if (blend.TintColorOverride)
                {
                    flare.TintColorOverride = true;
                    flare.TintColor = new Color(blend.TintColor.x, blend.TintColor.y, blend.TintColor.z, blend.TintColor.w);
                }

                if (blend.FirstFlareIntensityOverride)
                {
                    flare.FirstFlareIntensityOverride = true;
                    flare.FirstFlareIntensity = blend.FirstFlareIntensity;
                }

                if (blend.SecondaryFlareIntensityOverride)
                {
                    flare.SecondaryFlareIntensityOverride = true;
                    flare.SecondaryFlareIntensity = blend.SecondaryFlareIntensity;
                }

                if (blend.WarpedFlareIntensityOverride)
                {
                    flare.WarpedFlareIntensityOverride = true;
                    flare.WarpedFlareIntensity = blend.WarpedFlareIntensity;
                }

                if (blend.WarpedFlareScaleOverride)
                {
                    flare.WarpedFlareScaleOverride = true;
                    flare.WarpedFlareScale = new Vector2(blend.WarpedFlareScale.x, blend.WarpedFlareScale.y);
                }

                if (blend.SampleDimmerOverride)
                {
                    flare.SampleDimmerOverride = true;
                    flare.SampleDimmer = blend.SampleDimmer;
                }

                if (blend.VignetteEffectOverride)
                {
                    flare.VignetteEffectOverride = true;
                    flare.VignetteEffect = blend.VignetteEffect;
                }

                if (blend.StartingPositionOverride)
                {
                    flare.StartingPositionOverride = true;
                    flare.StartingPosition = blend.StartingPosition;
                }

                if (blend.ScaleOverride)
                {
                    flare.ScaleOverride = true;
                    flare.Scale = blend.Scale;
                }

                if (blend.StreaksIntensityOverride)
                {
                    flare.StreaksIntensityOverride = true;
                    flare.StreaksIntensity = blend.StreaksIntensity;
                }

                if (blend.StreaksLengthOverride)
                {
                    flare.StreaksLengthOverride = true;
                    flare.StreaksLength = blend.StreaksLength;
                }

                if (blend.StreaksOrientationOverride)
                {
                    flare.StreaksOrientationOverride = true;
                    flare.StreaksOrientation = blend.StreaksOrientation;
                }

                if (blend.StreaksThresholdOverride)
                {
                    flare.StreaksThresholdOverride = true;
                    flare.StreaksThreshold = blend.StreaksThreshold;
                }

                if (blend.ChromaticAbberationIntensityOverride)
                {
                    flare.ChromaticAbberationIntensityOverride = true;
                    flare.ChromaticAbberationIntensity = blend.ChromaticAbberationIntensity;
                }
            }
        }

        private struct VolumeScreenSpaceLensFlareMixer : IMixer<VolumeScreenSpaceLensFlareBlend>
        {
            public VolumeScreenSpaceLensFlareBlend Lerp(
                in VolumeScreenSpaceLensFlareBlend a, in VolumeScreenSpaceLensFlareBlend b, in float s)
            {
                return new VolumeScreenSpaceLensFlareBlend
                {
                    Intensity = MixUtil.LerpFloat(a.Intensity, b.Intensity, s, a.IntensityOverride, b.IntensityOverride),
                    TintColor = MixUtil.LerpFloat4(a.TintColor, b.TintColor, s, a.TintColorOverride, b.TintColorOverride),
                    FirstFlareIntensity = MixUtil.LerpFloat(a.FirstFlareIntensity, b.FirstFlareIntensity, s, a.FirstFlareIntensityOverride, b.FirstFlareIntensityOverride),
                    SecondaryFlareIntensity = MixUtil.LerpFloat(a.SecondaryFlareIntensity, b.SecondaryFlareIntensity, s, a.SecondaryFlareIntensityOverride, b.SecondaryFlareIntensityOverride),
                    WarpedFlareIntensity = MixUtil.LerpFloat(a.WarpedFlareIntensity, b.WarpedFlareIntensity, s, a.WarpedFlareIntensityOverride, b.WarpedFlareIntensityOverride),
                    WarpedFlareScale = MixUtil.LerpFloat2(a.WarpedFlareScale, b.WarpedFlareScale, s, a.WarpedFlareScaleOverride, b.WarpedFlareScaleOverride),
                    SampleDimmer = MixUtil.LerpFloat(a.SampleDimmer, b.SampleDimmer, s, a.SampleDimmerOverride, b.SampleDimmerOverride),
                    VignetteEffect = MixUtil.LerpFloat(a.VignetteEffect, b.VignetteEffect, s, a.VignetteEffectOverride, b.VignetteEffectOverride),
                    StartingPosition = MixUtil.LerpFloat(a.StartingPosition, b.StartingPosition, s, a.StartingPositionOverride, b.StartingPositionOverride),
                    Scale = MixUtil.LerpFloat(a.Scale, b.Scale, s, a.ScaleOverride, b.ScaleOverride),
                    StreaksIntensity = MixUtil.LerpFloat(a.StreaksIntensity, b.StreaksIntensity, s, a.StreaksIntensityOverride, b.StreaksIntensityOverride),
                    StreaksLength = MixUtil.LerpFloat(a.StreaksLength, b.StreaksLength, s, a.StreaksLengthOverride, b.StreaksLengthOverride),
                    StreaksOrientation = MixUtil.LerpFloat(a.StreaksOrientation, b.StreaksOrientation, s, a.StreaksOrientationOverride, b.StreaksOrientationOverride),
                    StreaksThreshold = MixUtil.LerpFloat(a.StreaksThreshold, b.StreaksThreshold, s, a.StreaksThresholdOverride, b.StreaksThresholdOverride),
                    ChromaticAbberationIntensity = MixUtil.LerpFloat(
                        a.ChromaticAbberationIntensity, b.ChromaticAbberationIntensity, s,
                        a.ChromaticAbberationIntensityOverride, b.ChromaticAbberationIntensityOverride),
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                    TintColorOverride = a.TintColorOverride || b.TintColorOverride,
                    FirstFlareIntensityOverride = a.FirstFlareIntensityOverride || b.FirstFlareIntensityOverride,
                    SecondaryFlareIntensityOverride = a.SecondaryFlareIntensityOverride || b.SecondaryFlareIntensityOverride,
                    WarpedFlareIntensityOverride = a.WarpedFlareIntensityOverride || b.WarpedFlareIntensityOverride,
                    WarpedFlareScaleOverride = a.WarpedFlareScaleOverride || b.WarpedFlareScaleOverride,
                    SampleDimmerOverride = a.SampleDimmerOverride || b.SampleDimmerOverride,
                    VignetteEffectOverride = a.VignetteEffectOverride || b.VignetteEffectOverride,
                    StartingPositionOverride = a.StartingPositionOverride || b.StartingPositionOverride,
                    ScaleOverride = a.ScaleOverride || b.ScaleOverride,
                    StreaksIntensityOverride = a.StreaksIntensityOverride || b.StreaksIntensityOverride,
                    StreaksLengthOverride = a.StreaksLengthOverride || b.StreaksLengthOverride,
                    StreaksOrientationOverride = a.StreaksOrientationOverride || b.StreaksOrientationOverride,
                    StreaksThresholdOverride = a.StreaksThresholdOverride || b.StreaksThresholdOverride,
                    ChromaticAbberationIntensityOverride = a.ChromaticAbberationIntensityOverride || b.ChromaticAbberationIntensityOverride,
                };
            }

            public VolumeScreenSpaceLensFlareBlend Add(in VolumeScreenSpaceLensFlareBlend a, in VolumeScreenSpaceLensFlareBlend b)
            {
                return new VolumeScreenSpaceLensFlareBlend
                {
                    Intensity = MixUtil.AddFloat(a.Intensity, b.Intensity, a.IntensityOverride, b.IntensityOverride),
                    TintColor = MixUtil.AddFloat4(a.TintColor, b.TintColor, a.TintColorOverride, b.TintColorOverride),
                    FirstFlareIntensity = MixUtil.AddFloat(a.FirstFlareIntensity, b.FirstFlareIntensity, a.FirstFlareIntensityOverride, b.FirstFlareIntensityOverride),
                    SecondaryFlareIntensity = MixUtil.AddFloat(a.SecondaryFlareIntensity, b.SecondaryFlareIntensity, a.SecondaryFlareIntensityOverride, b.SecondaryFlareIntensityOverride),
                    WarpedFlareIntensity = MixUtil.AddFloat(a.WarpedFlareIntensity, b.WarpedFlareIntensity, a.WarpedFlareIntensityOverride, b.WarpedFlareIntensityOverride),
                    WarpedFlareScale = MixUtil.AddFloat2(a.WarpedFlareScale, b.WarpedFlareScale, a.WarpedFlareScaleOverride, b.WarpedFlareScaleOverride),
                    SampleDimmer = MixUtil.AddFloat(a.SampleDimmer, b.SampleDimmer, a.SampleDimmerOverride, b.SampleDimmerOverride),
                    VignetteEffect = MixUtil.AddFloat(a.VignetteEffect, b.VignetteEffect, a.VignetteEffectOverride, b.VignetteEffectOverride),
                    StartingPosition = MixUtil.AddFloat(a.StartingPosition, b.StartingPosition, a.StartingPositionOverride, b.StartingPositionOverride),
                    Scale = MixUtil.AddFloat(a.Scale, b.Scale, a.ScaleOverride, b.ScaleOverride),
                    StreaksIntensity = MixUtil.AddFloat(a.StreaksIntensity, b.StreaksIntensity, a.StreaksIntensityOverride, b.StreaksIntensityOverride),
                    StreaksLength = MixUtil.AddFloat(a.StreaksLength, b.StreaksLength, a.StreaksLengthOverride, b.StreaksLengthOverride),
                    StreaksOrientation = MixUtil.AddFloat(a.StreaksOrientation, b.StreaksOrientation, a.StreaksOrientationOverride, b.StreaksOrientationOverride),
                    StreaksThreshold = MixUtil.AddFloat(a.StreaksThreshold, b.StreaksThreshold, a.StreaksThresholdOverride, b.StreaksThresholdOverride),
                    ChromaticAbberationIntensity = MixUtil.AddFloat(
                        a.ChromaticAbberationIntensity, b.ChromaticAbberationIntensity,
                        a.ChromaticAbberationIntensityOverride, b.ChromaticAbberationIntensityOverride),
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                    TintColorOverride = a.TintColorOverride || b.TintColorOverride,
                    FirstFlareIntensityOverride = a.FirstFlareIntensityOverride || b.FirstFlareIntensityOverride,
                    SecondaryFlareIntensityOverride = a.SecondaryFlareIntensityOverride || b.SecondaryFlareIntensityOverride,
                    WarpedFlareIntensityOverride = a.WarpedFlareIntensityOverride || b.WarpedFlareIntensityOverride,
                    WarpedFlareScaleOverride = a.WarpedFlareScaleOverride || b.WarpedFlareScaleOverride,
                    SampleDimmerOverride = a.SampleDimmerOverride || b.SampleDimmerOverride,
                    VignetteEffectOverride = a.VignetteEffectOverride || b.VignetteEffectOverride,
                    StartingPositionOverride = a.StartingPositionOverride || b.StartingPositionOverride,
                    ScaleOverride = a.ScaleOverride || b.ScaleOverride,
                    StreaksIntensityOverride = a.StreaksIntensityOverride || b.StreaksIntensityOverride,
                    StreaksLengthOverride = a.StreaksLengthOverride || b.StreaksLengthOverride,
                    StreaksOrientationOverride = a.StreaksOrientationOverride || b.StreaksOrientationOverride,
                    StreaksThresholdOverride = a.StreaksThresholdOverride || b.StreaksThresholdOverride,
                    ChromaticAbberationIntensityOverride = a.ChromaticAbberationIntensityOverride || b.ChromaticAbberationIntensityOverride,
                };
            }
        }
    }
}
#endif
