// <copyright file="CMFreeLookModifierTrackSystem.cs" company="BovineLabs">
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
    /// Blends Cinemachine free look modifier timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMFreeLookModifierTrackSystem : ISystem
    {
        private TrackLifeImpl<CMFreeLookModifier, CMFreeLookModifierInitial> lifeImpl;
        private TrackBlendImpl<CMFreeLookModifierBlend, CMFreeLookModifierAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMFreeLookModifierClipData>();
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
                .WithAllRW<CMFreeLookModifierAnimated>()
                .WithAll<CMFreeLookModifierClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMFreeLookModifierAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMFreeLookModifierClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<CMFreeLookModifierInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Modifiers = SystemAPI.GetComponentLookup<CMFreeLookModifier>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMFreeLookModifierAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMFreeLookModifierClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<CMFreeLookModifierInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMFreeLookModifierAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMFreeLookModifierClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clipData = ref clipDatas[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        CMFreeLookModifierClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMFreeLookModifierClipType.Animated => new CMFreeLookModifierBlend { Easing = clipData.Easing },
                        _ => animated.Value,
                    };
                }
            }

            private CMFreeLookModifierBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return new CMFreeLookModifierBlend
                {
                    Easing = initial.Value.Easing,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMFreeLookModifierBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMFreeLookModifier> Modifiers;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Modifiers.TryGetRefRW(entity, out var modifier))
                {
                    return;
                }

                ref var value = ref modifier.ValueRW;
                var current = new CMFreeLookModifierBlend
                {
                    Easing = value.Easing,
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(CMFreeLookModifierMixer));
                value.Easing = math.clamp(blended.Easing, 0f, 1f);
            }
        }

        private struct CMFreeLookModifierMixer : IMixer<CMFreeLookModifierBlend>
        {
            public CMFreeLookModifierBlend Lerp(in CMFreeLookModifierBlend a, in CMFreeLookModifierBlend b, in float s)
            {
                return new CMFreeLookModifierBlend
                {
                    Easing = math.lerp(a.Easing, b.Easing, s),
                };
            }

            public CMFreeLookModifierBlend Add(in CMFreeLookModifierBlend a, in CMFreeLookModifierBlend b)
            {
                return new CMFreeLookModifierBlend
                {
                    Easing = a.Easing + b.Easing,
                };
            }
        }
    }
}
#endif
