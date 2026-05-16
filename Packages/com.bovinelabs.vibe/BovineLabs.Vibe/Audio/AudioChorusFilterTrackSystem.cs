// <copyright file="AudioChorusFilterTrackSystem.cs" company="BovineLabs">
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
    /// Blends audio chorus filter clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct AudioChorusFilterTrackSystem : ISystem
    {
        private TrackLifeImpl<AudioChorusFilterData, AudioChorusFilterInitial> lifeImpl;
        private TrackBlendImpl<AudioChorusFilterBlend, AudioChorusFilterAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AudioChorusFilterClipData>();
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
                .WithAllRW<AudioChorusFilterAnimated>()
                .WithAll<AudioChorusFilterClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioChorusFilterAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioChorusFilterClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<AudioChorusFilterInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioChorusFilterAnimated>()
                .WithAll<AudioChorusFilterClipData, Clip, ClipActive, LocalTime, ClipBlobCurveCache>()
                .Build();

            state.Dependency = new ClipUpdateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioChorusFilterAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioChorusFilterClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                Initials = SystemAPI.GetComponentLookup<AudioChorusFilterInitial>(true),
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Filters = SystemAPI.GetComponentLookup<AudioChorusFilterData>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioChorusFilterAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioChorusFilterClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<AudioChorusFilterInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioChorusFilterAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (AudioChorusFilterClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        AudioChorusFilterClipType.Initial => this.SelectInitialValue(clip.Track),
                        AudioChorusFilterClipType.Sweep => this.SelectInitialValue(clip.Track),
                        AudioChorusFilterClipType.Animated => new AudioChorusFilterBlend
                        {
                            DryMix = clipData.Data.DryMix,
                            WetMix1 = clipData.Data.WetMix1,
                            WetMix2 = clipData.Data.WetMix2,
                            WetMix3 = clipData.Data.WetMix3,
                            Delay = clipData.Data.Delay,
                            Rate = clipData.Data.Rate,
                            Depth = clipData.Data.Depth,
                        },
                        _ => animated.Value,
                    };
                }
            }

            private AudioChorusFilterBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return new AudioChorusFilterBlend
                {
                    DryMix = initial.Value.DryMix,
                    WetMix1 = initial.Value.WetMix1,
                    WetMix2 = initial.Value.WetMix2,
                    WetMix3 = initial.Value.WetMix3,
                    Delay = initial.Value.Delay,
                    Rate = initial.Value.Rate,
                    Depth = initial.Value.Depth,
                };
            }
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioChorusFilterAnimated> AnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioChorusFilterClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentLookup<AudioChorusFilterInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioChorusFilterAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var clipDatas = (AudioChorusFilterClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    if (clipData.Type != AudioChorusFilterClipType.Sweep)
                    {
                        continue;
                    }

                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    ref readonly var localTime = ref localTimes[entityIndexInChunk];

                    var initial = this.Initials.TryGetComponent(clip.Track, out var initialData)
                        ? initialData.Value
                        : default;

                    var depth = AudioCurveSweepUtility.Evaluate(
                        ref clipData.Sweep,
                        (float)localTime.Value,
                        ref clipBlobCaches[entityIndexInChunk],
                        initial.Depth);

                    animated.Value = new AudioChorusFilterBlend
                    {
                        DryMix = initial.DryMix,
                        WetMix1 = initial.WetMix1,
                        WetMix2 = initial.WetMix2,
                        WetMix3 = initial.WetMix3,
                        Delay = initial.Delay,
                        Rate = initial.Rate,
                        Depth = depth,
                    };
                }
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<AudioChorusFilterBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<AudioChorusFilterData> Filters;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Filters.TryGetRefRW(entity, out var filter))
                {
                    return;
                }

                ref var value = ref filter.ValueRW;
                var current = new AudioChorusFilterBlend
                {
                    DryMix = value.DryMix,
                    WetMix1 = value.WetMix1,
                    WetMix2 = value.WetMix2,
                    WetMix3 = value.WetMix3,
                    Delay = value.Delay,
                    Rate = value.Rate,
                    Depth = value.Depth,
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(AudioChorusFilterMixer));
                value.DryMix = blended.DryMix;
                value.WetMix1 = blended.WetMix1;
                value.WetMix2 = blended.WetMix2;
                value.WetMix3 = blended.WetMix3;
                value.Delay = blended.Delay;
                value.Rate = blended.Rate;
                value.Depth = blended.Depth;
            }
        }

        private struct AudioChorusFilterMixer : IMixer<AudioChorusFilterBlend>
        {
            public AudioChorusFilterBlend Lerp(in AudioChorusFilterBlend a, in AudioChorusFilterBlend b, in float s)
            {
                return new AudioChorusFilterBlend
                {
                    DryMix = math.lerp(a.DryMix, b.DryMix, s),
                    WetMix1 = math.lerp(a.WetMix1, b.WetMix1, s),
                    WetMix2 = math.lerp(a.WetMix2, b.WetMix2, s),
                    WetMix3 = math.lerp(a.WetMix3, b.WetMix3, s),
                    Delay = math.lerp(a.Delay, b.Delay, s),
                    Rate = math.lerp(a.Rate, b.Rate, s),
                    Depth = math.lerp(a.Depth, b.Depth, s),
                };
            }

            public AudioChorusFilterBlend Add(in AudioChorusFilterBlend a, in AudioChorusFilterBlend b)
            {
                return new AudioChorusFilterBlend
                {
                    DryMix = a.DryMix + b.DryMix,
                    WetMix1 = a.WetMix1 + b.WetMix1,
                    WetMix2 = a.WetMix2 + b.WetMix2,
                    WetMix3 = a.WetMix3 + b.WetMix3,
                    Delay = a.Delay + b.Delay,
                    Rate = a.Rate + b.Rate,
                    Depth = a.Depth + b.Depth,
                };
            }
        }
    }
}
#endif
