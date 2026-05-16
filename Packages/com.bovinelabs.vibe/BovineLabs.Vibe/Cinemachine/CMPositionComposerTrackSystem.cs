// <copyright file="CMPositionComposerTrackSystem.cs" company="BovineLabs">
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
    /// Blends Cinemachine position composer timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMPositionComposerTrackSystem : ISystem
    {
        private TrackLifeImpl<CMPositionComposer, CMPositionComposerInitial> lifeImpl;
        private TrackBlendImpl<CMPositionComposerBlend, CMPositionComposerAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMPositionComposerClipData>();
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
                .WithAllRW<CMPositionComposerAnimated>()
                .WithAll<CMPositionComposerClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMPositionComposerAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMPositionComposerClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Composers = SystemAPI.GetComponentLookup<CMPositionComposer>(),
                Initials = SystemAPI.GetComponentLookup<CMPositionComposerInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Composers = SystemAPI.GetComponentLookup<CMPositionComposer>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static CMPositionComposerBlend CreateBlend(in CMPositionComposer composer)
        {
            return new CMPositionComposerBlend
            {
                CameraDistance = composer.CameraDistance,
                DeadZoneDepth = composer.DeadZoneDepth,
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
            public ComponentTypeHandle<CMPositionComposerAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMPositionComposerClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMPositionComposer> Composers;

            [ReadOnly]
            public ComponentLookup<CMPositionComposerInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMPositionComposerAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMPositionComposerClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        CMPositionComposerClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMPositionComposerClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, clipBlob),
                        _ => animated.Value,
                    };
                }
            }

            private CMPositionComposerBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return CreateBlend(initial.Value);
            }

            private CMPositionComposerBlend SelectAnimatedValue(Entity boundEntity, in CMPositionComposerClipBlob clipData)
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

                return new CMPositionComposerBlend
                {
                    CameraDistance = clipData.CameraDistance,
                    DeadZoneDepth = clipData.DeadZoneDepth,
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
            public NativeParallelHashMap<Entity, MixData<CMPositionComposerBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMPositionComposer> Composers;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Composers.TryGetRefRW(entity, out var composer))
                {
                    return;
                }

                ref var value = ref composer.ValueRW;
                var current = CreateBlend(value);
                var blended = JobHelpers.Blend(ref mixData, current, default(CMPositionComposerMixer));
                ApplyBlend(ref value, in blended);
                SanitizeComposer(ref value);
            }

            private static void ApplyBlend(ref CMPositionComposer composer, in CMPositionComposerBlend blend)
            {
                composer.CameraDistance = blend.CameraDistance;
                composer.DeadZoneDepth = blend.DeadZoneDepth;
                composer.TargetOffset = blend.TargetOffset;
                composer.Damping = blend.Damping;

                composer.Lookahead.Time = blend.LookaheadTime;
                composer.Lookahead.Smoothing = blend.LookaheadSmoothing;

                composer.Composition.ScreenPosition = blend.ScreenPosition;
                composer.Composition.DeadZone.Size = blend.DeadZoneSize;
                composer.Composition.HardLimits.Size = blend.HardLimitSize;
                composer.Composition.HardLimits.Offset = blend.HardLimitOffset;
            }

            private static void SanitizeComposer(ref CMPositionComposer composer)
            {
                composer.CameraDistance = math.max(0f, composer.CameraDistance);
                composer.DeadZoneDepth = math.max(0f, composer.DeadZoneDepth);
                composer.Damping = math.max(composer.Damping, float3.zero);

                composer.Lookahead.Time = math.clamp(composer.Lookahead.Time, 0f, 1f);
                composer.Lookahead.Smoothing = math.clamp(composer.Lookahead.Smoothing, 0f, 30f);

                composer.Composition.Validate();
            }
        }

        private struct CMPositionComposerMixer : IMixer<CMPositionComposerBlend>
        {
            public CMPositionComposerBlend Lerp(in CMPositionComposerBlend a, in CMPositionComposerBlend b, in float s)
            {
                return new CMPositionComposerBlend
                {
                    CameraDistance = math.lerp(a.CameraDistance, b.CameraDistance, s),
                    DeadZoneDepth = math.lerp(a.DeadZoneDepth, b.DeadZoneDepth, s),
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

            public CMPositionComposerBlend Add(in CMPositionComposerBlend a, in CMPositionComposerBlend b)
            {
                return new CMPositionComposerBlend
                {
                    CameraDistance = a.CameraDistance + b.CameraDistance,
                    DeadZoneDepth = a.DeadZoneDepth + b.DeadZoneDepth,
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
