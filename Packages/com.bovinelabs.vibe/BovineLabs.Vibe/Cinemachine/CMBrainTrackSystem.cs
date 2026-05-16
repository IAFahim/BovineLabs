// <copyright file="CMBrainTrackSystem.cs" company="BovineLabs">
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
    /// Blends Cinemachine brain timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMBrainTrackSystem : ISystem
    {
        private TrackLifeImpl<CMBrain, CMBrainInitial> lifeImpl;
        private TrackBlendImpl<CMBrainBlend, CMBrainAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMBrain>();
            state.RequireForUpdate<CMBrainClipData>();
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
                .WithAllRW<CMBrainAnimated>()
                .WithAll<CMBrainClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMBrainAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMBrainClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Brains = SystemAPI.GetComponentLookup<CMBrain>(),
                Initials = SystemAPI.GetComponentLookup<CMBrainInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Brains = SystemAPI.GetComponentLookup<CMBrain>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMBrainAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMBrainClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMBrain> Brains;

            [ReadOnly]
            public ComponentLookup<CMBrainInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMBrainAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMBrainClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var trackBindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk];
                    ref var clipBlob = ref clipData.Value.Value;
                    ref readonly var trackBinding = ref trackBindings[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipBlob.Type switch
                    {
                        CMBrainClipType.Initial => this.SelectInitialValue(clip.Track, trackBinding.Value),
                        CMBrainClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, clipBlob),
                        _ => animated.Value,
                    };
                }
            }

            private CMBrainBlend SelectInitialValue(Entity trackEntity, Entity boundEntity)
            {
                var initial = this.Initials[trackEntity];

                if (this.Brains.TryGetRefRW(boundEntity, out var brain))
                {
                    brain.ValueRW = initial.Value;
                }

                return new CMBrainBlend
                {
                    DefaultBlendTime = math.max(initial.Value.DefaultBlend.Time, 0f),
                };
            }

            private CMBrainBlend SelectAnimatedValue(Entity boundEntity, in CMBrainClipBlob clipData)
            {
                if (this.Brains.TryGetRefRW(boundEntity, out var brain))
                {
                    ref var value = ref brain.ValueRW;
                    value.IgnoreTimeScale = clipData.IgnoreTimeScale;
                    value.UpdateMethod = clipData.UpdateMethod;
                    value.BlendUpdateMethod = clipData.BlendUpdateMethod;
                    value.DefaultBlend.Style = clipData.DefaultBlendStyle;
                }

                return new CMBrainBlend
                {
                    DefaultBlendTime = math.max(clipData.DefaultBlendTime, 0f),
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMBrainBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMBrain> Brains;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Brains.TryGetRefRW(entity, out var brain))
                {
                    return;
                }

                ref var value = ref brain.ValueRW;
                var current = new CMBrainBlend
                {
                    DefaultBlendTime = math.max(value.DefaultBlend.Time, 0f),
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(CMBrainMixer));
                value.DefaultBlend.Time = math.max(blended.DefaultBlendTime, 0f);
            }
        }

        private struct CMBrainMixer : IMixer<CMBrainBlend>
        {
            public CMBrainBlend Lerp(in CMBrainBlend a, in CMBrainBlend b, in float s)
            {
                return new CMBrainBlend
                {
                    DefaultBlendTime = math.lerp(a.DefaultBlendTime, b.DefaultBlendTime, s),
                };
            }

            public CMBrainBlend Add(in CMBrainBlend a, in CMBrainBlend b)
            {
                return new CMBrainBlend
                {
                    DefaultBlendTime = a.DefaultBlendTime + b.DefaultBlendTime,
                };
            }
        }
    }
}
#endif
