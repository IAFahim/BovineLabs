// <copyright file="AudioHighPassFilterTrackSystem.cs" company="BovineLabs">
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
    /// Blends audio high-pass filter clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct AudioHighPassFilterTrackSystem : ISystem
    {
        private TrackLifeImpl<AudioHighPassFilterData, AudioHighPassFilterInitial> lifeImpl;
        private TrackBlendImpl<AudioHighPassFilterBlend, AudioHighPassFilterAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AudioHighPassFilterClipData>();
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
                .WithAllRW<AudioHighPassFilterAnimated>()
                .WithAll<AudioHighPassFilterClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioHighPassFilterAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioHighPassFilterClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<AudioHighPassFilterInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioHighPassFilterAnimated>()
                .WithAll<AudioHighPassFilterClipData, Clip, ClipActive, LocalTime, ClipBlobCurveCache>()
                .Build();

            state.Dependency = new ClipUpdateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioHighPassFilterAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioHighPassFilterClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                Initials = SystemAPI.GetComponentLookup<AudioHighPassFilterInitial>(true),
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Filters = SystemAPI.GetComponentLookup<AudioHighPassFilterData>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioHighPassFilterAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioHighPassFilterClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<AudioHighPassFilterInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioHighPassFilterAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (AudioHighPassFilterClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        AudioHighPassFilterClipType.Initial => this.SelectInitialValue(clip.Track),
                        AudioHighPassFilterClipType.Sweep => this.SelectInitialValue(clip.Track),
                        AudioHighPassFilterClipType.Animated => new AudioHighPassFilterBlend
                        {
                            CutoffFrequency = clipData.Data.CutoffFrequency,
                            HighpassResonanceQ = clipData.Data.HighpassResonanceQ,
                        },
                        _ => animated.Value,
                    };
                }
            }

            private AudioHighPassFilterBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return new AudioHighPassFilterBlend
                {
                    CutoffFrequency = initial.Value.CutoffFrequency,
                    HighpassResonanceQ = initial.Value.HighpassResonanceQ,
                };
            }
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioHighPassFilterAnimated> AnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioHighPassFilterClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentLookup<AudioHighPassFilterInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioHighPassFilterAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var clipDatas = (AudioHighPassFilterClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    if (clipData.Type != AudioHighPassFilterClipType.Sweep)
                    {
                        continue;
                    }

                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    ref readonly var localTime = ref localTimes[entityIndexInChunk];

                    var initial = this.Initials.TryGetComponent(clip.Track, out var initialData)
                        ? initialData.Value
                        : default;

                    var cutoff = AudioCurveSweepUtility.Evaluate(
                        ref clipData.Sweep,
                        (float)localTime.Value,
                        ref clipBlobCaches[entityIndexInChunk],
                        initial.CutoffFrequency);

                    animated.Value = new AudioHighPassFilterBlend
                    {
                        CutoffFrequency = cutoff,
                        HighpassResonanceQ = initial.HighpassResonanceQ,
                    };
                }
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<AudioHighPassFilterBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<AudioHighPassFilterData> Filters;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Filters.TryGetRefRW(entity, out var filter))
                {
                    return;
                }

                ref var value = ref filter.ValueRW;
                var current = new AudioHighPassFilterBlend
                {
                    CutoffFrequency = value.CutoffFrequency,
                    HighpassResonanceQ = value.HighpassResonanceQ,
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(AudioHighPassFilterMixer));
                value.CutoffFrequency = blended.CutoffFrequency;
                value.HighpassResonanceQ = blended.HighpassResonanceQ;
            }
        }

        private struct AudioHighPassFilterMixer : IMixer<AudioHighPassFilterBlend>
        {
            public AudioHighPassFilterBlend Lerp(in AudioHighPassFilterBlend a, in AudioHighPassFilterBlend b, in float s)
            {
                return new AudioHighPassFilterBlend
                {
                    CutoffFrequency = math.lerp(a.CutoffFrequency, b.CutoffFrequency, s),
                    HighpassResonanceQ = math.lerp(a.HighpassResonanceQ, b.HighpassResonanceQ, s),
                };
            }

            public AudioHighPassFilterBlend Add(in AudioHighPassFilterBlend a, in AudioHighPassFilterBlend b)
            {
                return new AudioHighPassFilterBlend
                {
                    CutoffFrequency = a.CutoffFrequency + b.CutoffFrequency,
                    HighpassResonanceQ = a.HighpassResonanceQ + b.HighpassResonanceQ,
                };
            }
        }
    }
}
#endif
