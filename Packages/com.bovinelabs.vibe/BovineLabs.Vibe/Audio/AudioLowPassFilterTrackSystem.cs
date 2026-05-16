// <copyright file="AudioLowPassFilterTrackSystem.cs" company="BovineLabs">
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
    /// Blends audio low-pass filter clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct AudioLowPassFilterTrackSystem : ISystem
    {
        private TrackLifeImpl<AudioLowPassFilterData, AudioLowPassFilterInitial> lifeImpl;
        private TrackBlendImpl<AudioLowPassFilterBlend, AudioLowPassFilterAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AudioLowPassFilterClipData>();
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
                .WithAllRW<AudioLowPassFilterAnimated>()
                .WithAll<AudioLowPassFilterClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioLowPassFilterAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioLowPassFilterClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<AudioLowPassFilterInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioLowPassFilterAnimated>()
                .WithAll<AudioLowPassFilterClipData, Clip, ClipActive, LocalTime, ClipBlobCurveCache>()
                .Build();

            state.Dependency = new ClipUpdateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioLowPassFilterAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioLowPassFilterClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                Initials = SystemAPI.GetComponentLookup<AudioLowPassFilterInitial>(true),
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Filters = SystemAPI.GetComponentLookup<AudioLowPassFilterData>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioLowPassFilterAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioLowPassFilterClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<AudioLowPassFilterInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioLowPassFilterAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (AudioLowPassFilterClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        AudioLowPassFilterClipType.Initial => this.SelectInitialValue(clip.Track),
                        AudioLowPassFilterClipType.Sweep => this.SelectInitialValue(clip.Track),
                        AudioLowPassFilterClipType.Animated => new AudioLowPassFilterBlend
                        {
                            CutoffFrequency = clipData.Data.CutoffFrequency,
                            LowpassResonanceQ = clipData.Data.LowpassResonanceQ,
                        },
                        _ => animated.Value,
                    };
                }
            }

            private AudioLowPassFilterBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return new AudioLowPassFilterBlend
                {
                    CutoffFrequency = initial.Value.CutoffFrequency,
                    LowpassResonanceQ = initial.Value.LowpassResonanceQ,
                };
            }
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioLowPassFilterAnimated> AnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioLowPassFilterClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentLookup<AudioLowPassFilterInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioLowPassFilterAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var clipDatas = (AudioLowPassFilterClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    if (clipData.Type != AudioLowPassFilterClipType.Sweep)
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

                    animated.Value = new AudioLowPassFilterBlend
                    {
                        CutoffFrequency = cutoff,
                        LowpassResonanceQ = initial.LowpassResonanceQ,
                    };
                }
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<AudioLowPassFilterBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<AudioLowPassFilterData> Filters;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Filters.TryGetRefRW(entity, out var filter))
                {
                    return;
                }

                ref var value = ref filter.ValueRW;
                var current = new AudioLowPassFilterBlend
                {
                    CutoffFrequency = value.CutoffFrequency,
                    LowpassResonanceQ = value.LowpassResonanceQ,
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(AudioLowPassFilterMixer));
                value.CutoffFrequency = blended.CutoffFrequency;
                value.LowpassResonanceQ = blended.LowpassResonanceQ;
            }
        }

        private struct AudioLowPassFilterMixer : IMixer<AudioLowPassFilterBlend>
        {
            public AudioLowPassFilterBlend Lerp(in AudioLowPassFilterBlend a, in AudioLowPassFilterBlend b, in float s)
            {
                return new AudioLowPassFilterBlend
                {
                    CutoffFrequency = math.lerp(a.CutoffFrequency, b.CutoffFrequency, s),
                    LowpassResonanceQ = math.lerp(a.LowpassResonanceQ, b.LowpassResonanceQ, s),
                };
            }

            public AudioLowPassFilterBlend Add(in AudioLowPassFilterBlend a, in AudioLowPassFilterBlend b)
            {
                return new AudioLowPassFilterBlend
                {
                    CutoffFrequency = a.CutoffFrequency + b.CutoffFrequency,
                    LowpassResonanceQ = a.LowpassResonanceQ + b.LowpassResonanceQ,
                };
            }
        }
    }
}
#endif
