// <copyright file="RotationTrackSystem.cs" company="BovineLabs">
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
    /// Manages rotation-based timeline track animation with blending, offsetting, and reset capabilities.
    /// </summary>
    [UpdateAfter(typeof(LocalTransformTrackSystem))]
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct RotationTrackSystem : ISystem
    {
        private TrackBlendImpl<quaternion, RotationAnimated> impl;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RotationClipData>();
            this.impl.OnCreate(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            this.impl.OnDestroy(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var localTransformsRW = SystemAPI.GetComponentLookup<LocalTransform>();

            new TrackDeactivateJob { LocalTransforms = localTransformsRW }.Schedule();

            var clipActivateQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<RotationAnimated>()
                .WithAll<RotationClipData, LocalTransformClipInitial>()
                .WithAll<Clip, ClipActive>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                RotationAnimatedHandle = SystemAPI.GetComponentTypeHandle<RotationAnimated>(),
                LocalTransformClipInitialHandle = SystemAPI.GetComponentTypeHandle<LocalTransformClipInitial>(true),
                RotationClipDataHandle = SystemAPI.GetComponentTypeHandle<RotationClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                DirectorRootHandle = SystemAPI.GetComponentTypeHandle<DirectorRoot>(true),
                LocalTransforms = localTransforms,
                LocalTransformInitials = SystemAPI.GetComponentLookup<LocalTransformInitial>(true),
#if BL_REACTION
                Targets = SystemAPI.GetComponentLookup<Targets>(true),
                TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
#endif
            }.ScheduleParallel(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<RotationAnimated>()
                .WithAll<RotationClipData, LocalTransformClipInitial>()
                .WithAll<Clip, ClipActive, DirectorRoot>()
                .WithAll<LocalTime, TimeTransform>()
                .Build();

            state.Dependency = new ClipUpdateJob
            {
                RotationAnimatedHandle = SystemAPI.GetComponentTypeHandle<RotationAnimated>(),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                RotationClipDataHandle = SystemAPI.GetComponentTypeHandle<RotationClipData>(true),
                DirectorRootHandle = SystemAPI.GetComponentTypeHandle<DirectorRoot>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                TimeTransformHandle = SystemAPI.GetComponentTypeHandle<TimeTransform>(true),
                LocalTransformClipInitialHandle = SystemAPI.GetComponentTypeHandle<LocalTransformClipInitial>(true),
                LocalTransforms = localTransforms,
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
                LocalTransforms = localTransformsRW,
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

#if BL_REACTION
        private static quaternion Offset(in RotationClipBlob.LookAtTargetData data, quaternion rotation)
        {
            var offsetValue = math.normalizesafe(data.Offset);

            var offsetWorld = data.Space switch
            {
                TransformSpace.World => offsetValue,
                TransformSpace.Local => math.mul(rotation, offsetValue),
                _ => quaternion.identity,
            };

            return math.normalize(math.mul(offsetWorld, rotation));
        }

        private static bool TryGetLookAtTargetPosition(
            in DirectorRoot director, in RotationClipBlob.LookAtTargetData lookAtTarget, ref ComponentLookup<Targets> targets,
            ref ComponentLookup<TargetsCustom> targetsCustoms, ref ComponentLookup<LocalTransform> localTransforms, in LocalTransform bindingTransform,
            out float3 targetPosition)
        {
            if (lookAtTarget.Target == Target.None)
            {
                targetPosition = lookAtTarget.AnchorSpace == TransformSpace.World
                    ? lookAtTarget.AnchorPosition
                    : bindingTransform.TransformPoint(lookAtTarget.AnchorPosition);

                return true;
            }

            if (TargetResolver.TryResolveLocalTransform(director, lookAtTarget.Target, ref targets, ref targetsCustoms, ref localTransforms,
                out var targetTransform))
            {
                targetPosition = targetTransform.Position;
                return true;
            }

            targetPosition = float3.zero;
            return false;
        }
#endif

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))]
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

                localTransform.ValueRW.Rotation = localTransformInitial.Value.Rotation;
            }
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<RotationAnimated> RotationAnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTransformClipInitial> LocalTransformClipInitialHandle;

            [ReadOnly]
            public ComponentTypeHandle<RotationClipData> RotationClipDataHandle;

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
                var rotationAnimateds = (RotationAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.RotationAnimatedHandle);
                var localTransformClipInitials = (LocalTransformClipInitial*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTransformClipInitialHandle);
                var rotationClipDatas = (RotationClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.RotationClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var directorRoots = (DirectorRoot*)chunk.GetRequiredComponentDataPtrRO(ref this.DirectorRootHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInQuery))
                {
                    ref var rotationClipData = ref (rotationClipDatas + entityIndexInQuery)->Value.Value;
                    ref var rotationAnimated = ref rotationAnimateds[entityIndexInQuery];

                    // Are we using the transform when track was activated or clip activated
                    var transform = ClipTransformSelection.SelectLocalTransform(rotationClipData.TransformOnClipActivation,
                        localTransformClipInitials[entityIndexInQuery].Value, ref this.LocalTransformInitials, clips[entityIndexInQuery]);

                    rotationAnimated.Value = rotationClipData.Type switch
                    {
                        RotationType.Initial => ClipTransformSelection.GetTrackLocalTransform(ref this.LocalTransformInitials, clips[entityIndexInQuery])
                            .Rotation,
#if BL_REACTION
                        RotationType.LookAtTarget => this.LookAtTarget(directorRoots[entityIndexInQuery], ref rotationClipData, transform),
#endif
                        RotationType.LookAtStart => ClipTransformSelection.GetTrackLocalTransform(ref this.LocalTransformInitials, clips[entityIndexInQuery])
                            .Rotation,
                        RotationType.LookInDirection => DirectionRotation(rotationClipData.LookInDirection, transform),
                        RotationType.LookAtRotation => RotationValue(rotationClipData.LookAtRotation, transform),
                        _ => transform.Rotation,
                    };
                }
            }

