// <copyright file="VolumePaniniProjectionTrackSystem.cs" company="BovineLabs">
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
    /// Blends panini projection clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumePaniniProjectionTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumePaniniProjection, VolumePaniniProjectionInitial> lifeImpl;
        private TrackBlendImpl<VolumePaniniProjectionBlend, VolumePaniniProjectionAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumePaniniProjectionClipData>();
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
                .WithAllRW<VolumePaniniProjectionAnimated>()
                .WithAll<VolumePaniniProjectionClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumePaniniProjectionAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumePaniniProjectionClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumePaniniProjectionInitial>(true),
                Projections = SystemAPI.GetComponentLookup<VolumePaniniProjection>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Projections = SystemAPI.GetComponentLookup<VolumePaniniProjection>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumePaniniProjectionBlend CreateBlend(in VolumePaniniProjection data, bool useOverrides)
        {
            return new VolumePaniniProjectionBlend
            {
                Distance = data.Distance,
                CropToFit = data.CropToFit,
                DistanceOverride = useOverrides && data.DistanceOverride,
                CropToFitOverride = useOverrides && data.CropToFitOverride,
            };
        }

        private static VolumePaniniProjectionBlend CreateBlend(in VolumePaniniProjectionConstantData data)
        {
            return new VolumePaniniProjectionBlend
            {
                Distance = data.Distance,
                CropToFit = data.CropToFit,
                DistanceOverride = data.DistanceOverride,
                CropToFitOverride = data.CropToFitOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumePaniniProjectionAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumePaniniProjectionClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumePaniniProjectionInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumePaniniProjection> Projections;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumePaniniProjectionAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumePaniniProjectionClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumePaniniProjectionClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Projections);
                            break;
                        case VolumePaniniProjectionClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyActive(binding.Value, clipBlob.Constant.Active, ref this.Projections);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyActive(Entity entity, bool active, ref ComponentLookup<VolumePaniniProjection> projections)
            {
                if (!projections.TryGetRefRW(entity, out var projection))
                {
                    return;
                }

                projection.ValueRW.Active = active;
            }

            private static void ApplyInitial(Entity entity, in VolumePaniniProjection initial, ref ComponentLookup<VolumePaniniProjection> projections)
            {
                if (!projections.TryGetRefRW(entity, out var projection))
                {
                    return;
                }

                ref var value = ref projection.ValueRW;
                value.Distance = initial.Distance;
                value.CropToFit = initial.CropToFit;
                value.Active = initial.Active;
                value.DistanceOverride = initial.DistanceOverride;
                value.CropToFitOverride = initial.CropToFitOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumePaniniProjectionBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumePaniniProjection> Projections;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Projections.TryGetRefRW(entity, out var projection))
                {
                    return;
                }

                ref var value = ref projection.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumePaniniProjectionMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumePaniniProjection projection, in VolumePaniniProjectionBlend blend)
            {
                if (blend.DistanceOverride)
                {
                    projection.DistanceOverride = true;
                    projection.Distance = blend.Distance;
                }

                if (blend.CropToFitOverride)
                {
                    projection.CropToFitOverride = true;
                    projection.CropToFit = blend.CropToFit;
                }
            }
        }

        private struct VolumePaniniProjectionMixer : IMixer<VolumePaniniProjectionBlend>
        {
            public VolumePaniniProjectionBlend Lerp(in VolumePaniniProjectionBlend a, in VolumePaniniProjectionBlend b, in float s)
            {
                return new VolumePaniniProjectionBlend
                {
                    Distance = MixUtil.LerpFloat(a.Distance, b.Distance, s, a.DistanceOverride, b.DistanceOverride),
                    CropToFit = MixUtil.LerpFloat(a.CropToFit, b.CropToFit, s, a.CropToFitOverride, b.CropToFitOverride),
                    DistanceOverride = a.DistanceOverride || b.DistanceOverride,
                    CropToFitOverride = a.CropToFitOverride || b.CropToFitOverride,
                };
            }

            public VolumePaniniProjectionBlend Add(in VolumePaniniProjectionBlend a, in VolumePaniniProjectionBlend b)
            {
                return new VolumePaniniProjectionBlend
                {
                    Distance = MixUtil.AddFloat(a.Distance, b.Distance, a.DistanceOverride, b.DistanceOverride),
                    CropToFit = MixUtil.AddFloat(a.CropToFit, b.CropToFit, a.CropToFitOverride, b.CropToFitOverride),
                    DistanceOverride = a.DistanceOverride || b.DistanceOverride,
                    CropToFitOverride = a.CropToFitOverride || b.CropToFitOverride,
                };
            }
        }
    }
}
#endif
