// <copyright file="CMHardLockToTargetTrackSystem.cs" company="BovineLabs">
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
    /// Blends hard-lock-to-target timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMHardLockToTargetTrackSystem : ISystem
    {
        private TrackLifeImpl<CMHardLockToTarget, CMHardLockToTargetInitial> lifeImpl;
        private TrackBlendImpl<CMHardLockToTargetBlend, CMHardLockToTargetAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMHardLockToTargetClipData>();
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
                .WithAllRW<CMHardLockToTargetAnimated>()
                .WithAll<CMHardLockToTargetClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMHardLockToTargetAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMHardLockToTargetClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<CMHardLockToTargetInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                HardLockToTargets = SystemAPI.GetComponentLookup<CMHardLockToTarget>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMHardLockToTargetAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMHardLockToTargetClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<CMHardLockToTargetInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMHardLockToTargetAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMHardLockToTargetClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clipData = ref clipDatas[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        CMHardLockToTargetClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMHardLockToTargetClipType.Animated => this.SelectAnimatedValue(clipData),
                        _ => animated.Value,
                    };
                }
            }

            private CMHardLockToTargetBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return new CMHardLockToTargetBlend
                {
                    Damping = math.max(initial.Value.Damping, 0f),
                };
            }

            private CMHardLockToTargetBlend SelectAnimatedValue(in CMHardLockToTargetClipData clipData)
            {
                return new CMHardLockToTargetBlend
                {
                    Damping = math.max(clipData.Damping, 0f),
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMHardLockToTargetBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMHardLockToTarget> HardLockToTargets;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.HardLockToTargets.TryGetRefRW(entity, out var component))
                {
                    return;
                }

                ref var value = ref component.ValueRW;
                var current = new CMHardLockToTargetBlend
                {
                    Damping = math.max(value.Damping, 0f),
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(CMHardLockToTargetMixer));
                value.Damping = math.max(blended.Damping, 0f);
            }
        }

        private struct CMHardLockToTargetMixer : IMixer<CMHardLockToTargetBlend>
        {
            public CMHardLockToTargetBlend Lerp(in CMHardLockToTargetBlend a, in CMHardLockToTargetBlend b, in float s)
            {
                return new CMHardLockToTargetBlend
                {
                    Damping = math.lerp(a.Damping, b.Damping, s),
                };
            }

            public CMHardLockToTargetBlend Add(in CMHardLockToTargetBlend a, in CMHardLockToTargetBlend b)
            {
                return new CMHardLockToTargetBlend
                {
                    Damping = a.Damping + b.Damping,
                };
            }
        }
    }
}
#endif
