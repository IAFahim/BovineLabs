// <copyright file="CMCameraOffsetTrackSystem.cs" company="BovineLabs">
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
    /// Blends Cinemachine camera offset timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMCameraOffsetTrackSystem : ISystem
    {
        private TrackLifeImpl<CMCameraOffset, CMCameraOffsetInitial> lifeImpl;
        private TrackBlendImpl<CMCameraOffsetBlend, CMCameraOffsetAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMCameraOffsetClipData>();
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
                .WithAllRW<CMCameraOffsetAnimated>()
                .WithAll<CMCameraOffsetClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMCameraOffsetAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMCameraOffsetClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Offsets = SystemAPI.GetComponentLookup<CMCameraOffset>(),
                Initials = SystemAPI.GetComponentLookup<CMCameraOffsetInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Offsets = SystemAPI.GetComponentLookup<CMCameraOffset>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMCameraOffsetAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMCameraOffsetClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMCameraOffset> Offsets;

            [ReadOnly]
            public ComponentLookup<CMCameraOffsetInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMCameraOffsetAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMCameraOffsetClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var trackBindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clipData = ref clipDatas[entityIndexInChunk];
                    ref readonly var trackBinding = ref trackBindings[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        CMCameraOffsetClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMCameraOffsetClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, clipData),
                        _ => animated.Value,
                    };
                }
            }

            private CMCameraOffsetBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return new CMCameraOffsetBlend
                {
                    Offset = initial.Value.Offset,
                };
            }

            private CMCameraOffsetBlend SelectAnimatedValue(Entity boundEntity, in CMCameraOffsetClipData data)
            {
                if (this.Offsets.TryGetRefRW(boundEntity, out var offset))
                {
                    ref var value = ref offset.ValueRW;
                    value.ApplyAfter = data.ApplyAfter;
                    value.PreserveComposition = data.PreserveComposition;
                }

                return new CMCameraOffsetBlend
                {
                    Offset = data.Offset,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMCameraOffsetBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMCameraOffset> Offsets;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Offsets.TryGetRefRW(entity, out var offset))
                {
                    return;
                }

                ref var value = ref offset.ValueRW;
                var current = new CMCameraOffsetBlend
                {
                    Offset = value.Offset,
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(CMCameraOffsetMixer));
                value.Offset = blended.Offset;
            }
        }

        private struct CMCameraOffsetMixer : IMixer<CMCameraOffsetBlend>
        {
            public CMCameraOffsetBlend Lerp(in CMCameraOffsetBlend a, in CMCameraOffsetBlend b, in float s)
            {
                return new CMCameraOffsetBlend
                {
                    Offset = math.lerp(a.Offset, b.Offset, s),
                };
            }

            public CMCameraOffsetBlend Add(in CMCameraOffsetBlend a, in CMCameraOffsetBlend b)
            {
                return new CMCameraOffsetBlend
                {
                    Offset = a.Offset + b.Offset,
                };
            }
        }
    }
}
#endif
