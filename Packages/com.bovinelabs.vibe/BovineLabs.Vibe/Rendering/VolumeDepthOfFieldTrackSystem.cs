// <copyright file="VolumeDepthOfFieldTrackSystem.cs" company="BovineLabs">
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

    /// <summary>
    /// Blends depth of field clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeDepthOfFieldTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeDepthOfField, VolumeDepthOfFieldInitial> lifeImpl;
        private TrackBlendImpl<VolumeDepthOfFieldBlend, VolumeDepthOfFieldAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeDepthOfFieldClipData>();
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
                .WithAllRW<VolumeDepthOfFieldAnimated>()
                .WithAll<VolumeDepthOfFieldClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeDepthOfFieldAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeDepthOfFieldClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeDepthOfFieldInitial>(true),
                Depths = SystemAPI.GetComponentLookup<VolumeDepthOfField>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Depths = SystemAPI.GetComponentLookup<VolumeDepthOfField>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeDepthOfFieldBlend CreateBlend(in VolumeDepthOfField data, bool useOverrides)
        {
            return new VolumeDepthOfFieldBlend
            {
                GaussianStart = data.GaussianStart,
                GaussianEnd = data.GaussianEnd,
                GaussianMaxRadius = data.GaussianMaxRadius,
                FocusDistance = data.FocusDistance,
                Aperture = data.Aperture,
                FocalLength = data.FocalLength,
                BladeCurvature = data.BladeCurvature,
                BladeRotation = data.BladeRotation,
                GaussianStartOverride = useOverrides && data.GaussianStartOverride,
                GaussianEndOverride = useOverrides && data.GaussianEndOverride,
                GaussianMaxRadiusOverride = useOverrides && data.GaussianMaxRadiusOverride,
                FocusDistanceOverride = useOverrides && data.FocusDistanceOverride,
                ApertureOverride = useOverrides && data.ApertureOverride,
                FocalLengthOverride = useOverrides && data.FocalLengthOverride,
                BladeCurvatureOverride = useOverrides && data.BladeCurvatureOverride,
                BladeRotationOverride = useOverrides && data.BladeRotationOverride,
            };
        }

        private static VolumeDepthOfFieldBlend CreateBlend(in VolumeDepthOfFieldConstantData data)
        {
            return new VolumeDepthOfFieldBlend
            {
                GaussianStart = data.GaussianStart,
                GaussianEnd = data.GaussianEnd,
                GaussianMaxRadius = data.GaussianMaxRadius,
                FocusDistance = data.FocusDistance,
                Aperture = data.Aperture,
                FocalLength = data.FocalLength,
                BladeCurvature = data.BladeCurvature,
                BladeRotation = data.BladeRotation,
                GaussianStartOverride = data.GaussianStartOverride,
                GaussianEndOverride = data.GaussianEndOverride,
                GaussianMaxRadiusOverride = data.GaussianMaxRadiusOverride,
                FocusDistanceOverride = data.FocusDistanceOverride,
                ApertureOverride = data.ApertureOverride,
                FocalLengthOverride = data.FocalLengthOverride,
                BladeCurvatureOverride = data.BladeCurvatureOverride,
                BladeRotationOverride = data.BladeRotationOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeDepthOfFieldAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeDepthOfFieldClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeDepthOfFieldInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeDepthOfField> Depths;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeDepthOfFieldAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeDepthOfFieldClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeDepthOfFieldClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Depths);
                            break;
                        case VolumeDepthOfFieldClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyConstantOverrides(binding.Value, in clipBlob.Constant, ref this.Depths);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyConstantOverrides(Entity entity, in VolumeDepthOfFieldConstantData data, ref ComponentLookup<VolumeDepthOfField> depths)
            {
                if (!depths.TryGetRefRW(entity, out var depth))
                {
                    return;
                }

                ref var value = ref depth.ValueRW;
                value.Active = data.Active;

                if (data.ModeOverride)
                {
                    value.ModeOverride = true;
                    value.Mode = data.Mode;
                }

                if (data.HighQualitySamplingOverride)
                {
                    value.HighQualitySamplingOverride = true;
                    value.HighQualitySampling = data.HighQualitySampling;
                }

                if (data.BladeCountOverride)
                {
                    value.BladeCountOverride = true;
                    value.BladeCount = data.BladeCount;
                }
            }

            private static void ApplyInitial(Entity entity, in VolumeDepthOfField initial, ref ComponentLookup<VolumeDepthOfField> depths)
            {
                if (!depths.TryGetRefRW(entity, out var depth))
                {
                    return;
                }

                ref var value = ref depth.ValueRW;
                value.Mode = initial.Mode;
                value.GaussianStart = initial.GaussianStart;
                value.GaussianEnd = initial.GaussianEnd;
                value.GaussianMaxRadius = initial.GaussianMaxRadius;
                value.HighQualitySampling = initial.HighQualitySampling;
                value.FocusDistance = initial.FocusDistance;
                value.Aperture = initial.Aperture;
                value.FocalLength = initial.FocalLength;
                value.BladeCount = initial.BladeCount;
                value.BladeCurvature = initial.BladeCurvature;
                value.BladeRotation = initial.BladeRotation;
                value.Active = initial.Active;
                value.ModeOverride = initial.ModeOverride;
                value.GaussianStartOverride = initial.GaussianStartOverride;
                value.GaussianEndOverride = initial.GaussianEndOverride;
                value.GaussianMaxRadiusOverride = initial.GaussianMaxRadiusOverride;
                value.HighQualitySamplingOverride = initial.HighQualitySamplingOverride;
                value.FocusDistanceOverride = initial.FocusDistanceOverride;
                value.ApertureOverride = initial.ApertureOverride;
                value.FocalLengthOverride = initial.FocalLengthOverride;
                value.BladeCountOverride = initial.BladeCountOverride;
                value.BladeCurvatureOverride = initial.BladeCurvatureOverride;
                value.BladeRotationOverride = initial.BladeRotationOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeDepthOfFieldBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeDepthOfField> Depths;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Depths.TryGetRefRW(entity, out var depth))
                {
                    return;
                }

                ref var value = ref depth.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeDepthOfFieldMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeDepthOfField depth, in VolumeDepthOfFieldBlend blend)
            {
                if (blend.GaussianStartOverride)
                {
                    depth.GaussianStartOverride = true;
                    depth.GaussianStart = blend.GaussianStart;
                }

                if (blend.GaussianEndOverride)
                {
                    depth.GaussianEndOverride = true;
                    depth.GaussianEnd = blend.GaussianEnd;
                }

                if (blend.GaussianMaxRadiusOverride)
                {
                    depth.GaussianMaxRadiusOverride = true;
                    depth.GaussianMaxRadius = blend.GaussianMaxRadius;
                }

                if (blend.FocusDistanceOverride)
                {
                    depth.FocusDistanceOverride = true;
                    depth.FocusDistance = blend.FocusDistance;
                }

                if (blend.ApertureOverride)
                {
                    depth.ApertureOverride = true;
                    depth.Aperture = blend.Aperture;
                }

                if (blend.FocalLengthOverride)
                {
                    depth.FocalLengthOverride = true;
                    depth.FocalLength = blend.FocalLength;
                }

                if (blend.BladeCurvatureOverride)
                {
                    depth.BladeCurvatureOverride = true;
                    depth.BladeCurvature = blend.BladeCurvature;
                }

                if (blend.BladeRotationOverride)
                {
                    depth.BladeRotationOverride = true;
                    depth.BladeRotation = blend.BladeRotation;
                }
            }
        }

        private struct VolumeDepthOfFieldMixer : IMixer<VolumeDepthOfFieldBlend>
        {
            public VolumeDepthOfFieldBlend Lerp(in VolumeDepthOfFieldBlend a, in VolumeDepthOfFieldBlend b, in float s)
            {
                return new VolumeDepthOfFieldBlend
                {
                    GaussianStart = MixUtil.LerpFloat(a.GaussianStart, b.GaussianStart, s, a.GaussianStartOverride, b.GaussianStartOverride),
                    GaussianEnd = MixUtil.LerpFloat(a.GaussianEnd, b.GaussianEnd, s, a.GaussianEndOverride, b.GaussianEndOverride),
                    GaussianMaxRadius = MixUtil.LerpFloat(a.GaussianMaxRadius, b.GaussianMaxRadius, s, a.GaussianMaxRadiusOverride, b.GaussianMaxRadiusOverride),
                    FocusDistance = MixUtil.LerpFloat(a.FocusDistance, b.FocusDistance, s, a.FocusDistanceOverride, b.FocusDistanceOverride),
                    Aperture = MixUtil.LerpFloat(a.Aperture, b.Aperture, s, a.ApertureOverride, b.ApertureOverride),
                    FocalLength = MixUtil.LerpFloat(a.FocalLength, b.FocalLength, s, a.FocalLengthOverride, b.FocalLengthOverride),
                    BladeCurvature = MixUtil.LerpFloat(a.BladeCurvature, b.BladeCurvature, s, a.BladeCurvatureOverride, b.BladeCurvatureOverride),
                    BladeRotation = MixUtil.LerpFloat(a.BladeRotation, b.BladeRotation, s, a.BladeRotationOverride, b.BladeRotationOverride),
                    GaussianStartOverride = a.GaussianStartOverride || b.GaussianStartOverride,
                    GaussianEndOverride = a.GaussianEndOverride || b.GaussianEndOverride,
                    GaussianMaxRadiusOverride = a.GaussianMaxRadiusOverride || b.GaussianMaxRadiusOverride,
                    FocusDistanceOverride = a.FocusDistanceOverride || b.FocusDistanceOverride,
                    ApertureOverride = a.ApertureOverride || b.ApertureOverride,
                    FocalLengthOverride = a.FocalLengthOverride || b.FocalLengthOverride,
                    BladeCurvatureOverride = a.BladeCurvatureOverride || b.BladeCurvatureOverride,
                    BladeRotationOverride = a.BladeRotationOverride || b.BladeRotationOverride,
                };
            }

            public VolumeDepthOfFieldBlend Add(in VolumeDepthOfFieldBlend a, in VolumeDepthOfFieldBlend b)
            {
                return new VolumeDepthOfFieldBlend
                {
                    GaussianStart = MixUtil.AddFloat(a.GaussianStart, b.GaussianStart, a.GaussianStartOverride, b.GaussianStartOverride),
                    GaussianEnd = MixUtil.AddFloat(a.GaussianEnd, b.GaussianEnd, a.GaussianEndOverride, b.GaussianEndOverride),
                    GaussianMaxRadius = MixUtil.AddFloat(a.GaussianMaxRadius, b.GaussianMaxRadius, a.GaussianMaxRadiusOverride, b.GaussianMaxRadiusOverride),
                    FocusDistance = MixUtil.AddFloat(a.FocusDistance, b.FocusDistance, a.FocusDistanceOverride, b.FocusDistanceOverride),
                    Aperture = MixUtil.AddFloat(a.Aperture, b.Aperture, a.ApertureOverride, b.ApertureOverride),
                    FocalLength = MixUtil.AddFloat(a.FocalLength, b.FocalLength, a.FocalLengthOverride, b.FocalLengthOverride),
                    BladeCurvature = MixUtil.AddFloat(a.BladeCurvature, b.BladeCurvature, a.BladeCurvatureOverride, b.BladeCurvatureOverride),
                    BladeRotation = MixUtil.AddFloat(a.BladeRotation, b.BladeRotation, a.BladeRotationOverride, b.BladeRotationOverride),
                    GaussianStartOverride = a.GaussianStartOverride || b.GaussianStartOverride,
                    GaussianEndOverride = a.GaussianEndOverride || b.GaussianEndOverride,
                    GaussianMaxRadiusOverride = a.GaussianMaxRadiusOverride || b.GaussianMaxRadiusOverride,
                    FocusDistanceOverride = a.FocusDistanceOverride || b.FocusDistanceOverride,
                    ApertureOverride = a.ApertureOverride || b.ApertureOverride,
                    FocalLengthOverride = a.FocalLengthOverride || b.FocalLengthOverride,
                    BladeCurvatureOverride = a.BladeCurvatureOverride || b.BladeCurvatureOverride,
                    BladeRotationOverride = a.BladeRotationOverride || b.BladeRotationOverride,
                };
            }
        }
    }
}
#endif
