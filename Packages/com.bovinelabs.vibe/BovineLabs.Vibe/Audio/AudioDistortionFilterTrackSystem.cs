// <copyright file="AudioDistortionFilterTrackSystem.cs" company="BovineLabs">
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
    using BovineLabs.Vibe.Jobs;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Blends audio distortion filter clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct AudioDistortionFilterTrackSystem : ISystem
    {
        private TrackLifeImpl<AudioDistortionFilterData, AudioDistortionFilterInitial> lifeImpl;
        private TrackBlendImpl<AudioDistortionFilterBlend, AudioDistortionFilterAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AudioDistortionFilterClipData>();
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
                .WithAllRW<AudioDistortionFilterAnimated>()
                .WithAll<AudioDistortionFilterClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioDistortionFilterAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioDistortionFilterClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<AudioDistortionFilterInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioDistortionFilterAnimated>()
                .WithAll<AudioDistortionFilterClipData, Clip, ClipActive, LocalTime, ClipBlobCurveCache>()
                .Build();

            state.Dependency = new ClipUpdateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioDistortionFilterAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioDistortionFilterClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                Initials = SystemAPI.GetComponentLookup<AudioDistortionFilterInitial>(true),
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Filters = SystemAPI.GetComponentLookup<AudioDistortionFilterData>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioDistortionFilterAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioDistortionFilterClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<AudioDistortionFilterInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioDistortionFilterAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (AudioDistortionFilterClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        AudioDistortionFilterClipType.Initial => this.SelectInitialValue(clip.Track),
                        AudioDistortionFilterClipType.Sweep => this.SelectInitialValue(clip.Track),
                        AudioDistortionFilterClipType.Animated => new AudioDistortionFilterBlend
                        {
                            DistortionLevel = clipData.Data.DistortionLevel,
                        },
                        _ => animated.Value,
                    };
                }
            }

            private AudioDistortionFilterBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return new AudioDistortionFilterBlend
                {
                    DistortionLevel = initial.Value.DistortionLevel,
                };
            }
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioDistortionFilterAnimated> AnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioDistortionFilterClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentLookup<AudioDistortionFilterInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioDistortionFilterAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var clipDatas = (AudioDistortionFilterClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    if (clipData.Type != AudioDistortionFilterClipType.Sweep)
                    {
                        continue;
                    }

                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    ref readonly var localTime = ref localTimes[entityIndexInChunk];

                    var initial = this.Initials.TryGetComponent(clip.Track, out var initialData)
                        ? initialData.Value
                        : default;

                    var distortion = AudioCurveSweepUtility.Evaluate(
                        ref clipData.Sweep,
                        (float)localTime.Value,
                        ref clipBlobCaches[entityIndexInChunk],
                        initial.DistortionLevel);

                    animated.Value = new AudioDistortionFilterBlend
                    {
                        DistortionLevel = distortion,
                    };
                }
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<AudioDistortionFilterBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<AudioDistortionFilterData> Filters;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Filters.TryGetRefRW(entity, out var filter))
                {
                    return;
                }

                ref var value = ref filter.ValueRW;
                var current = new AudioDistortionFilterBlend
                {
                    DistortionLevel = value.DistortionLevel,
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(AudioDistortionFilterMixer));
                value.DistortionLevel = blended.DistortionLevel;
            }
        }

        private struct AudioDistortionFilterMixer : IMixer<AudioDistortionFilterBlend>
        {
            public AudioDistortionFilterBlend Lerp(in AudioDistortionFilterBlend a, in AudioDistortionFilterBlend b, in float s)
            {
                return new AudioDistortionFilterBlend
                {
                    DistortionLevel = math.lerp(a.DistortionLevel, b.DistortionLevel, s),
                };
            }

            public AudioDistortionFilterBlend Add(in AudioDistortionFilterBlend a, in AudioDistortionFilterBlend b)
            {
                return new AudioDistortionFilterBlend
                {
                    DistortionLevel = a.DistortionLevel + b.DistortionLevel,
                };
            }
        }
    }
}
#endif
