// <copyright file="CMRotationComposerTrackSystem.cs" company="BovineLabs">
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
    /// Blends Cinemachine rotation composer timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMRotationComposerTrackSystem : ISystem
    {
        private TrackLifeImpl<CMRotationComposer, CMRotationComposerInitial> lifeImpl;
        private TrackBlendImpl<CMRotationComposerBlend, CMRotationComposerAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMRotationComposerClipData>();

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
                .WithAllRW<CMRotationComposerAnimated>()
                .WithAll<CMRotationComposerClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMRotationComposerAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMRotationComposerClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Composers = SystemAPI.GetComponentLookup<CMRotationComposer>(),
                Initials = SystemAPI.GetComponentLookup<CMRotationComposerInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Composers = SystemAPI.GetComponentLookup<CMRotationComposer>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static CMRotationComposerBlend CreateBlend(in CMRotationComposer composer)
        {
            return new CMRotationComposerBlend
            {
                TargetOffset = composer.TargetOffset,
                Damping = composer.Damping,
                LookaheadTime = composer.Lookahead.Time,
                LookaheadSmoothing = composer.Lookahead.Smoothing,
                ScreenPosition = composer.Composition.ScreenPosition,
                DeadZoneSize = composer.Composition.DeadZone.Size,
                HardLimitSize = composer.Composition.HardLimits.Size,
                HardLimitOffset = composer.Composition.HardLimits.Offset,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMRotationComposerAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMRotationComposerClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMRotationComposer> Composers;

            [ReadOnly]
            public ComponentLookup<CMRotationComposerInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMRotationComposerAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMRotationComposerClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        CMRotationComposerClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMRotationComposerClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, clipBlob),
                        _ => animated.Value,
                    };
                }
            }

            private CMRotationComposerBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return CreateBlend(initial.Value);
            }

            private CMRotationComposerBlend SelectAnimatedValue(Entity boundEntity, in CMRotationComposerClipBlob clipData)
            {
                if (this.Composers.TryGetRefRW(boundEntity, out var composer))
                {
                    ref var value = ref composer.ValueRW;

                    var lookahead = value.Lookahead;
                    lookahead.Enabled = clipData.LookaheadEnabled;
                    lookahead.IgnoreY = clipData.LookaheadIgnoreY;
                    value.Lookahead = lookahead;

                    var composition = value.Composition;
                    composition.DeadZone.Enabled = clipData.DeadZoneEnabled;
                    composition.HardLimits.Enabled = clipData.HardLimitsEnabled;
                    value.Composition = composition;

                    value.CenterOnActivate = clipData.CenterOnActivate;
                }

                return new CMRotationComposerBlend
                {
                    TargetOffset = clipData.TargetOffset,
                    Damping = clipData.Damping,
                    LookaheadTime = clipData.LookaheadTime,
                    LookaheadSmoothing = clipData.LookaheadSmoothing,
                    ScreenPosition = clipData.ScreenPosition,
                    DeadZoneSize = clipData.DeadZoneSize,
                    HardLimitSize = clipData.HardLimitSize,
                    HardLimitOffset = clipData.HardLimitOffset,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMRotationComposerBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMRotationComposer> Composers;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Composers.TryGetRefRW(entity, out var composer))
                {
                    return;
                }

                ref var value = ref composer.ValueRW;
                var current = CreateBlend(value);
                var blended = JobHelpers.Blend(ref mixData, current, default(CMRotationComposerMixer));
                ApplyBlend(ref value, in blended);
                Sanitize(ref value);
            }

            private static void ApplyBlend(ref CMRotationComposer composer, in CMRotationComposerBlend blend)
            {
                composer.TargetOffset = blend.TargetOffset;
                composer.Damping = blend.Damping;

                var lookahead = composer.Lookahead;
                lookahead.Time = blend.LookaheadTime;
                lookahead.Smoothing = blend.LookaheadSmoothing;
                composer.Lookahead = lookahead;

                var composition = composer.Composition;
                composition.ScreenPosition = blend.ScreenPosition;
                composition.DeadZone.Size = blend.DeadZoneSize;
                composition.HardLimits.Size = blend.HardLimitSize;
                composition.HardLimits.Offset = blend.HardLimitOffset;
                composer.Composition = composition;
            }

            private static void Sanitize(ref CMRotationComposer composer)
            {
                composer.Damping = math.max(composer.Damping, float2.zero);

                var lookahead = composer.Lookahead;
                lookahead.Time = math.clamp(lookahead.Time, 0f, 1f);
                lookahead.Smoothing = math.clamp(lookahead.Smoothing, 0f, 30f);
                composer.Lookahead = lookahead;

                var composition = composer.Composition;
                composition.Validate();
                composer.Composition = composition;
            }
        }

        private struct CMRotationComposerMixer : IMixer<CMRotationComposerBlend>
        {
            public CMRotationComposerBlend Lerp(in CMRotationComposerBlend a, in CMRotationComposerBlend b, in float s)
            {
                return new CMRotationComposerBlend
                {
                    TargetOffset = math.lerp(a.TargetOffset, b.TargetOffset, s),
                    Damping = math.lerp(a.Damping, b.Damping, s),
                    LookaheadTime = math.lerp(a.LookaheadTime, b.LookaheadTime, s),
                    LookaheadSmoothing = math.lerp(a.LookaheadSmoothing, b.LookaheadSmoothing, s),
                    ScreenPosition = math.lerp(a.ScreenPosition, b.ScreenPosition, s),
                    DeadZoneSize = math.lerp(a.DeadZoneSize, b.DeadZoneSize, s),
                    HardLimitSize = math.lerp(a.HardLimitSize, b.HardLimitSize, s),
                    HardLimitOffset = math.lerp(a.HardLimitOffset, b.HardLimitOffset, s),
                };
            }

            public CMRotationComposerBlend Add(in CMRotationComposerBlend a, in CMRotationComposerBlend b)
            {
                return new CMRotationComposerBlend
                {
                    TargetOffset = a.TargetOffset + b.TargetOffset,
                    Damping = a.Damping + b.Damping,
                    LookaheadTime = a.LookaheadTime + b.LookaheadTime,
                    LookaheadSmoothing = a.LookaheadSmoothing + b.LookaheadSmoothing,
                    ScreenPosition = a.ScreenPosition + b.ScreenPosition,
                    DeadZoneSize = a.DeadZoneSize + b.DeadZoneSize,
                    HardLimitSize = a.HardLimitSize + b.HardLimitSize,
                    HardLimitOffset = a.HardLimitOffset + b.HardLimitOffset,
                };
            }
        }
    }
}
#endif
