// <copyright file="AudioSourcePanSweepTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Core.Jobs;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.Audio;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Blends audio source pan sweep clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct AudioSourcePanSweepTrackSystem : ISystem
    {
        private TrackBlendImpl<float, AudioSourcePanAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AudioSourcePanSweepClipData>();
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
            state.Dependency = new TrackDeactivateJob
            {
                AudioSources = SystemAPI.GetComponentLookup<AudioSourceDataExtended>(),
            }.Schedule(state.Dependency);

            state.Dependency = new TrackActivateJob
            {
                AudioSources = SystemAPI.GetComponentLookup<AudioSourceDataExtended>(true),
            }.ScheduleParallel(state.Dependency);

            var clipActivateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioSourcePanAnimated>()
                .WithAll<AudioSourcePanSweepClipData, Clip, ClipActive>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioSourcePanAnimated>(),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackInitials = SystemAPI.GetComponentLookup<AudioSourcePanInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioSourcePanAnimated>()
                .WithAll<AudioSourcePanSweepClipData, Clip, ClipActive, LocalTime, ClipBlobCurveCache>()
                .Build();

            state.Dependency = new ClipUpdateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioSourcePanAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioSourcePanSweepClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                TrackInitials = SystemAPI.GetComponentLookup<AudioSourcePanInitial>(true),
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                AudioSources = SystemAPI.GetComponentLookup<AudioSourceDataExtended>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        private partial struct TrackActivateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<AudioSourceDataExtended> AudioSources;

            private void Execute(ref AudioSourcePanInitial initial, in TrackBinding trackBinding)
            {
                if (!this.AudioSources.TryGetComponent(trackBinding.Value, out var audioSource))
                {
                    return;
                }

                initial.Value = audioSource.PanStereo;
            }
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))]
        [WithAll(typeof(TimelineActivePrevious))]
        [WithDisabled(typeof(TimelineActive))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            public ComponentLookup<AudioSourceDataExtended> AudioSources;

            private void Execute(in AudioSourcePanInitial initial, in TrackBinding trackBinding)
            {
                if (!this.AudioSources.TryGetRefRW(trackBinding.Value, out var audioSource))
                {
                    return;
                }

                audioSource.ValueRW.PanStereo = initial.Value;
            }
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioSourcePanAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<AudioSourcePanInitial> TrackInitials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioSourcePanAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    var baseValue = this.TrackInitials.TryGetComponent(clip.Track, out var initial)
                        ? initial.Value
                        : 0f;

                    animated.Value = baseValue;
                }
            }
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioSourcePanAnimated> AnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioSourcePanSweepClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentLookup<AudioSourcePanInitial> TrackInitials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioSourcePanAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var clipDatas = (AudioSourcePanSweepClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    ref readonly var localTime = ref localTimes[entityIndexInChunk];

                    var baseValue = this.TrackInitials.TryGetComponent(clip.Track, out var initial)
                        ? initial.Value
                        : 0f;

                    animated.Value = AudioCurveSweepUtility.Evaluate(
                        ref clipData.Sweep,
                        (float)localTime.Value,
                        ref clipBlobCaches[entityIndexInChunk],
                        baseValue);
                }
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<float>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<AudioSourceDataExtended> AudioSources;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.AudioSources.TryGetRefRW(entity, out var audioSource))
                {
                    return;
                }

                audioSource.ValueRW.PanStereo = JobHelpers.Blend<float, FloatMixer>(ref mixData, audioSource.ValueRO.PanStereo);
            }
        }
    }
}
#endif
