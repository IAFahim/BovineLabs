// <copyright file="CMRotateWithFollowTargetTrackSystem.cs" company="BovineLabs">
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
    /// Blends rotate-with-follow-target timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMRotateWithFollowTargetTrackSystem : ISystem
    {
        private TrackLifeImpl<CMRotateWithFollowTarget, CMRotateWithFollowTargetInitial> lifeImpl;
        private TrackBlendImpl<CMRotateWithFollowTargetBlend, CMRotateWithFollowTargetAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMRotateWithFollowTargetClipData>();
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
                .WithAllRW<CMRotateWithFollowTargetAnimated>()
                .WithAll<CMRotateWithFollowTargetClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMRotateWithFollowTargetAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMRotateWithFollowTargetClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<CMRotateWithFollowTargetInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                RotateWithFollowTargets = SystemAPI.GetComponentLookup<CMRotateWithFollowTarget>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMRotateWithFollowTargetAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMRotateWithFollowTargetClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<CMRotateWithFollowTargetInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMRotateWithFollowTargetAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMRotateWithFollowTargetClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clipData = ref clipDatas[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        CMRotateWithFollowTargetClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMRotateWithFollowTargetClipType.Animated => this.SelectAnimatedValue(clipData),
                        _ => animated.Value,
                    };
                }
            }

            private CMRotateWithFollowTargetBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return new CMRotateWithFollowTargetBlend
                {
                    Damping = math.max(initial.Value.Damping, 0f),
                };
            }

            private CMRotateWithFollowTargetBlend SelectAnimatedValue(in CMRotateWithFollowTargetClipData clipData)
            {
                return new CMRotateWithFollowTargetBlend
                {
                    Damping = math.max(clipData.Damping, 0f),
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMRotateWithFollowTargetBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMRotateWithFollowTarget> RotateWithFollowTargets;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.RotateWithFollowTargets.TryGetRefRW(entity, out var component))
                {
                    return;
                }

                ref var value = ref component.ValueRW;
                var current = new CMRotateWithFollowTargetBlend
                {
                    Damping = math.max(value.Damping, 0f),
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(CMRotateWithFollowTargetMixer));
                value.Damping = math.max(blended.Damping, 0f);
            }
        }

        private struct CMRotateWithFollowTargetMixer : IMixer<CMRotateWithFollowTargetBlend>
        {
            public CMRotateWithFollowTargetBlend Lerp(in CMRotateWithFollowTargetBlend a, in CMRotateWithFollowTargetBlend b, in float s)
            {
                return new CMRotateWithFollowTargetBlend
                {
                    Damping = math.lerp(a.Damping, b.Damping, s),
                };
            }

            public CMRotateWithFollowTargetBlend Add(in CMRotateWithFollowTargetBlend a, in CMRotateWithFollowTargetBlend b)
            {
                return new CMRotateWithFollowTargetBlend
                {
                    Damping = a.Damping + b.Damping,
                };
            }
        }
    }
}
#endif
