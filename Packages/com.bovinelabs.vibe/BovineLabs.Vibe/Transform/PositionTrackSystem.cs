// <copyright file="PositionTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe
{
    using BovineLabs.Core.Jobs;
    using BovineLabs.Core.Utility;
#if BL_REACTION
    using BovineLabs.Reaction.Data.Core;
#endif
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary>
    /// Manages position-based timeline track animation with blending, offsetting, and reset capabilities.
    /// </summary>
    [UpdateAfter(typeof(LocalTransformTrackSystem))]
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct PositionTrackSystem : ISystem
    {
        private TrackBlendImpl<float3, PositionAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PositionClipData>();
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
            new TrackDeactivateJob { LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>() }.Schedule();

            var clipActivateQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<PositionAnimated>()
                .WithAll<PositionClipData, LocalTransformClipInitial>()
                .WithAll<Clip, ClipActive>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                PositionAnimatedHandle = SystemAPI.GetComponentTypeHandle<PositionAnimated>(),
                LocalTransformClipInitialHandle = SystemAPI.GetComponentTypeHandle<LocalTransformClipInitial>(true),
                PositionClipDataHandle = SystemAPI.GetComponentTypeHandle<PositionClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                DirectorRootHandle = SystemAPI.GetComponentTypeHandle<DirectorRoot>(true),
                LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
                LocalTransformInitials = SystemAPI.GetComponentLookup<LocalTransformInitial>(true),
#if BL_REACTION
                Targets = SystemAPI.GetComponentLookup<Targets>(true),
                TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
#endif
            }.ScheduleParallel(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<PositionAnimated>()
                .WithAll<PositionClipData, LocalTransformClipInitial>()
                .WithAll<Clip, ClipActive, DirectorRoot>()
                .WithAll<LocalTime, TimeTransform>()
                .Build();

            state.Dependency = new ClipUpdateJob
            {
                PositionAnimatedHandle = SystemAPI.GetComponentTypeHandle<PositionAnimated>(),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                PositionClipDataHandle = SystemAPI.GetComponentTypeHandle<PositionClipData>(true),
                DirectorRootHandle = SystemAPI.GetComponentTypeHandle<DirectorRoot>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TimeTransformHandle = SystemAPI.GetComponentTypeHandle<TimeTransform>(true),
                LocalTransformClipInitialHandle = SystemAPI.GetComponentTypeHandle<LocalTransformClipInitial>(true),
                LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
#if BL_REACTION
                Targets = SystemAPI.GetComponentLookup<Targets>(true),
                TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
#endif
                LocalTransformInitials = SystemAPI.GetComponentLookup<LocalTransformInitial>(true),
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static float3 Offset(ref PositionClipBlob data, LocalTransform transform)
        {
            TransformSpace space;
            float3 value;

            switch (data.Type)
            {
                case PositionType.Offset:
                    space = data.Offset.Space;
                    value = data.Offset.Value;
                    break;
#if BL_REACTION
                case PositionType.Target:
                    space = data.Target.Space;
                    value = data.Target.Offset;
                    break;
#endif
                default:
                    return transform.Position;
            }

            var offset = space switch
            {
                TransformSpace.World => value,
                TransformSpace.Local => transform.TransformPoint(value),
                _ => float3.zero,
            };

            return transform.Position + offset;
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))] // Currently the only deactivate task is resetting
        [WithDisabled(typeof(TimelineActive))]
        [WithAll(typeof(TimelineActivePrevious))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            public ComponentLookup<LocalTransform> LocalTransforms;

            private void Execute(in LocalTransformInitial localTransformInitial, in TrackBinding trackBinding)
            {
                if (!this.LocalTransforms.TryGetRefRW(trackBinding.Value, out var localTransform))
                {
                    return;
                }

                localTransform.ValueRW.Position = localTransformInitial.Value.Position;
            }
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<PositionAnimated> PositionAnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTransformClipInitial> LocalTransformClipInitialHandle;

            [ReadOnly]
            public ComponentTypeHandle<PositionClipData> PositionClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<DirectorRoot> DirectorRootHandle;

            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransforms;

            [ReadOnly]
            public ComponentLookup<LocalTransformInitial> LocalTransformInitials;

#if BL_REACTION
            [ReadOnly]
            public ComponentLookup<Targets> Targets;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;
#endif

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var positionAnimateds = (PositionAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.PositionAnimatedHandle);
                var localTransformClipInitials = (LocalTransformClipInitial*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTransformClipInitialHandle);
                var positionClipDatas = (PositionClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.PositionClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var directorRoots = (DirectorRoot*)chunk.GetRequiredComponentDataPtrRO(ref this.DirectorRootHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInQuery))
                {
                    ref var positionClipData = ref (positionClipDatas + entityIndexInQuery)->Value.Value;
                    ref var positionAnimated = ref *(positionAnimateds + entityIndexInQuery);

                    // Are we using the transform when track was activated or clip activated
                    var transform = ClipTransformSelection.SelectLocalTransform(positionClipData.TransformOnClipActivation,
                        localTransformClipInitials[entityIndexInQuery].Value, ref this.LocalTransformInitials, clips[entityIndexInQuery]);

                    positionAnimated.Value = positionClipData.Type switch
                    {
                        PositionType.Initial => ClipTransformSelection.GetTrackLocalTransform(ref this.LocalTransformInitials, clips[entityIndexInQuery]).Position,
                        PositionType.World => positionClipData.World.Position,
                        PositionType.Offset => Offset(ref positionClipData, transform),
#if BL_REACTION
                        PositionType.Target => this.TargetPosition(directorRoots[entityIndexInQuery], ref positionClipData, transform),
#endif
                        _ => transform.Position,
                    };
                }
            }

#if BL_REACTION
            private float3 TargetPosition(in DirectorRoot director, ref PositionClipBlob data, in LocalTransform transform)
            {
                if (!data.Target.FixedPosition)
                {
                    // Just update it in Update
                    return transform.Position;
                }

                if (!TargetResolver.TryResolveLocalTransform(director, data.Target.Target, ref this.Targets, ref this.TargetsCustoms, ref this.LocalTransforms,
                    out var targetTransform))
                {
                    return transform.Position;
                }

                return Offset(ref data, targetTransform);
            }
#endif
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<PositionAnimated> PositionAnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<PositionClipData> PositionClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<DirectorRoot> DirectorRootHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TimeTransform> TimeTransformHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTransformClipInitial> LocalTransformClipInitialHandle;

            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransforms;

#if BL_REACTION
            [ReadOnly]
            public ComponentLookup<Targets> Targets;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;
#endif

            [ReadOnly]
            public ComponentLookup<LocalTransformInitial> LocalTransformInitials;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var positionAnimateds = (PositionAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.PositionAnimatedHandle);
                var positionClipDatas = (PositionClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.PositionClipDataHandle);
                var directorRoots = (DirectorRoot*)chunk.GetRequiredComponentDataPtrRO(ref this.DirectorRootHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var timeTransforms = (TimeTransform*)chunk.GetRequiredComponentDataPtrRO(ref this.TimeTransformHandle);
                var localTransformClipInitials = (LocalTransformClipInitial*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTransformClipInitialHandle);

                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInQuery))
                {
                    ref var positionAnimated = ref *(positionAnimateds + entityIndexInQuery);
                    ref var director = ref *(directorRoots + entityIndexInQuery);
                    ref var localTime = ref *(localTimes + entityIndexInQuery);
                    var clip = *(clips + entityIndexInQuery);
                    var timeTransform = *(timeTransforms + entityIndexInQuery);
                    var localTransformClipInitial = *(localTransformClipInitials + entityIndexInQuery);

                    ref var data = ref (positionClipDatas + entityIndexInQuery)->Value.Value;

                    positionAnimated.Value = data.Type switch
                    {
#if BL_REACTION
                        PositionType.Target => this.TargetPosition(director, ref data, positionAnimated.Value),
#endif
                        PositionType.Curve => this.CurvePosition(ref data, localTime, clip, localTransformClipInitial, ref clipBlobCaches[entityIndexInQuery]),
                        PositionType.Shake => this.ShakePosition(ref data, clip, localTransformClipInitial, (float)localTime.Value,
                            timeTransform, ref clipBlobCaches[entityIndexInQuery]),
                        PositionType.Spring => this.SpringPosition(ref data, director, clip, localTransformClipInitial, (float)localTime.Value),
                        PositionType.Orbit => this.OrbitPosition(ref data, director, clip, localTransformClipInitial, (float)localTime.Value,
                            ref clipBlobCaches[entityIndexInQuery]),
                        PositionType.Wiggle => this.WigglePosition(ref data, clip, localTransformClipInitial, (float)localTime.Value, timeTransform,
                            ref clipBlobCaches[entityIndexInQuery]),
                        _ => positionAnimated.Value,
                    };
                }
            }

