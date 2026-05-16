// <copyright file="CMSplineDollyTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Core.Jobs;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Cinemachine;
    using BovineLabs.Vibe.Jobs;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Evaluates Cinemachine spline dolly timeline tracks and applies their blended results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMSplineDollyTrackSystem : ISystem
    {
        private TrackLifeImpl<CMSplineDolly, CMSplineDollyInitial> lifeImpl;
        private TrackLifeImpl<CMSplineDollyTarget, CMSplineDollyTargetInitial> targetLifeImpl;
        private TrackBlendImpl<float, CMSplineDollyAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var requireQuery = new EntityQueryBuilder(Allocator.Temp).WithAny<CMSplineDollyClipData, CMSplineDollyTargetClipData>().Build(ref state);
            state.RequireForUpdate(requireQuery);

            this.lifeImpl.OnCreate(ref state);
            this.targetLifeImpl.OnCreate(ref state);
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
            this.targetLifeImpl.OnUpdate(ref state);

            state.Dependency = new TargetClipActivateJob
            {
                Targets = SystemAPI.GetComponentLookup<CMSplineDollyTarget>(),
            }.ScheduleParallel(state.Dependency);

            var clipActivateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<CMSplineDollyAnimated>()
                .WithAll<CMSplineDollyClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMSplineDollyAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMSplineDollyClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<CMSplineDollyInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Dollies = SystemAPI.GetComponentLookup<CMSplineDolly>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMSplineDollyAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMSplineDollyClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<CMSplineDollyInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMSplineDollyAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMSplineDollyClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clipData = ref clipDatas[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        CMSplineDollyClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMSplineDollyClipType.Animated => math.clamp(clipData.Position, 0f, 1f),
                        _ => animated.Value,
                    };
                }
            }

            private float SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return math.clamp(initial.Value.Position, 0f, 1f);
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<float>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMSplineDolly> Dollies;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Dollies.TryGetRefRW(entity, out var dolly))
                {
                    return;
                }

                ref var dollyValue = ref dolly.ValueRW;
                var current = math.clamp(dollyValue.Position, 0f, 1f);
                var blended = JobHelpers.Blend<float, CMSplineDollyMixer>(ref mixData, current);
                dollyValue.Position = math.clamp(blended, 0f, 1f);
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct TargetClipActivateJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMSplineDollyTarget> Targets;

            private void Execute(in CMSplineDollyTargetClipData clipData, in TrackBinding trackBinding)
            {
                if (!this.Targets.TryGetRefRW(trackBinding.Value, out var target))
                {
                    return;
                }

                target.ValueRW.Spline = clipData.Spline;
            }
        }

        private struct CMSplineDollyMixer : IMixer<float>
        {
            public float Lerp(in float a, in float b, in float t)
            {
                return math.lerp(a, b, t);
            }

            public float Add(in float a, in float b)
            {
                return a + b;
            }
        }
    }
}
#endif
