// <copyright file="CMFollowTrackSystem.cs" company="BovineLabs">
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
    using Unity.Cinemachine.TargetTracking;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Evaluates Cinemachine follow timeline tracks and applies their blended results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMFollowTrackSystem : ISystem
    {
        private TrackLifeImpl<CMFollow, CMFollowInitial> lifeImpl;
        private TrackBlendImpl<CMFollowBlend, CMFollowAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMFollowClipData>();
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

            var clipActivateQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<CMFollowAnimated>()
                .WithAll<CMFollowClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMFollowAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMFollowClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Follows = SystemAPI.GetComponentLookup<CMFollow>(),
                Initials = SystemAPI.GetComponentLookup<CMFollowInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Follows = SystemAPI.GetComponentLookup<CMFollow>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static void ApplyBindingModeConstraints(ref CMFollow follow)
        {
            follow.TrackerSettings.Validate();
            follow.FollowOffset = ApplyOffsetConstraints(follow.FollowOffset, follow.TrackerSettings.BindingMode);
        }

        private static float3 ApplyOffsetConstraints(in float3 offset, BindingMode bindingMode)
        {
            if (bindingMode != BindingMode.LazyFollow)
            {
                return offset;
            }

            var sanitized = offset;
            sanitized.x = 0f;
            sanitized.z = -math.abs(sanitized.z);
            return sanitized;
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMFollowAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMFollowClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            public ComponentLookup<CMFollow> Follows;

            [ReadOnly]
            public ComponentLookup<CMFollowInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMFollowAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMFollowClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var trackBindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly CMFollowClipData clipData = ref clipDatas[entityIndexInChunk];
                    ref readonly var trackBinding = ref trackBindings[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        CMFollowClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMFollowClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, clipData),
                        _ => animated.Value,
                    };
                }
            }

            private CMFollowBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];

                return new CMFollowBlend
                {
                    FollowOffset = initial.Value.FollowOffset,
                    PositionDamping = initial.Value.TrackerSettings.PositionDamping,
                    RotationDamping = initial.Value.TrackerSettings.RotationDamping,
                    QuaternionDamping = initial.Value.TrackerSettings.QuaternionDamping,
                };
            }

            private CMFollowBlend SelectAnimatedValue(Entity boundEntity, in CMFollowClipData data)
            {
                if (this.Follows.TryGetRefRW(boundEntity, out var follow))
                {
                    ref var tracker = ref follow.ValueRW.TrackerSettings;
                    tracker.BindingMode = data.TrackerSettings.BindingMode;
                    tracker.AngularDampingMode = data.TrackerSettings.AngularDampingMode;
                }

                return new CMFollowBlend
                {
                    FollowOffset = data.FollowOffset,
                    PositionDamping = data.TrackerSettings.PositionDamping,
                    RotationDamping = data.TrackerSettings.RotationDamping,
                    QuaternionDamping = data.TrackerSettings.QuaternionDamping,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMFollowBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMFollow> Follows;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Follows.TryGetRefRW(entity, out var follow))
                {
                    return;
                }

                ref var followValue = ref follow.ValueRW;
                var tracker = followValue.TrackerSettings;
                var current = new CMFollowBlend
                {
                    FollowOffset = followValue.FollowOffset,
                    PositionDamping = math.float3(tracker.PositionDamping),
                    RotationDamping = math.float3(tracker.RotationDamping),
                    QuaternionDamping = tracker.QuaternionDamping,
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(CMFollowMixer));
                ApplyBlend(ref followValue, in blended);
            }

            private static void ApplyBlend(ref CMFollow follow, in CMFollowBlend blend)
            {
                follow.FollowOffset = blend.FollowOffset;
                follow.TrackerSettings.PositionDamping = blend.PositionDamping;
                follow.TrackerSettings.RotationDamping = blend.RotationDamping;
                follow.TrackerSettings.QuaternionDamping = blend.QuaternionDamping;

                ApplyBindingModeConstraints(ref follow);
            }
        }

        private struct CMFollowMixer : IMixer<CMFollowBlend>
        {
            public CMFollowBlend Lerp(in CMFollowBlend a, in CMFollowBlend b, in float s)
            {
                return new CMFollowBlend
                {
                    FollowOffset = math.lerp(a.FollowOffset, b.FollowOffset, s),
                    PositionDamping = math.lerp(a.PositionDamping, b.PositionDamping, s),
                    RotationDamping = math.lerp(a.RotationDamping, b.RotationDamping, s),
                    QuaternionDamping = math.lerp(a.QuaternionDamping, b.QuaternionDamping, s),
                };
            }

            public CMFollowBlend Add(in CMFollowBlend a, in CMFollowBlend b)
            {
                return new CMFollowBlend
                {
                    FollowOffset = a.FollowOffset + b.FollowOffset,
                    PositionDamping = a.PositionDamping + b.PositionDamping,
                    RotationDamping = a.RotationDamping + b.RotationDamping,
                    QuaternionDamping = a.QuaternionDamping + b.QuaternionDamping,
                };
            }
        }
    }
}
#endif
