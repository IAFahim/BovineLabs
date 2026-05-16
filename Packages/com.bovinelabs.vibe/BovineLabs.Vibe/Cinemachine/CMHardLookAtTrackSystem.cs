// <copyright file="CMHardLookAtTrackSystem.cs" company="BovineLabs">
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
    /// Blends hard-look-at timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMHardLookAtTrackSystem : ISystem
    {
        private TrackLifeImpl<CMHardLookAt, CMHardLookAtInitial> lifeImpl;
        private TrackBlendImpl<CMHardLookAtBlend, CMHardLookAtAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMHardLookAtClipData>();
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
                .WithAllRW<CMHardLookAtAnimated>()
                .WithAll<CMHardLookAtClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMHardLookAtAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMHardLookAtClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<CMHardLookAtInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                HardLookAts = SystemAPI.GetComponentLookup<CMHardLookAt>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMHardLookAtAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMHardLookAtClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<CMHardLookAtInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMHardLookAtAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMHardLookAtClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clipData = ref clipDatas[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        CMHardLookAtClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMHardLookAtClipType.Animated => SelectAnimatedValue(clipData),
                        _ => animated.Value,
                    };
                }
            }

            private CMHardLookAtBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return new CMHardLookAtBlend
                {
                    LookAtOffset = initial.Value.LookAtOffset,
                };
            }

            private static CMHardLookAtBlend SelectAnimatedValue(in CMHardLookAtClipData clipData)
            {
                return new CMHardLookAtBlend
                {
                    LookAtOffset = clipData.LookAtOffset,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMHardLookAtBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMHardLookAt> HardLookAts;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.HardLookAts.TryGetRefRW(entity, out var component))
                {
                    return;
                }

                ref var value = ref component.ValueRW;
                var current = new CMHardLookAtBlend { LookAtOffset = value.LookAtOffset };
                var blended = JobHelpers.Blend(ref mixData, current, default(CMHardLookAtMixer));
                value.LookAtOffset = blended.LookAtOffset;
            }
        }

        private struct CMHardLookAtMixer : IMixer<CMHardLookAtBlend>
        {
            public CMHardLookAtBlend Lerp(in CMHardLookAtBlend a, in CMHardLookAtBlend b, in float s)
            {
                return new CMHardLookAtBlend
                {
                    LookAtOffset = math.lerp(a.LookAtOffset, b.LookAtOffset, s),
                };
            }

            public CMHardLookAtBlend Add(in CMHardLookAtBlend a, in CMHardLookAtBlend b)
            {
                return new CMHardLookAtBlend
                {
                    LookAtOffset = a.LookAtOffset + b.LookAtOffset,
                };
            }
        }
    }
}
#endif
