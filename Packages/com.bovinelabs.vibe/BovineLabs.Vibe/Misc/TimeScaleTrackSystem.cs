// <copyright file="TimeScaleTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe
{
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.Time;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Evaluates time scale clips and applies the blended result to the global time scale.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct TimeScaleTrackSystem : ISystem
    {
        private TrackBlendImpl<float, TimeScaleAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeScaleClipData>();
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
            var currentTimeScale = Time.timeScale;

            foreach (var initial in SystemAPI.Query<RefRW<TimeScaleInitial>>()
                         .WithAll<TimelineActive>()
                         .WithDisabled<TimelineActivePrevious>())
            {
                initial.ValueRW.Value = currentTimeScale;
            }

            var restoreRequested = false;
            var restoreValue = currentTimeScale;

            foreach (var initial in SystemAPI.Query<RefRO<TimeScaleInitial>>()
                         .WithAll<TrackResetOnDeactivate, TimelineActivePrevious>()
                         .WithDisabled<TimelineActive>())
            {
                restoreRequested = true;
                restoreValue = initial.ValueRO.Value;
            }

            var timeScaleInitials = SystemAPI.GetComponentLookup<TimeScaleInitial>(true);

            foreach (var (clipData, clip) in SystemAPI.Query<RefRO<TimeScaleClipData>, RefRO<Clip>>()
                         .WithAll<ClipActivePrevious>()
                         .WithDisabled<ClipActive>())
            {
                ref var clipValue = ref clipData.ValueRO.Value.Value;
                if (!clipValue.RestoreOnDeactivate)
                {
                    continue;
                }

                if (timeScaleInitials.TryGetComponent(clip.ValueRO.Track, out var initial))
                {
                    restoreValue = initial.Value;
                }

                restoreRequested = true;
            }

            var clipActivateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<TimeScaleAnimated>()
                .WithAll<TimeScaleClipData, Clip, ClipActive>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<TimeScaleAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<TimeScaleClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = timeScaleInitials,
            }.Schedule(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<TimeScaleAnimated>()
                .WithAll<TimeScaleClipData, Clip, ClipActive, LocalTime, ClipBlobCurveCache>()
                .Build();

            state.Dependency = new ClipUpdateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<TimeScaleAnimated>(),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<TimeScaleClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                Initials = timeScaleInitials,
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            // TODO we'll defer this in future to a separate system
            state.Dependency.Complete();

            if (blendData.TryGetValue(Entity.Null, out var mixData))
            {
                var blended = JobHelpers.Blend<float, FloatMixer>(ref mixData, currentTimeScale);
                Time.timeScale = math.max(blended, 0f);
            }
            else if (restoreRequested)
            {
                Time.timeScale = math.max(restoreValue, 0f);
            }
        }

        private static float ClampValue(float value, ref TimeScaleClipBlob clipData)
        {
            var minValue = math.min(clipData.ClampMin, clipData.ClampMax);
            var maxValue = math.max(clipData.ClampMin, clipData.ClampMax);
            return math.clamp(value, minValue, maxValue);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<TimeScaleAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<TimeScaleClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<TimeScaleInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (TimeScaleAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (TimeScaleClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    var baseScale = this.Initials.TryGetComponent(clip.Track, out var initial) ? initial.Value : 1f;
                    var value = clipData.UseCurve ? baseScale : clipData.TargetScale;

                    animated.Value = ClampValue(value, ref clipData);
                }
            }
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<TimeScaleAnimated> AnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<TimeScaleClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentLookup<TimeScaleInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (TimeScaleAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var clipDatas = (TimeScaleClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    if (!clipData.UseCurve || !clipData.Curve.IsCreated)
                    {
                        continue;
                    }

                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    ref readonly var localTime = ref localTimes[entityIndexInChunk];
                    ref var clipCache = ref clipBlobCaches[entityIndexInChunk];

                    var baseScale = this.Initials.TryGetComponent(clip.Track, out var initial) ? initial.Value : 1f;
                    var normalized = math.saturate(clipData.Curve.Evaluate((float)localTime.Value, ref clipCache.Cache0));
                    var value = math.lerp(baseScale, clipData.TargetScale, normalized);
                    animated.Value = ClampValue(value, ref clipData);
                }
            }
        }
    }
}
