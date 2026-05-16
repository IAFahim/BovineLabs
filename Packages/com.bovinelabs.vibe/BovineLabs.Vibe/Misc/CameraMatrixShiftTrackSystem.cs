// <copyright file="CameraMatrixShiftTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Camera;
    using BovineLabs.Core.Jobs;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Camera;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Evaluates camera matrix shift timeline tracks and applies their blended results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CameraMatrixShiftTrackSystem : ISystem
    {
        private TrackBlendImpl<CameraMatrixShiftBlend, CameraMatrixShiftAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraMatrixShiftClipData>();
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
            SystemAPI.TryGetSingletonEntity<CameraMain>(out var cameraMainEntity);

            var offsetsRw = SystemAPI.GetComponentLookup<CameraViewSpaceOffset>();
            var offsetsRo = SystemAPI.GetComponentLookup<CameraViewSpaceOffset>(true);

            new TrackDeactivateJob
            {
                Offsets = offsetsRw,
                CameraMain = cameraMainEntity,
            }.Schedule();

            new TrackActivateJob
            {
                Offsets = offsetsRo,
                CameraMain = cameraMainEntity,
            }.ScheduleParallel();

            var clipActivateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<CameraMatrixShiftAnimated>()
                .WithAll<CameraMatrixShiftClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CameraMatrixShiftAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CameraMatrixShiftClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<CameraMatrixShiftInitial>(true),
            }.ScheduleParallel(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Offsets = offsetsRw,
                CameraMain = cameraMainEntity,
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))]
        [WithDisabled(typeof(TimelineActive))]
        [WithAll(typeof(TimelineActivePrevious))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<CameraViewSpaceOffset> Offsets;

            public Entity CameraMain;

            private void Execute(in CameraMatrixShiftInitial initial, in TrackBinding trackBinding)
            {
                var cameraEntity = trackBinding.Value == Entity.Null ? this.CameraMain : trackBinding.Value;

                if (this.Offsets.TryGetRefRW(cameraEntity, out var offset))
                {
                    offset.ValueRW = initial.Offset;
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        private partial struct TrackActivateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<CameraViewSpaceOffset> Offsets;

            public Entity CameraMain;

            private void Execute(ref CameraMatrixShiftInitial initial, in TrackBinding trackBinding)
            {
                var cameraEntity = trackBinding.Value == Entity.Null ? this.CameraMain : trackBinding.Value;

                if (this.Offsets.TryGetComponent(cameraEntity, out var offset))
                {
                    initial.Offset = offset;
                }
            }
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CameraMatrixShiftAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CameraMatrixShiftClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<CameraMatrixShiftInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CameraMatrixShiftAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CameraMatrixShiftClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clipData = ref clipDatas[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        CameraMatrixShiftClipType.Initial => this.SelectInitialValue(clip.Track, animated.Value),
                        CameraMatrixShiftClipType.Animated => new CameraMatrixShiftBlend
                        {
                            ProjectionCenterOffset = clipData.ProjectionCenterOffset,
                        },
                        _ => animated.Value,
                    };
                }
            }

            private CameraMatrixShiftBlend SelectInitialValue(in Entity trackEntity, in CameraMatrixShiftBlend defaultValue)
            {
                if (!this.Initials.TryGetComponent(trackEntity, out var initial))
                {
                    return defaultValue;
                }

                return new CameraMatrixShiftBlend
                {
                    ProjectionCenterOffset = initial.Offset.ProjectionCenterOffset,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CameraMatrixShiftBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CameraViewSpaceOffset> Offsets;

            public Entity CameraMain;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                var cameraEntity = entity == Entity.Null ? this.CameraMain : entity;

                if (!this.Offsets.TryGetRefRW(cameraEntity, out var offset))
                {
                    return;
                }

                var defaultValue = new CameraMatrixShiftBlend
                {
                    ProjectionCenterOffset = offset.ValueRW.ProjectionCenterOffset,
                };

                var blended = JobHelpers.Blend<CameraMatrixShiftBlend, CameraMatrixShiftMixer>(ref mixData, defaultValue);

                offset.ValueRW = new CameraViewSpaceOffset
                {
                    ProjectionCenterOffset = blended.ProjectionCenterOffset,
                };
            }
        }

        private struct CameraMatrixShiftMixer : IMixer<CameraMatrixShiftBlend>
        {
            public CameraMatrixShiftBlend Lerp(in CameraMatrixShiftBlend a, in CameraMatrixShiftBlend b, in float t)
            {
                return new CameraMatrixShiftBlend
                {
                    ProjectionCenterOffset = math.lerp(a.ProjectionCenterOffset, b.ProjectionCenterOffset, t),
                };
            }

            public CameraMatrixShiftBlend Add(in CameraMatrixShiftBlend a, in CameraMatrixShiftBlend b)
            {
                return new CameraMatrixShiftBlend
                {
                    ProjectionCenterOffset = a.ProjectionCenterOffset + b.ProjectionCenterOffset,
                };
            }
        }
    }
}
#endif
