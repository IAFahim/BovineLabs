// <copyright file="ScaleTrackSystem.cs" company="BovineLabs">
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
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary>
    /// Drives uniform scale animation for timeline tracks.
    /// </summary>
    [UpdateAfter(typeof(LocalTransformTrackSystem))]
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct ScaleTrackSystem : ISystem
    {
        private TrackBlendImpl<float, ScaleAnimated> impl;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScaleClipData>();
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
            var localTransformsRw = SystemAPI.GetComponentLookup<LocalTransform>();

            new TrackDeactivateJob { LocalTransforms = localTransformsRw }.Schedule();

            var clipActivateQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<ScaleAnimated>()
                .WithAll<ScaleClipData, LocalTransformClipInitial>()
                .WithAll<Clip, ClipActive>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                ScaleAnimatedHandle = SystemAPI.GetComponentTypeHandle<ScaleAnimated>(),
                LocalTransformClipInitialHandle = SystemAPI.GetComponentTypeHandle<LocalTransformClipInitial>(),
                ScaleClipDataHandle = SystemAPI.GetComponentTypeHandle<ScaleClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTransformInitials = SystemAPI.GetComponentLookup<LocalTransformInitial>(true),
            }.ScheduleParallel(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<ScaleAnimated>()
                .WithAll<ScaleClipData, LocalTransformClipInitial>()
                .WithAll<Clip, ClipActive>()
                .WithAll<LocalTime, TimeTransform>()
                .Build();

            state.Dependency = new ClipUpdateJob
            {
                ScaleAnimatedHandle = SystemAPI.GetComponentTypeHandle<ScaleAnimated>(),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                ScaleClipDataHandle = SystemAPI.GetComponentTypeHandle<ScaleClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                TimeTransformHandle = SystemAPI.GetComponentTypeHandle<TimeTransform>(true),
                LocalTransformClipInitialHandle = SystemAPI.GetComponentTypeHandle<LocalTransformClipInitial>(true),
                LocalTransformInitials = SystemAPI.GetComponentLookup<LocalTransformInitial>(true),
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                LocalTransforms = localTransformsRw,
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static float OffsetValue(ref ScaleClipBlob.OffsetData data, float baseScale)
        {
            return data.IsMultiplier ? baseScale * data.Value : baseScale + data.Value;
        }

        private static float EvaluateCurve(ref ScaleClipBlob.CurveData data, float time, ref ClipBlobCurveCache clipCurveCache)
        {
            return data.Curve.IsCreated ? data.Curve.Evaluate(time, ref clipCurveCache.Cache0) : 0f;
        }

        private static bool UseClipActivation(ref ScaleClipBlob data)
        {
            return data.Type switch
            {
                ScaleType.Curve => data.Curve.TransformOnClipActivation,
                ScaleType.Shake => data.Shake.TransformOnClipActivation,
                ScaleType.Spring => data.Spring.TransformOnClipActivation,
                ScaleType.Wiggle => data.Wiggle.TransformOnClipActivation,
                _ => false,
            };
        }

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

                localTransform.ValueRW.Scale = localTransformInitial.Value.Scale;
            }
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<ScaleAnimated> ScaleAnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTransformClipInitial> LocalTransformClipInitialHandle;

            [ReadOnly]
            public ComponentTypeHandle<ScaleClipData> ScaleClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<LocalTransformInitial> LocalTransformInitials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var scaleAnimateds = (ScaleAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.ScaleAnimatedHandle);
                var localTransformClipInitials = (LocalTransformClipInitial*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTransformClipInitialHandle);
                var scaleClipDatas = (ScaleClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ScaleClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInQuery))
                {
                    ref var scaleClipData = ref (scaleClipDatas + entityIndexInQuery)->Value.Value;
                    ref var scaleAnimated = ref scaleAnimateds[entityIndexInQuery];
                    var transform = ClipTransformSelection.SelectLocalTransform(UseClipActivation(ref scaleClipData),
                        localTransformClipInitials[entityIndexInQuery].Value, ref this.LocalTransformInitials, clips[entityIndexInQuery]);

                    scaleAnimated.Value = scaleClipData.Type switch
                    {
                        ScaleType.Initial => ClipTransformSelection.GetTrackLocalTransform(ref this.LocalTransformInitials, clips[entityIndexInQuery]).Scale,
                        ScaleType.Absolute => scaleClipData.Absolute.Value,
                        ScaleType.Offset => OffsetValue(ref scaleClipData.Offset, transform.Scale),
                        _ => transform.Scale,
                    };
                }
            }
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<ScaleAnimated> ScaleAnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<ScaleClipData> ScaleClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentTypeHandle<TimeTransform> TimeTransformHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTransformClipInitial> LocalTransformClipInitialHandle;

            [ReadOnly]
            public ComponentLookup<LocalTransformInitial> LocalTransformInitials;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var scaleAnimateds = (ScaleAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.ScaleAnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var scaleClipDatas = (ScaleClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ScaleClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);
                var timeTransforms = (TimeTransform*)chunk.GetRequiredComponentDataPtrRO(ref this.TimeTransformHandle);
                var localTransformClipInitials = (LocalTransformClipInitial*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTransformClipInitialHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInQuery))
                {
                    ref var scaleAnimated = ref scaleAnimateds[entityIndexInQuery];
                    ref var scaleClipData = ref scaleClipDatas[entityIndexInQuery];
                    var clip = clips[entityIndexInQuery];
                    ref var localTime = ref localTimes[entityIndexInQuery];
                    var timeTransform = timeTransforms[entityIndexInQuery];
                    var localTransformClipInitial = localTransformClipInitials[entityIndexInQuery];

                    ref var data = ref scaleClipData.Value.Value;

                    scaleAnimated.Value = data.Type switch
                    {
                        ScaleType.Curve => this.CurveValue(ref data, clip, localTransformClipInitial, localTime, ref clipBlobCaches[entityIndexInQuery]),
                        ScaleType.Shake => this.ShakeValue(ref data, clip, localTransformClipInitial, (float)localTime.Value,
                            timeTransform, ref clipBlobCaches[entityIndexInQuery]),
                        ScaleType.Spring => this.SpringValue(ref data, clip, localTransformClipInitial, (float)localTime.Value),
                        ScaleType.Wiggle => this.WiggleValue(ref data, clip, localTransformClipInitial, (float)localTime.Value, timeTransform,
                            ref clipBlobCaches[entityIndexInQuery]),
                        _ => scaleAnimated.Value,
                    };
                }
            }

            private float CurveValue(
                ref ScaleClipBlob data, in Clip clip, in LocalTransformClipInitial localTransformClipInitial, in LocalTime localTime,
                ref ClipBlobCurveCache clipCurveCache)
            {
                var baseScale = this.GetBaseScale(ref data, clip, localTransformClipInitial);
                ref var curve = ref data.Curve;
                if (!curve.Curve.IsCreated)
                {
                    return baseScale;
                }

                return baseScale + EvaluateCurve(ref curve, (float)localTime.Value, ref clipCurveCache);
            }

            private float GetBaseScale(ref ScaleClipBlob data, in Clip clip, in LocalTransformClipInitial localTransformClipInitial)
            {
                var baseTransform = ClipTransformSelection.SelectLocalTransform(UseClipActivation(ref data), localTransformClipInitial.Value,
                    ref this.LocalTransformInitials, clip);

                return baseTransform.Scale;
            }

            private float ShakeValue(
                ref ScaleClipBlob data, in Clip clip, in LocalTransformClipInitial localTransformClipInitial, float time, in TimeTransform timeTransform,
                ref ClipBlobCurveCache clipCurveCache)
            {
                var baseScale = this.GetBaseScale(ref data, clip, localTransformClipInitial);
                ref var shake = ref data.Shake;
                var attenuation = EvaluateShakeAttenuation(ref shake, time, timeTransform, ref clipCurveCache);
                var amplitudeScale = ClipCurveUtility.EvaluateNormalized(ref shake.AmplitudeCurve, time, timeTransform, ref clipCurveCache.Cache0);
                var amplitude = shake.Amplitude * (attenuation * amplitudeScale);
                if (math.abs(amplitude) <= math.FLT_MIN_NORMAL)
                {
                    return baseScale;
                }

                var seed = shake.Seed != 0 ? shake.Seed : GlobalRandom.NextUInt(1u, uint.MaxValue);
                var frequencyScale = ClipCurveUtility.EvaluateNormalized(ref shake.FrequencyCurve, time, timeTransform, ref clipCurveCache.Cache2);
                var frequency = math.max(0f, shake.Frequency * frequencyScale * shake.PerAxisFrequencyMultiplier);
                var offset = amplitude * ShakeUtility.Sample(time, frequency, shake.Damping, seed);
                return baseScale + offset;
            }

            private static float EvaluateShakeAttenuation(
                ref ScaleClipBlob.ShakeData shake, float localTime, in TimeTransform timeTransform, ref ClipBlobCurveCache clipCurveCache)
            {
                return ClipCurveUtility.EvaluateNormalized(ref shake.AttenuationCurve, localTime, timeTransform, ref clipCurveCache.Cache1);
            }

            private float SpringValue(ref ScaleClipBlob data, in Clip clip, in LocalTransformClipInitial localTransformClipInitial, float time)
            {
                var baseScale = this.GetBaseScale(ref data, clip, localTransformClipInitial);
                ref var spring = ref data.Spring;
                var restScale = spring.RestIsMultiplier ? baseScale * spring.RestScale : spring.RestScale;
                var amplitude = baseScale - restScale;

                if (math.abs(amplitude) <= math.FLT_MIN_NORMAL && math.abs(spring.InitialVelocity) <= math.FLT_MIN_NORMAL)
                {
                    return restScale;
                }

                var offset = SpringUtility.Sample(time, spring.Frequency, spring.Damping, amplitude, spring.InitialVelocity);
                return restScale + offset;
            }

            private float WiggleValue(
                ref ScaleClipBlob data, in Clip clip, in LocalTransformClipInitial localTransformClipInitial, float time, in TimeTransform timeTransform,
                ref ClipBlobCurveCache clipCurveCache)
            {
                var baseScale = this.GetBaseScale(ref data, clip, localTransformClipInitial);
                ref var wiggle = ref data.Wiggle;
                var amplitudeScale = ClipCurveUtility.EvaluateNormalized(ref wiggle.AmplitudeCurve, time, timeTransform, ref clipCurveCache.Cache0);
                var amplitude = wiggle.Amplitude * amplitudeScale;
                if (math.abs(amplitude) <= math.FLT_MIN_NORMAL)
                {
                    return baseScale;
                }

                var seed = wiggle.Seed != 0 ? wiggle.Seed : GlobalRandom.NextUInt(1u, uint.MaxValue);
                var frequencyScale = ClipCurveUtility.EvaluateNormalized(ref wiggle.FrequencyCurve, time, timeTransform, ref clipCurveCache.Cache1);
                var frequency = math.max(0f, wiggle.Frequency * frequencyScale * wiggle.PerAxisFrequencyMultiplier);
                var offset = amplitude * WiggleUtility.Sample(time, frequency, wiggle.Smoothness, seed);
                return baseScale + offset;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<float>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> LocalTransforms;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var target);

                if (!this.LocalTransforms.TryGetRefRW(entity, out var localTransform))
                {
                    return;
                }

                localTransform.ValueRW.Scale = JobHelpers.Blend<float, FloatMixer>(ref target, localTransform.ValueRO.Scale);
            }
        }
    }
}
