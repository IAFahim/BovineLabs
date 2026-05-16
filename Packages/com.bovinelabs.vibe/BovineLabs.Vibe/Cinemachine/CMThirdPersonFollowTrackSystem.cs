// <copyright file="CMThirdPersonFollowTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
#if UNITY_PHYSICS
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
    /// Blends Cinemachine third person follow timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMThirdPersonFollowTrackSystem : ISystem
    {
        private TrackLifeImpl<CMThirdPersonFollow, CMThirdPersonFollowInitial> lifeImpl;
        private TrackBlendImpl<CMThirdPersonFollowBlend, CMThirdPersonFollowAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMThirdPersonFollowClipData>();
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
                .WithAllRW<CMThirdPersonFollowAnimated>()
                .WithAll<CMThirdPersonFollowClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMThirdPersonFollowAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMThirdPersonFollowClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                ThirdPersonFollows = SystemAPI.GetComponentLookup<CMThirdPersonFollow>(),
                Initials = SystemAPI.GetComponentLookup<CMThirdPersonFollowInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                ThirdPersonFollows = SystemAPI.GetComponentLookup<CMThirdPersonFollow>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static CMThirdPersonFollowBlend CreateBlend(in CMThirdPersonFollow follow)
        {
            return new CMThirdPersonFollowBlend
            {
                Damping = follow.Damping,
                ShoulderOffset = follow.ShoulderOffset,
                VerticalArmLength = follow.VerticalArmLength,
                CameraSide = follow.CameraSide,
                CameraDistance = follow.CameraDistance,
            };
        }

        private static void SanitizeBlend(ref CMThirdPersonFollowBlend blend)
        {
            blend.Damping = math.max(blend.Damping, float3.zero);
            blend.VerticalArmLength = math.max(blend.VerticalArmLength, 0f);
            blend.CameraSide = math.clamp(blend.CameraSide, 0f, 1f);
            blend.CameraDistance = math.max(blend.CameraDistance, 0f);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMThirdPersonFollowAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMThirdPersonFollowClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMThirdPersonFollow> ThirdPersonFollows;

            [ReadOnly]
            public ComponentLookup<CMThirdPersonFollowInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMThirdPersonFollowAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMThirdPersonFollowClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        CMThirdPersonFollowClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMThirdPersonFollowClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, clipData),
                        _ => animated.Value,
                    };
                }
            }

            private CMThirdPersonFollowBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                var blend = CreateBlend(initial.Value);
                SanitizeBlend(ref blend);
                return blend;
            }

            private CMThirdPersonFollowBlend SelectAnimatedValue(Entity boundEntity, in CMThirdPersonFollowClipData data)
            {
                if (this.ThirdPersonFollows.TryGetRefRW(boundEntity, out var follow))
                {
                    follow.ValueRW.AvoidObstacles = data.AvoidObstacles;
                }

                var blend = new CMThirdPersonFollowBlend
                {
                    Damping = data.Damping,
                    ShoulderOffset = data.ShoulderOffset,
                    VerticalArmLength = data.VerticalArmLength,
                    CameraSide = data.CameraSide,
                    CameraDistance = data.CameraDistance,
                };

                SanitizeBlend(ref blend);
                return blend;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMThirdPersonFollowBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMThirdPersonFollow> ThirdPersonFollows;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.ThirdPersonFollows.TryGetRefRW(entity, out var follow))
                {
                    return;
                }

                ref var value = ref follow.ValueRW;
                var current = CreateBlend(value);
                var blended = JobHelpers.Blend(ref mixData, current, default(CMThirdPersonFollowMixer));
                ApplyBlend(ref value, in blended);
                Sanitize(ref value);
            }

            private static void ApplyBlend(ref CMThirdPersonFollow follow, in CMThirdPersonFollowBlend blend)
            {
                follow.Damping = blend.Damping;
                follow.ShoulderOffset = blend.ShoulderOffset;
                follow.VerticalArmLength = blend.VerticalArmLength;
                follow.CameraSide = blend.CameraSide;
                follow.CameraDistance = blend.CameraDistance;
            }

            private static void Sanitize(ref CMThirdPersonFollow follow)
            {
                follow.Damping = math.max(follow.Damping, float3.zero);
                follow.VerticalArmLength = math.max(follow.VerticalArmLength, 0f);
                follow.CameraSide = math.clamp(follow.CameraSide, 0f, 1f);
                follow.CameraDistance = math.max(follow.CameraDistance, 0f);

                var obstacles = follow.AvoidObstacles;
                obstacles.CameraRadius = math.max(obstacles.CameraRadius, 0.001f);
                obstacles.DampingIntoCollision = math.max(obstacles.DampingIntoCollision, 0f);
                obstacles.DampingFromCollision = math.max(obstacles.DampingFromCollision, 0f);
                follow.AvoidObstacles = obstacles;
            }
        }

        private struct CMThirdPersonFollowMixer : IMixer<CMThirdPersonFollowBlend>
        {
            public CMThirdPersonFollowBlend Lerp(in CMThirdPersonFollowBlend a, in CMThirdPersonFollowBlend b, in float s)
            {
                return new CMThirdPersonFollowBlend
                {
                    Damping = math.lerp(a.Damping, b.Damping, s),
                    ShoulderOffset = math.lerp(a.ShoulderOffset, b.ShoulderOffset, s),
                    VerticalArmLength = math.lerp(a.VerticalArmLength, b.VerticalArmLength, s),
                    CameraSide = math.lerp(a.CameraSide, b.CameraSide, s),
                    CameraDistance = math.lerp(a.CameraDistance, b.CameraDistance, s),
                };
            }

            public CMThirdPersonFollowBlend Add(in CMThirdPersonFollowBlend a, in CMThirdPersonFollowBlend b)
            {
                return new CMThirdPersonFollowBlend
                {
                    Damping = a.Damping + b.Damping,
                    ShoulderOffset = a.ShoulderOffset + b.ShoulderOffset,
                    VerticalArmLength = a.VerticalArmLength + b.VerticalArmLength,
                    CameraSide = a.CameraSide + b.CameraSide,
                    CameraDistance = a.CameraDistance + b.CameraDistance,
                };
            }
        }
    }
}
#endif
#endif
