// <copyright file="VolumeColorAdjustmentsTrackSystem.cs" company="BovineLabs">
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
    /// Blends color adjustments clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeColorAdjustmentsTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeColorAdjustments, VolumeColorAdjustmentsInitial> lifeImpl;
        private TrackBlendImpl<VolumeColorAdjustmentsBlend, VolumeColorAdjustmentsAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeColorAdjustmentsClipData>();
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
                .WithAllRW<VolumeColorAdjustmentsAnimated>()
                .WithAll<VolumeColorAdjustmentsClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeColorAdjustmentsAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeColorAdjustmentsClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeColorAdjustmentsInitial>(true),
                Adjustments = SystemAPI.GetComponentLookup<VolumeColorAdjustments>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Adjustments = SystemAPI.GetComponentLookup<VolumeColorAdjustments>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeColorAdjustmentsBlend CreateBlend(in VolumeColorAdjustments data, bool useOverrides)
        {
            return new VolumeColorAdjustmentsBlend
            {
                PostExposure = data.PostExposure,
                Contrast = data.Contrast,
                HueShift = data.HueShift,
                Saturation = data.Saturation,
                ColorFilter = new float4(data.ColorFilter.r, data.ColorFilter.g, data.ColorFilter.b, data.ColorFilter.a),
                PostExposureOverride = useOverrides && data.PostExposureOverride,
                ContrastOverride = useOverrides && data.ContrastOverride,
                ColorFilterOverride = useOverrides && data.ColorFilterOverride,
                HueShiftOverride = useOverrides && data.HueShiftOverride,
                SaturationOverride = useOverrides && data.SaturationOverride,
            };
        }

        private static VolumeColorAdjustmentsBlend CreateBlend(in VolumeColorAdjustmentsConstantData data)
        {
            return new VolumeColorAdjustmentsBlend
            {
                PostExposure = data.PostExposure,
                Contrast = data.Contrast,
                HueShift = data.HueShift,
                Saturation = data.Saturation,
                ColorFilter = data.ColorFilter,
                PostExposureOverride = data.PostExposureOverride,
                ContrastOverride = data.ContrastOverride,
                ColorFilterOverride = data.ColorFilterOverride,
                HueShiftOverride = data.HueShiftOverride,
                SaturationOverride = data.SaturationOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeColorAdjustmentsAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeColorAdjustmentsClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeColorAdjustmentsInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeColorAdjustments> Adjustments;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeColorAdjustmentsAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeColorAdjustmentsClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeColorAdjustmentsClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Adjustments);
                            break;
                        case VolumeColorAdjustmentsClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyActive(binding.Value, clipBlob.Constant.Active, ref this.Adjustments);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyActive(Entity entity, bool active, ref ComponentLookup<VolumeColorAdjustments> adjustments)
            {
                if (!adjustments.TryGetRefRW(entity, out var adjustment))
                {
                    return;
                }

                adjustment.ValueRW.Active = active;
            }

            private static void ApplyInitial(Entity entity, in VolumeColorAdjustments initial, ref ComponentLookup<VolumeColorAdjustments> adjustments)
            {
                if (!adjustments.TryGetRefRW(entity, out var adjustment))
                {
                    return;
                }

                ref var value = ref adjustment.ValueRW;
                value.PostExposure = initial.PostExposure;
                value.Contrast = initial.Contrast;
                value.ColorFilter = initial.ColorFilter;
                value.HueShift = initial.HueShift;
                value.Saturation = initial.Saturation;
                value.Active = initial.Active;
                value.PostExposureOverride = initial.PostExposureOverride;
                value.ContrastOverride = initial.ContrastOverride;
                value.ColorFilterOverride = initial.ColorFilterOverride;
                value.HueShiftOverride = initial.HueShiftOverride;
                value.SaturationOverride = initial.SaturationOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeColorAdjustmentsBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeColorAdjustments> Adjustments;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Adjustments.TryGetRefRW(entity, out var adjustment))
                {
                    return;
                }

                ref var value = ref adjustment.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeColorAdjustmentsMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeColorAdjustments adjustment, in VolumeColorAdjustmentsBlend blend)
            {
                if (blend.PostExposureOverride)
                {
                    adjustment.PostExposureOverride = true;
                    adjustment.PostExposure = blend.PostExposure;
                }

                if (blend.ContrastOverride)
                {
                    adjustment.ContrastOverride = true;
                    adjustment.Contrast = blend.Contrast;
                }

                if (blend.ColorFilterOverride)
                {
                    adjustment.ColorFilterOverride = true;
                    adjustment.ColorFilter = new Color(blend.ColorFilter.x, blend.ColorFilter.y, blend.ColorFilter.z, blend.ColorFilter.w);
                }

                if (blend.HueShiftOverride)
                {
                    adjustment.HueShiftOverride = true;
                    adjustment.HueShift = blend.HueShift;
                }

                if (blend.SaturationOverride)
                {
                    adjustment.SaturationOverride = true;
                    adjustment.Saturation = blend.Saturation;
                }
            }
        }

        private struct VolumeColorAdjustmentsMixer : IMixer<VolumeColorAdjustmentsBlend>
        {
            public VolumeColorAdjustmentsBlend Lerp(in VolumeColorAdjustmentsBlend a, in VolumeColorAdjustmentsBlend b, in float s)
            {
                return new VolumeColorAdjustmentsBlend
                {
                    PostExposure = MixUtil.LerpFloat(a.PostExposure, b.PostExposure, s, a.PostExposureOverride, b.PostExposureOverride),
                    Contrast = MixUtil.LerpFloat(a.Contrast, b.Contrast, s, a.ContrastOverride, b.ContrastOverride),
                    HueShift = MixUtil.LerpFloat(a.HueShift, b.HueShift, s, a.HueShiftOverride, b.HueShiftOverride),
                    Saturation = MixUtil.LerpFloat(a.Saturation, b.Saturation, s, a.SaturationOverride, b.SaturationOverride),
                    ColorFilter = MixUtil.LerpFloat4(a.ColorFilter, b.ColorFilter, s, a.ColorFilterOverride, b.ColorFilterOverride),
                    PostExposureOverride = a.PostExposureOverride || b.PostExposureOverride,
                    ContrastOverride = a.ContrastOverride || b.ContrastOverride,
                    ColorFilterOverride = a.ColorFilterOverride || b.ColorFilterOverride,
                    HueShiftOverride = a.HueShiftOverride || b.HueShiftOverride,
                    SaturationOverride = a.SaturationOverride || b.SaturationOverride,
                };
            }

            public VolumeColorAdjustmentsBlend Add(in VolumeColorAdjustmentsBlend a, in VolumeColorAdjustmentsBlend b)
            {
                return new VolumeColorAdjustmentsBlend
                {
                    PostExposure = MixUtil.AddFloat(a.PostExposure, b.PostExposure, a.PostExposureOverride, b.PostExposureOverride),
                    Contrast = MixUtil.AddFloat(a.Contrast, b.Contrast, a.ContrastOverride, b.ContrastOverride),
                    HueShift = MixUtil.AddFloat(a.HueShift, b.HueShift, a.HueShiftOverride, b.HueShiftOverride),
                    Saturation = MixUtil.AddFloat(a.Saturation, b.Saturation, a.SaturationOverride, b.SaturationOverride),
                    ColorFilter = MixUtil.AddFloat4(a.ColorFilter, b.ColorFilter, a.ColorFilterOverride, b.ColorFilterOverride),
                    PostExposureOverride = a.PostExposureOverride || b.PostExposureOverride,
                    ContrastOverride = a.ContrastOverride || b.ContrastOverride,
                    ColorFilterOverride = a.ColorFilterOverride || b.ColorFilterOverride,
                    HueShiftOverride = a.HueShiftOverride || b.HueShiftOverride,
                    SaturationOverride = a.SaturationOverride || b.SaturationOverride,
                };
            }
        }
    }
}
#endif