#if BL_REACTION
            private float3 TargetPosition(in DirectorRoot director, ref PositionClipBlob data, in float3 currentPosition)
            {
                if (data.Target.FixedPosition || !TargetResolver.TryResolveLocalTransform(director, data.Target.Target, ref this.Targets,
                    ref this.TargetsCustoms, ref this.LocalTransforms, out var targetTransform))
                {
                    return currentPosition;
                }

                return Offset(ref data, targetTransform);
            }
#endif

            private float3 CurvePosition(
                ref PositionClipBlob data, in LocalTime localTime, in Clip clip, in LocalTransformClipInitial localTransformClipInitial,
                ref ClipBlobCurveCache clipCurveCache)
            {
                ref var curve = ref data.Curve;

                var baseTransform = data.TransformOnClipActivation ? localTransformClipInitial.Value : this.GetInitialTransform(clip);
                var position = baseTransform.Position;

                var time = (float)localTime.Value;

                if (curve.CurveX.IsCreated)
                {
                    var offset = curve.CurveX.Evaluate(time, ref clipCurveCache.Cache0);
                    position.x += offset;
                }

                if (curve.CurveY.IsCreated)
                {
                    var offset = curve.CurveY.Evaluate(time, ref clipCurveCache.Cache1);
                    position.y += offset;
                }

                if (curve.CurveZ.IsCreated)
                {
                    var offset = curve.CurveZ.Evaluate(time, ref clipCurveCache.Cache2);
                    position.z += offset;
                }

                return position;
            }

            private LocalTransform GetInitialTransform(Clip clip)
            {
                return ClipTransformSelection.GetTrackLocalTransform(ref this.LocalTransformInitials, clip);
            }

            private float3 ShakePosition(
                ref PositionClipBlob data, in Clip clip, in LocalTransformClipInitial localTransformClipInitial, float time,
                in TimeTransform timeTransform, ref ClipBlobCurveCache clipCurveCache)
            {
                ref var shake = ref data.Shake;

                var baseTransform = data.TransformOnClipActivation ? localTransformClipInitial.Value : this.GetInitialTransform(clip);

                var attenuation = EvaluateShakeAttenuation(ref shake, time, timeTransform, ref clipCurveCache);
                var amplitudeScale = ClipCurveUtility.EvaluateNormalized(ref shake.AmplitudeCurve, time, timeTransform, ref clipCurveCache.Cache1);
                var amplitude = shake.Amplitude * (attenuation * amplitudeScale);

                if (math.all(math.abs(amplitude) <= new float3(math.FLT_MIN_NORMAL)))
                {
                    return baseTransform.Position;
                }

                var seed = shake.Seed != 0 ? shake.Seed : GlobalRandom.NextUInt(1u, uint.MaxValue);
                var frequencyScale = ClipCurveUtility.EvaluateNormalized(ref shake.FrequencyCurve, time, timeTransform, ref clipCurveCache.Cache2);
                var frequency = math.max(0f, shake.Frequency * frequencyScale);
                var perAxisFrequency = frequency * shake.PerAxisFrequencyMultiplier;
                var offset = new float3(amplitude.x * ShakeUtility.Sample(time, perAxisFrequency.x, shake.Damping, seed),
                    amplitude.y * ShakeUtility.Sample(time, perAxisFrequency.y, shake.Damping, seed, 1),
                    amplitude.z * ShakeUtility.Sample(time, perAxisFrequency.z, shake.Damping, seed, 2));

                if (shake.Space == TransformSpace.Local)
                {
                    offset = math.rotate(baseTransform.Rotation, offset);
                }

                return baseTransform.Position + offset;
            }

            private static float EvaluateShakeAttenuation(
                ref PositionClipBlob.ShakeData shake, float localTime, in TimeTransform timeTransform, ref ClipBlobCurveCache clipCurveCache)
            {
                return ClipCurveUtility.EvaluateNormalized(ref shake.AttenuationCurve, localTime, timeTransform, ref clipCurveCache.Cache0);
            }

            private float3 SpringPosition(
                ref PositionClipBlob data, in DirectorRoot director, in Clip clip, in LocalTransformClipInitial localTransformClipInitial, float time)
            {
                ref var spring = ref data.Spring;

                var baseTransform = data.TransformOnClipActivation ? localTransformClipInitial.Value : this.GetInitialTransform(clip);

                var hasTarget = false;
                var targetTransform = default(LocalTransform);
#if BL_REACTION
                hasTarget = spring.Target != Target.None;
                if (hasTarget)
                {
                    hasTarget = TargetResolver.TryResolveLocalTransform(director, spring.Target, ref this.Targets, ref this.TargetsCustoms,
                        ref this.LocalTransforms, out targetTransform);

                    if (!hasTarget)
                    {
                        return baseTransform.Position;
                    }
                }
#endif

                var restPoint = ResolveSpringRestPoint(in spring, baseTransform, targetTransform, hasTarget);
                var amplitude = baseTransform.Position - restPoint;
                var velocity = ResolveSpringVelocity(in spring, baseTransform, targetTransform, hasTarget);

                var amplitudeAbs = math.abs(amplitude);
                var velocityAbs = math.abs(velocity);
                if (math.all(amplitudeAbs <= new float3(math.FLT_MIN_NORMAL)) && math.all(velocityAbs <= new float3(math.FLT_MIN_NORMAL)))
                {
                    return baseTransform.Position;
                }

                var offset = SpringUtility.Sample(time, spring.Frequency, spring.Damping, amplitude, velocity);
                return restPoint + offset;
            }

            private static float3 ResolveSpringRestPoint(
                in PositionClipBlob.SpringData spring, in LocalTransform baseTransform, in LocalTransform targetTransform, bool hasTarget)
            {
                return spring.Mode switch
                {
                    PositionClipBlob.SpringData.PositionSpringMode.MoveTo => ResolveMoveToRestPoint(in spring, baseTransform, targetTransform, hasTarget),
                    PositionClipBlob.SpringData.PositionSpringMode.MoveToAdditive => ResolveMoveToAdditiveRestPoint(in spring, baseTransform, targetTransform,
                        hasTarget),
                    _ => ResolveBumpRestPoint(in spring, baseTransform, targetTransform, hasTarget),
                };
            }

            private static float3 ResolveMoveToRestPoint(
                in PositionClipBlob.SpringData spring, in LocalTransform baseTransform, in LocalTransform targetTransform, bool hasTarget)
            {
                if (spring.Space == TransformSpace.Local)
                {
                    var reference = hasTarget ? targetTransform : baseTransform;
                    return reference.TransformPoint(spring.RestPoint);
                }

                if (hasTarget)
                {
                    return targetTransform.Position + spring.RestPoint;
                }

                return spring.RestPoint;
            }

            private static float3 ResolveMoveToAdditiveRestPoint(
                in PositionClipBlob.SpringData spring, in LocalTransform baseTransform, in LocalTransform targetTransform, bool hasTarget)
            {
                var reference = hasTarget ? targetTransform : baseTransform;
                var offset = spring.Space == TransformSpace.Local ? reference.TransformPoint(spring.RestPoint) - reference.Position : spring.RestPoint;

                return reference.Position + offset;
            }

            private static float3 ResolveBumpRestPoint(
                in PositionClipBlob.SpringData spring, in LocalTransform baseTransform, in LocalTransform targetTransform, bool hasTarget)
            {
                if (spring.Space == TransformSpace.Local)
                {
                    var reference = hasTarget ? targetTransform : baseTransform;
                    return reference.TransformPoint(spring.RestPoint);
                }

                var referencePosition = hasTarget ? targetTransform.Position : baseTransform.Position;
                return referencePosition + spring.RestPoint;
            }

            private static float3 ResolveSpringVelocity(
                in PositionClipBlob.SpringData spring, in LocalTransform baseTransform, in LocalTransform targetTransform, bool hasTarget)
            {
                if (spring.Space == TransformSpace.Local)
                {
                    var reference = hasTarget ? targetTransform : baseTransform;
                    return math.rotate(reference.Rotation, spring.InitialVelocity);
                }

                return spring.InitialVelocity;
            }

            private float3 OrbitPosition(
                ref PositionClipBlob data, in DirectorRoot director, in Clip clip, in LocalTransformClipInitial localTransformClipInitial, float time,
                ref ClipBlobCurveCache clipCurveCache)
            {
                ref var orbit = ref data.Orbit;

                var baseTransform = data.TransformOnClipActivation ? localTransformClipInitial.Value : this.GetInitialTransform(clip);

                var pivotTransform = baseTransform;
#if BL_REACTION
                if (orbit.Target != Target.None && TargetResolver.TryResolveLocalTransform(director, orbit.Target, ref this.Targets, ref this.TargetsCustoms,
                    ref this.LocalTransforms, out var targetTransform))
                {
                    pivotTransform = targetTransform;
                }
#endif

                var axis = TransformDirection(pivotTransform, orbit.Axis, orbit.AxisSpace);
                axis = math.normalizesafe(axis, math.up());
                if (math.lengthsq(axis) <= math.FLT_MIN_NORMAL)
                {
                    axis = math.up();
                }

                var center = pivotTransform.Position + TransformOffset(pivotTransform, orbit.PivotOffset, orbit.PivotSpace);

                var baseOffset = orbit.UseCustomInitialOffset
                    ? TransformDirection(pivotTransform, orbit.InitialOffset, orbit.InitialOffsetSpace)
                    : baseTransform.Position - center;

                var offsetDirection = math.normalizesafe(baseOffset, GetPerpendicular(axis));
                var baseRadius = math.length(baseOffset);
                float radius;

                if (orbit.UseCustomInitialOffset)
                {
                    radius = baseRadius;
                }
                else
                {
                    radius = orbit.Radius > 0f ? orbit.Radius : baseRadius;
                }

                if (orbit.RadiusCurve.IsCreated)
                {
                    radius += orbit.RadiusCurve.Evaluate(time, ref clipCurveCache.Cache0);
                }

                if (radius <= math.FLT_MIN_NORMAL)
                {
                    return center;
                }

                var offset = offsetDirection * radius;

                var angle = orbit.AngularSpeed * time;
                if (orbit.AngleCurve.IsCreated)
                {
                    angle += orbit.AngleCurve.Evaluate(time, ref clipCurveCache.Cache1);
                }

                var rotation = quaternion.AxisAngle(axis, angle);
                return center + math.rotate(rotation, offset);
            }

            private float3 WigglePosition(
                ref PositionClipBlob data, in Clip clip, in LocalTransformClipInitial localTransformClipInitial, float time, in TimeTransform timeTransform,
                ref ClipBlobCurveCache clipCurveCache)
            {
                ref var wiggle = ref data.Wiggle;

                var baseTransform = data.TransformOnClipActivation ? localTransformClipInitial.Value : this.GetInitialTransform(clip);

                var amplitudeScale = ClipCurveUtility.EvaluateNormalized(ref wiggle.AmplitudeCurve, time, timeTransform, ref clipCurveCache.Cache0);
                var amplitude = wiggle.Amplitude * amplitudeScale;
                var amplitudeAbs = math.abs(amplitude);
                if (math.all(amplitudeAbs <= new float3(math.FLT_MIN_NORMAL)))
                {
                    return baseTransform.Position;
                }

                var seed = wiggle.Seed != 0 ? wiggle.Seed : GlobalRandom.NextUInt(1u, uint.MaxValue);
                var frequencyScale = ClipCurveUtility.EvaluateNormalized(ref wiggle.FrequencyCurve, time, timeTransform, ref clipCurveCache.Cache1);
                var frequency = math.max(0f, wiggle.Frequency * frequencyScale);
                var perAxisFrequency = frequency * wiggle.PerAxisFrequencyMultiplier;
                var offset = new float3(amplitude.x * WiggleUtility.Sample(time, perAxisFrequency.x, wiggle.Smoothness, seed),
                    amplitude.y * WiggleUtility.Sample(time, perAxisFrequency.y, wiggle.Smoothness, seed, 1),
                    amplitude.z * WiggleUtility.Sample(time, perAxisFrequency.z, wiggle.Smoothness, seed, 2));

                if (wiggle.Space == TransformSpace.Local)
                {
                    offset = math.rotate(baseTransform.Rotation, offset);
                }

                return baseTransform.Position + offset;
            }

            private static float3 TransformOffset(in LocalTransform transform, in float3 value, TransformSpace space)
            {
                return space switch
                {
                    TransformSpace.World => value,
                    TransformSpace.Local => transform.TransformPoint(value) - transform.Position,
                    _ => float3.zero,
                };
            }

            private static float3 TransformDirection(in LocalTransform transform, in float3 value, TransformSpace space)
            {
                return space switch
                {
                    TransformSpace.World => value,
                    TransformSpace.Local => math.rotate(transform.Rotation, value),
                    _ => float3.zero,
                };
            }

            private static float3 GetPerpendicular(float3 axis)
            {
                var reference = math.abs(axis.y) < 0.99f ? new float3(0f, 1f, 0f) : new float3(1f, 0f, 0f);
                var perpendicular = math.cross(axis, reference);
                if (math.lengthsq(perpendicular) <= math.FLT_MIN_NORMAL)
                {
                    perpendicular = new float3(1f, 0f, 0f);
                }

                return math.normalizesafe(perpendicular, new float3(1f, 0f, 0f));
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<float3>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> LocalTransforms;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var target);

                if (!this.LocalTransforms.TryGetRefRW(entity, out var lt))
                {
                    return;
                }

                lt.ValueRW.Position = JobHelpers.Blend<float3, Float3Mixer>(ref target, lt.ValueRO.Position);
            }
        }
    }
}
