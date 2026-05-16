// <copyright file="NonUniformScaleTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe
{
    using BovineLabs.Core.Jobs;
    using BovineLabs.Core.Utility;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.LocalTransform;
    using BovineLabs.Vibe.Data.NonUniformScale;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary>
    /// Drives non-uniform scaling via <see cref="PostTransformMatrix"/>.
    /// </summary>
    [UpdateAfter(typeof(ScaleTrackSystem))]
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct NonUniformScaleTrackSystem : ISystem
    {
        private const float MinimumStretch = 0.0001f;

        private TrackBlendImpl<float3, NonUniformScaleAnimated> impl;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NonUniformScaleClipData>();
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
            var postTransformsRo = SystemAPI.GetComponentLookup<PostTransformMatrix>(true);
            var postTransformsRw = SystemAPI.GetComponentLookup<PostTransformMatrix>();

            new TrackActivateJob { PostTransforms = postTransformsRo }.ScheduleParallel();
            new TrackDeactivateJob { PostTransforms = postTransformsRw }.Schedule();

            new ClipInitialCaptureJob { PostTransforms = postTransformsRo }.ScheduleParallel();

            var clipActivateQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<NonUniformScaleAnimated>()
                .WithAll<NonUniformScaleClipData, PostTransformMatrixClipInitial>()
                .WithAll<Clip, ClipActive>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<NonUniformScaleAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<NonUniformScaleClipData>(true),
                ClipInitialHandle = SystemAPI.GetComponentTypeHandle<PostTransformMatrixClipInitial>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                PostTransformInitials = SystemAPI.GetComponentLookup<PostTransformMatrixInitial>(true),
            }.ScheduleParallel(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<NonUniformScaleAnimated>()
                .WithAll<NonUniformScaleClipData>()
                .WithAll<LocalTime, TimeTransform>()
                .WithAll<ClipActive>()
                .Build();

            state.Dependency = new ClipUpdateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<NonUniformScaleAnimated>(),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<NonUniformScaleClipData>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                TimeTransformHandle = SystemAPI.GetComponentTypeHandle<TimeTransform>(true),
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                PostTransforms = postTransformsRw,
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static float3 ExtractScale(in PostTransformMatrix matrix)
        {
            var value = matrix.Value;
            return new float3(value.c0.x, value.c1.y, value.c2.z);
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        private partial struct TrackActivateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<PostTransformMatrix> PostTransforms;

            private void Execute(ref PostTransformMatrixInitial initial, in TrackBinding trackBinding)
            {
                initial.Value = this.PostTransforms.TryGetComponent(trackBinding.Value, out var postTransform)
                    ? postTransform
                    : new PostTransformMatrix { Value = float4x4.identity };
            }
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))]
        [WithDisabled(typeof(TimelineActive))]
        [WithAll(typeof(TimelineActivePrevious))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            public ComponentLookup<PostTransformMatrix> PostTransforms;

            private void Execute(in PostTransformMatrixInitial initial, in TrackBinding trackBinding)
            {
                if (this.PostTransforms.TryGetRefRW(trackBinding.Value, out var postTransform))
                {
                    postTransform.ValueRW = initial.Value;
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct ClipInitialCaptureJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<PostTransformMatrix> PostTransforms;

            private void Execute(ref PostTransformMatrixClipInitial initial, in TrackBinding trackBinding)
            {
                initial.Value = this.PostTransforms.TryGetComponent(trackBinding.Value, out var postTransform)
                    ? postTransform
                    : new PostTransformMatrix { Value = float4x4.identity };
            }
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<NonUniformScaleAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<NonUniformScaleClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<PostTransformMatrixClipInitial> ClipInitialHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<PostTransformMatrixInitial> PostTransformInitials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (NonUniformScaleAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (NonUniformScaleClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clipInitials = (PostTransformMatrixClipInitial*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipInitialHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipBlob = ref clipDatas[entityIndexInChunk].Value.Value;
                    var useClipInitial = clipBlob.Type != NonUniformScaleType.Initial && clipBlob.SquashStretch.TransformOnClipActivation;
                    var baseMatrix = ClipTransformSelection.SelectPostTransformMatrix(useClipInitial, clipInitials[entityIndexInChunk].Value,
                        ref this.PostTransformInitials, clips[entityIndexInChunk]);

                    var baseScale = ExtractScale(baseMatrix);
                    if (!math.all(math.isfinite(baseScale)))
                    {
                        baseScale = new float3(1f, 1f, 1f);
                    }

                    animated.BaseScale = baseScale;
                    animated.Value = baseScale;
                }
            }
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<NonUniformScaleAnimated> AnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<NonUniformScaleClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentTypeHandle<TimeTransform> TimeTransformHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (NonUniformScaleAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var clipDatas = (NonUniformScaleClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);
                var timeTransforms = (TimeTransform*)chunk.GetRequiredComponentDataPtrRO(ref this.TimeTransformHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk];
                    ref var localTime = ref localTimes[entityIndexInChunk];
                    var timeTransform = timeTransforms[entityIndexInChunk];

                    ref var blob = ref clipData.Value.Value;

                    switch (blob.Type)
                    {
                        case NonUniformScaleType.SquashStretchCurve:
                        {
                            animated.Value = ExecuteSquashStretch(ref blob, animated.BaseScale, localTime, timeTransform,
                                ref clipBlobCaches[entityIndexInChunk]);

                            break;
                        }

                        case NonUniformScaleType.SquashStretchShake:
                        {
                            animated.Value = ExecuteSquashStretch(ref blob, animated.BaseScale, localTime, timeTransform,
                                ref clipBlobCaches[entityIndexInChunk]);

                            break;
                        }

                        case NonUniformScaleType.SquashStretchAbsolute:
                        {
                            animated.Value = animated.BaseScale * EvaluateMultiplier(blob.SquashStretchAbsolute.Amount, ref blob);
                            break;
                        }

                        case NonUniformScaleType.SquashStretchSpring:
                        {
                            var amount = EvaluateSpring(ref blob.SquashStretchSpring, blob.SquashStretch.Axis, animated.BaseScale, (float)localTime.Value);
                            var multiplier = EvaluateMultiplier(amount, ref blob);
                            animated.Value = animated.BaseScale * multiplier;
                            break;
                        }

                        case NonUniformScaleType.Initial:
                        {
                            animated.Value = animated.BaseScale;
                            break;
                        }
                    }
                }
            }

            private static float3 ExecuteSquashStretch(
                ref NonUniformScaleClipBlob blob, in float3 baseScale, in LocalTime localTime, in TimeTransform timeTransform,
                ref ClipBlobCurveCache clipCurveCache)
            {
                var time = (float)localTime.Value;
                var amount = EvaluateAmount(ref blob, baseScale, time, timeTransform, ref clipCurveCache);
                var multiplier = EvaluateMultiplier(amount, ref blob);

                return baseScale * multiplier;
            }

            private static int AxisIndex(SquashStretchNonUniformScaleAxis axis)
            {
                return axis switch
                {
                    SquashStretchNonUniformScaleAxis.X => 0,
                    SquashStretchNonUniformScaleAxis.Y => 1,
                    _ => 2,
                };
            }

            private static float EvaluateAmount(
                ref NonUniformScaleClipBlob data, in float3 baseScale, float time, in TimeTransform timeTransform, ref ClipBlobCurveCache clipCurveCache)
            {
                return data.Type switch
                {
                    NonUniformScaleType.SquashStretchAbsolute => data.SquashStretchAbsolute.Amount,
                    NonUniformScaleType.SquashStretchCurve => EvaluateCurve(ref data.SquashStretchCurve, time, ref clipCurveCache),
                    NonUniformScaleType.SquashStretchShake => EvaluateShake(ref data.SquashStretchShake, time, timeTransform, ref clipCurveCache),
                    NonUniformScaleType.SquashStretchSpring => EvaluateSpring(ref data.SquashStretchSpring, data.SquashStretch.Axis, baseScale, time),
                    _ => 0f,
                };
            }

            private static float EvaluateCurve(ref NonUniformScaleClipBlob.SquashStretchCurveData data, float time, ref ClipBlobCurveCache clipCurveCache)
            {
                return data.Curve.IsCreated ? data.Curve.Evaluate(time, ref clipCurveCache.Cache0) : 0f;
            }

            private static float EvaluateShake(
                ref NonUniformScaleClipBlob.SquashStretchShakeData data, float time, in TimeTransform timeTransform, ref ClipBlobCurveCache clipCurveCache)
            {
                if (math.abs(data.Amplitude) <= math.FLT_MIN_NORMAL)
                {
                    return 0f;
                }

                var attenuation = EvaluateShakeAttenuation(ref data, time, timeTransform, ref clipCurveCache);
                var seed = data.Seed != 0 ? data.Seed : GlobalRandom.NextUInt(1u, uint.MaxValue);
                return (data.Amplitude * attenuation) * ShakeUtility.Sample(time, data.Frequency, data.Damping, seed);
            }

            private static float EvaluateSpring(
                ref NonUniformScaleClipBlob.SquashStretchSpringData data, SquashStretchNonUniformScaleAxis axis, in float3 baseScale, float time)
            {
                var axisIndex = AxisIndex(axis);
                var baseAxisScale = baseScale[axisIndex];

                if (math.abs(baseAxisScale) <= math.FLT_MIN_NORMAL)
                {
                    return 0f;
                }

                var restMultiplier = data.RestIsMultiplier ? data.RestValue : 1f + data.RestValue;
                restMultiplier = math.max(restMultiplier, MinimumStretch);
                var restAxisScale = baseAxisScale * restMultiplier;
                var amplitude = baseAxisScale - restAxisScale;

                if (math.abs(amplitude) <= math.FLT_MIN_NORMAL && math.abs(data.InitialVelocity) <= math.FLT_MIN_NORMAL)
                {
                    return restMultiplier - 1f;
                }

                var offset = SpringUtility.Sample(time, data.Frequency, data.Damping, amplitude, data.InitialVelocity);
                var axisScale = restAxisScale + offset;
                axisScale = math.max(axisScale, baseAxisScale * MinimumStretch);
                return (axisScale / baseAxisScale) - 1f;
            }

            private static float3 EvaluateMultiplier(float amount, ref NonUniformScaleClipBlob data)
            {
                var stretch = math.max(1f + amount, MinimumStretch);
                var compensation = 1f;

                if (data.SquashStretch.PreserveVolume)
                {
                    var exponent = math.clamp(data.SquashStretch.VolumeExponent, 0f, 1f);
                    compensation = math.pow(stretch, -math.max(exponent, 0f));
                }

                var result = new float3(1f, 1f, 1f);
                switch (data.SquashStretch.Axis)
                {
                    case SquashStretchNonUniformScaleAxis.X:
                        result.x = stretch;
                        if (data.SquashStretch.PreserveVolume)
                        {
                            result.y = compensation;
                            result.z = compensation;
                        }

                        break;

                    case SquashStretchNonUniformScaleAxis.Y:
                        result.y = stretch;
                        if (data.SquashStretch.PreserveVolume)
                        {
                            result.x = compensation;
                            result.z = compensation;
                        }

                        break;

                    default:
                        result.z = stretch;
                        if (data.SquashStretch.PreserveVolume)
                        {
                            result.x = compensation;
                            result.y = compensation;
                        }

                        break;
                }

                return result;
            }

            private static float EvaluateShakeAttenuation(
                ref NonUniformScaleClipBlob.SquashStretchShakeData shake, float localTime, in TimeTransform timeTransform, ref ClipBlobCurveCache clipCurveCache)
            {
                if (!shake.AttenuationCurve.IsCreated)
                {
                    return 1f;
                }

                var clipDuration = (float)((timeTransform.End - timeTransform.Start) * timeTransform.Scale);
                if (!math.isfinite(clipDuration) || math.abs(clipDuration) <= math.FLT_MIN_NORMAL)
                {
                    return shake.AttenuationCurve.Evaluate(0f, ref clipCurveCache.Cache0);
                }

                var clipIn = (float)timeTransform.ClipIn;
                var normalizedTime = math.saturate((localTime - clipIn) / clipDuration);
                return shake.AttenuationCurve.Evaluate(normalizedTime, ref clipCurveCache.Cache0);
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<float3>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<PostTransformMatrix> PostTransforms;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.PostTransforms.TryGetRefRW(entity, out var postTransform))
                {
                    return;
                }

                var currentScale = ExtractScale(postTransform.ValueRO);
                var targetScale = JobHelpers.Blend<float3, Float3Mixer>(ref mixData, currentScale);
                postTransform.ValueRW = new PostTransformMatrix { Value = float4x4.Scale(targetScale) };
            }
        }
    }
}