#if BL_REACTION
            private quaternion LookAtTarget(in DirectorRoot director, ref RotationClipBlob data, in LocalTransform transform)
            {
                ref var lookAtTarget = ref data.LookAtTarget;

                if (!lookAtTarget.FixedRotation)
                {
                    return transform.Rotation;
                }

                if (!TryGetLookAtTargetPosition(director, lookAtTarget, ref this.Targets, ref this.TargetsCustoms, ref this.LocalTransforms, transform,
                    out var targetPosition))
                {
                    return transform.Rotation;
                }

                var direction = targetPosition - transform.Position;
                var rotation = quaternion.LookRotationSafe(direction, math.up());
                return Offset(lookAtTarget, rotation);
            }
#endif

            private static quaternion DirectionRotation(in RotationClipBlob.LookInDirectionData data, in LocalTransform bindingTransform)
            {
                var direction = data.Direction;
                if (math.lengthsq(direction) < math.FLT_MIN_NORMAL)
                {
                    return bindingTransform.Rotation;
                }

                if (data.Space == TransformSpace.Local)
                {
                    direction = math.rotate(bindingTransform.Rotation, direction);
                }

                return quaternion.LookRotationSafe(direction, math.up());
            }

            private static quaternion RotationValue(in RotationClipBlob.LookAtRotationData data, in LocalTransform bindingTransform)
            {
                var rotation = data.Rotation;
                if (data.Space == TransformSpace.Local)
                {
                    rotation = math.mul(bindingTransform.Rotation, rotation);
                }

                return math.normalize(rotation);
            }
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<RotationAnimated> RotationAnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<RotationClipData> RotationClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<DirectorRoot> DirectorRootHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

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
                var rotationAnimateds = (RotationAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.RotationAnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var rotationClipDatas = (RotationClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.RotationClipDataHandle);
                var directorRoots = (DirectorRoot*)chunk.GetRequiredComponentDataPtrRO(ref this.DirectorRootHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);
                var timeTransforms = (TimeTransform*)chunk.GetRequiredComponentDataPtrRO(ref this.TimeTransformHandle);
                var localTransformClipInitials = (LocalTransformClipInitial*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTransformClipInitialHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInQuery))
                {
                    ref var rotationAnimated = ref rotationAnimateds[entityIndexInQuery];
                    ref var director = ref directorRoots[entityIndexInQuery];
                    var clip = clips[entityIndexInQuery];
                    ref var localTime = ref localTimes[entityIndexInQuery];
                    var timeTransform = timeTransforms[entityIndexInQuery];
                    var localTransformClipInitial = localTransformClipInitials[entityIndexInQuery];

                    ref var data = ref rotationClipDatas[entityIndexInQuery].Value.Value;

                    rotationAnimated.Value = data.Type switch
                    {
#if BL_REACTION
                        RotationType.LookAtTarget => this.LookAtTarget(director, clip, localTransformClipInitial, ref data, rotationAnimated.Value),
#endif
                        RotationType.Shake => this.ShakeRotation(ref data, clip, localTransformClipInitial, (float)localTime.Value,
                            timeTransform, ref clipBlobCaches[entityIndexInQuery]),
                        RotationType.Spring => this.SpringRotation(ref data, clip, localTransformClipInitial, (float)localTime.Value),
                        RotationType.Wiggle => this.WiggleRotation(ref data, clip, localTransformClipInitial, (float)localTime.Value, timeTransform,
                            ref clipBlobCaches[entityIndexInQuery]),
                        _ => rotationAnimated.Value,
                    };
                }
            }

