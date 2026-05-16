// <copyright file="CMRecomposerTrackSystem.cs" company="BovineLabs">
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
    /// Blends Cinemachine recomposer timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMRecomposerTrackSystem : ISystem
    {
        private TrackLifeImpl<CMRecomposer, CMRecomposerInitial> lifeImpl;
        private TrackBlendImpl<CMRecomposerBlend, CMRecomposerAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMRecomposerClipData>();
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
                .WithAllRW<CMRecomposerAnimated>()
                .WithAll<CMRecomposerClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMRecomposerAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMRecomposerClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Recomposers = SystemAPI.GetComponentLookup<CMRecomposer>(),
                Initials = SystemAPI.GetComponentLookup<CMRecomposerInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Recomposers = SystemAPI.GetComponentLookup<CMRecomposer>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static CMRecomposerBlend CreateBlend(in CMRecomposer recomposer)
        {
            return new CMRecomposerBlend
            {
                Tilt = recomposer.Tilt,
                Pan = recomposer.Pan,
                Dutch = recomposer.Dutch,
                ZoomScale = recomposer.ZoomScale,
                FollowAttachment = recomposer.FollowAttachment,
                LookAtAttachment = recomposer.LookAtAttachment,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMRecomposerAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMRecomposerClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMRecomposer> Recomposers;

            [ReadOnly]
            public ComponentLookup<CMRecomposerInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMRecomposerAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMRecomposerClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        CMRecomposerClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMRecomposerClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, clipBlob),
                        _ => animated.Value,
                    };
                }
            }

            private CMRecomposerBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return CreateBlend(initial.Value);
            }

            private CMRecomposerBlend SelectAnimatedValue(Entity boundEntity, in CMRecomposerClipBlob clipData)
            {
                if (this.Recomposers.TryGetRefRW(boundEntity, out var recomposer))
                {
                    recomposer.ValueRW.ApplyAfter = clipData.ApplyAfter;
                }

                return new CMRecomposerBlend
                {
                    Tilt = clipData.Tilt,
                    Pan = clipData.Pan,
                    Dutch = clipData.Dutch,
                    ZoomScale = clipData.ZoomScale,
                    FollowAttachment = clipData.FollowAttachment,
                    LookAtAttachment = clipData.LookAtAttachment,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMRecomposerBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMRecomposer> Recomposers;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Recomposers.TryGetRefRW(entity, out var recomposer))
                {
                    return;
                }

                ref var value = ref recomposer.ValueRW;
                var current = CreateBlend(value);
                var blended = JobHelpers.Blend(ref mixData, current, default(CMRecomposerMixer));
                ApplyBlend(ref value, in blended);
                Sanitize(ref value);
            }

            private static void ApplyBlend(ref CMRecomposer recomposer, in CMRecomposerBlend blend)
            {
                recomposer.Tilt = blend.Tilt;
                recomposer.Pan = blend.Pan;
                recomposer.Dutch = blend.Dutch;
                recomposer.ZoomScale = blend.ZoomScale;
                recomposer.FollowAttachment = blend.FollowAttachment;
                recomposer.LookAtAttachment = blend.LookAtAttachment;
            }

            private static void Sanitize(ref CMRecomposer recomposer)
            {
                recomposer.ZoomScale = math.max(0f, recomposer.ZoomScale);
                recomposer.FollowAttachment = math.clamp(recomposer.FollowAttachment, 0f, 1f);
                recomposer.LookAtAttachment = math.clamp(recomposer.LookAtAttachment, 0f, 1f);
            }
        }

        private struct CMRecomposerMixer : IMixer<CMRecomposerBlend>
        {
            public CMRecomposerBlend Lerp(in CMRecomposerBlend a, in CMRecomposerBlend b, in float s)
            {
                return new CMRecomposerBlend
                {
                    Tilt = math.lerp(a.Tilt, b.Tilt, s),
                    Pan = math.lerp(a.Pan, b.Pan, s),
                    Dutch = math.lerp(a.Dutch, b.Dutch, s),
                    ZoomScale = math.lerp(a.ZoomScale, b.ZoomScale, s),
                    FollowAttachment = math.lerp(a.FollowAttachment, b.FollowAttachment, s),
                    LookAtAttachment = math.lerp(a.LookAtAttachment, b.LookAtAttachment, s),
                };
            }

            public CMRecomposerBlend Add(in CMRecomposerBlend a, in CMRecomposerBlend b)
            {
                return new CMRecomposerBlend
                {
                    Tilt = a.Tilt + b.Tilt,
                    Pan = a.Pan + b.Pan,
                    Dutch = a.Dutch + b.Dutch,
                    ZoomScale = a.ZoomScale + b.ZoomScale,
                    FollowAttachment = a.FollowAttachment + b.FollowAttachment,
                    LookAtAttachment = a.LookAtAttachment + b.LookAtAttachment,
                };
            }
        }
    }
}
#endif