#if BL_REACTION
            private quaternion LookAtTarget(
                in DirectorRoot director, in Clip clip, in LocalTransformClipInitial localTransformClipInitial, ref RotationClipBlob data,
                in quaternion currentRotation)
            {
                ref var lookAtTarget = ref data.LookAtTarget;

                if (lookAtTarget.FixedRotation)
                {
                    return currentRotation;
                }

                var bindingTransform = data.TransformOnClipActivation ? localTransformClipInitial.Value : this.GetInitialTransform(clip);

                if (!TryGetLookAtTargetPosition(director, lookAtTarget, ref this.Targets, ref this.TargetsCustoms, ref this.LocalTransforms, bindingTransform,
                    out var targetPosition))
                {
                    return currentRotation;
                }

                var direction = targetPosition - bindingTransform.Position;
                if (math.lengthsq(direction) <= math.FLT_MIN_NORMAL)
                {
                    return currentRotation;
                }

                var rotation = quaternion.LookRotationSafe(direction, math.up());
                return Offset(lookAtTarget, rotation);
            }
#endif

            private quaternion ShakeRotation(
                ref RotationClipBlob data, in Clip clip, in LocalTransformClipInitial localTransformClipInitial, float time,
                in TimeTransform timeTransform, ref ClipBlobCurveCache clipCurveCache)
            {
                ref var shake = ref data.Shake;

                var baseTransform = data.TransformOnClipActivation ? localTransformClipInitial.Value : this.GetInitialTransform(clip);

                var baseRotation = baseTransform.Rotation;

                var attenuation = EvaluateShakeAttenuation(ref shake, time, timeTransform, ref clipCurveCache);
                var amplitudeScale = ClipCurveUtility.EvaluateNormalized(ref shake.AmplitudeCurve, time, timeTransform, ref clipCurveCache.Cache1);
                var amplitude = shake.Amplitude * (attenuation * amplitudeScale);
                if (math.all(math.abs(amplitude) <= new float3(math.FLT_MIN_NORMAL)))
                {
                    return baseRotation;
                }

                var seed = shake.Seed != 0 ? shake.Seed : GlobalRandom.NextUInt(1u, uint.MaxValue);
                var frequencyScale = ClipCurveUtility.EvaluateNormalized(ref shake.FrequencyCurve, time, timeTransform, ref clipCurveCache.Cache2);
                var frequency = math.max(0f, shake.Frequency * frequencyScale);
                var perAxisFrequency = frequency * shake.PerAxisFrequencyMultiplier;
                var offsetEuler = new float3(amplitude.x * ShakeUtility.Sample(time, perAxisFrequency.x, shake.Damping, seed),
                    amplitude.y * ShakeUtility.Sample(time, perAxisFrequency.y, shake.Damping, seed, 1),
                    amplitude.z * ShakeUtility.Sample(time, perAxisFrequency.z, shake.Damping, seed, 2));

                var offset = quaternion.Euler(math.radians(offsetEuler));

                return shake.Space == TransformSpace.Local ? math.normalize(math.mul(baseRotation, offset)) : math.normalize(math.mul(offset, baseRotation));
            }

            private static float EvaluateShakeAttenuation(
                ref RotationClipBlob.ShakeData shake, float localTime, in TimeTransform timeTransform, ref ClipBlobCurveCache clipCurveCache)
            {
                return ClipCurveUtility.EvaluateNormalized(ref shake.AttenuationCurve, localTime, timeTransform, ref clipCurveCache.Cache0);
            }

            private quaternion SpringRotation(ref RotationClipBlob data, in Clip clip, in LocalTransformClipInitial localTransformClipInitial, float time)
            {
                ref var spring = ref data.Spring;

                var baseTransform = data.TransformOnClipActivation ? localTransformClipInitial.Value : this.GetInitialTransform(clip);
                var baseRotation = baseTransform.Rotation;

                var restRotation = math.normalize(math.mul(baseRotation, quaternion.Euler(math.radians(spring.RestEuler))));

                var deltaRotation = math.mul(math.inverse(restRotation), baseRotation);
                var amplitude = math.degrees(math.Euler(deltaRotation));
                var velocity = spring.InitialVelocity;

                var amplitudeAbs = math.abs(amplitude);
                var velocityAbs = math.abs(velocity);
                if (math.all(amplitudeAbs <= new float3(math.FLT_MIN_NORMAL)) && math.all(velocityAbs <= new float3(math.FLT_MIN_NORMAL)))
                {
                    return baseRotation;
                }

                var offsetEuler = SpringUtility.Sample(time, spring.Frequency, spring.Damping, amplitude, velocity);
                var offset = quaternion.Euler(math.radians(offsetEuler));

                return math.normalize(math.mul(restRotation, offset));
            }

            private LocalTransform GetInitialTransform(Clip clip)
            {
                return ClipTransformSelection.GetTrackLocalTransform(ref this.LocalTransformInitials, clip);
            }

            private quaternion WiggleRotation(
                ref RotationClipBlob data, in Clip clip, in LocalTransformClipInitial localTransformClipInitial, float time, in TimeTransform timeTransform,
                ref ClipBlobCurveCache clipCurveCache)
            {
                ref var wiggle = ref data.Wiggle;

                var baseTransform = data.TransformOnClipActivation ? localTransformClipInitial.Value : this.GetInitialTransform(clip);
                var baseRotation = baseTransform.Rotation;

                var amplitudeScale = ClipCurveUtility.EvaluateNormalized(ref wiggle.AmplitudeCurve, time, timeTransform, ref clipCurveCache.Cache0);
                var amplitude = wiggle.Amplitude * amplitudeScale;
                var amplitudeAbs = math.abs(amplitude);
                if (math.all(amplitudeAbs <= new float3(math.FLT_MIN_NORMAL)))
                {
                    return baseRotation;
                }

                var seed = wiggle.Seed != 0 ? wiggle.Seed : GlobalRandom.NextUInt(1u, uint.MaxValue);
                var frequencyScale = ClipCurveUtility.EvaluateNormalized(ref wiggle.FrequencyCurve, time, timeTransform, ref clipCurveCache.Cache1);
                var frequency = math.max(0f, wiggle.Frequency * frequencyScale);
                var perAxisFrequency = frequency * wiggle.PerAxisFrequencyMultiplier;
                var offsetEuler = new float3(amplitude.x * WiggleUtility.Sample(time, perAxisFrequency.x, wiggle.Smoothness, seed),
                    amplitude.y * WiggleUtility.Sample(time, perAxisFrequency.y, wiggle.Smoothness, seed, 1),
                    amplitude.z * WiggleUtility.Sample(time, perAxisFrequency.z, wiggle.Smoothness, seed, 2));

                var offset = quaternion.Euler(math.radians(offsetEuler));

                return wiggle.Space == TransformSpace.Local ? math.normalize(math.mul(baseRotation, offset)) : math.normalize(math.mul(offset, baseRotation));
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<quaternion>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> LocalTransforms;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var target);

                if (!this.LocalTransforms.TryGetRefRW(entity, out var localTransform))
                {
                    return;
                }

                localTransform.ValueRW.Rotation = JobHelpers.Blend<quaternion, QuaternionMixer>(ref target, localTransform.ValueRO.Rotation);
            }
        }
    }
}
