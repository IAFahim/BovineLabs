// <copyright file="AudioEchoFilterTrackSystem.cs" company="BovineLabs">
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
    /// Blends audio echo filter clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct AudioEchoFilterTrackSystem : ISystem
    {
        private TrackLifeImpl<AudioEchoFilterData, AudioEchoFilterInitial> lifeImpl;
        private TrackBlendImpl<AudioEchoFilterBlend, AudioEchoFilterAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AudioEchoFilterClipData>();
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
                .WithAllRW<AudioEchoFilterAnimated>()
                .WithAll<AudioEchoFilterClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioEchoFilterAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioEchoFilterClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<AudioEchoFilterInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioEchoFilterAnimated>()
                .WithAll<AudioEchoFilterClipData, Clip, ClipActive, LocalTime, ClipBlobCurveCache>()
                .Build();

            state.Dependency = new ClipUpdateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioEchoFilterAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioEchoFilterClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                Initials = SystemAPI.GetComponentLookup<AudioEchoFilterInitial>(true),
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Filters = SystemAPI.GetComponentLookup<AudioEchoFilterData>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioEchoFilterAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioEchoFilterClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<AudioEchoFilterInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioEchoFilterAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (AudioEchoFilterClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        AudioEchoFilterClipType.Initial => this.SelectInitialValue(clip.Track),
                        AudioEchoFilterClipType.Sweep => this.SelectInitialValue(clip.Track),
                        AudioEchoFilterClipType.Animated => new AudioEchoFilterBlend
                        {
                            Delay = clipData.Data.Delay,
                            DecayRatio = clipData.Data.DecayRatio,
                            WetMix = clipData.Data.WetMix,
                            DryMix = clipData.Data.DryMix,
                        },
                        _ => animated.Value,
                    };
                }
            }

            private AudioEchoFilterBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return new AudioEchoFilterBlend
                {
                    Delay = initial.Value.Delay,
                    DecayRatio = initial.Value.DecayRatio,
                    WetMix = initial.Value.WetMix,
                    DryMix = initial.Value.DryMix,
                };
            }
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioEchoFilterAnimated> AnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioEchoFilterClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentLookup<AudioEchoFilterInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioEchoFilterAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var clipDatas = (AudioEchoFilterClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    if (clipData.Type != AudioEchoFilterClipType.Sweep)
                    {
                        continue;
                    }

                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    ref readonly var localTime = ref localTimes[entityIndexInChunk];

                    var initial = this.Initials.TryGetComponent(clip.Track, out var initialData)
                        ? initialData.Value
                        : default;

                    var wetMix = AudioCurveSweepUtility.Evaluate(
                        ref clipData.Sweep,
                        (float)localTime.Value,
                        ref clipBlobCaches[entityIndexInChunk],
                        initial.WetMix);

                    animated.Value = new AudioEchoFilterBlend
                    {
                        Delay = initial.Delay,
                        DecayRatio = initial.DecayRatio,
                        WetMix = wetMix,
                        DryMix = initial.DryMix,
                    };
                }
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<AudioEchoFilterBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<AudioEchoFilterData> Filters;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Filters.TryGetRefRW(entity, out var filter))
                {
                    return;
                }

                ref var value = ref filter.ValueRW;
                var current = new AudioEchoFilterBlend
                {
                    Delay = value.Delay,
                    DecayRatio = value.DecayRatio,
                    WetMix = value.WetMix,
                    DryMix = value.DryMix,
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(AudioEchoFilterMixer));
                value.Delay = blended.Delay;
                value.DecayRatio = blended.DecayRatio;
                value.WetMix = blended.WetMix;
                value.DryMix = blended.DryMix;
            }
        }

        private struct AudioEchoFilterMixer : IMixer<AudioEchoFilterBlend>
        {
            public AudioEchoFilterBlend Lerp(in AudioEchoFilterBlend a, in AudioEchoFilterBlend b, in float s)
            {
                return new AudioEchoFilterBlend
                {
                    Delay = math.lerp(a.Delay, b.Delay, s),
                    DecayRatio = math.lerp(a.DecayRatio, b.DecayRatio, s),
                    WetMix = math.lerp(a.WetMix, b.WetMix, s),
                    DryMix = math.lerp(a.DryMix, b.DryMix, s),
                };
            }

            public AudioEchoFilterBlend Add(in AudioEchoFilterBlend a, in AudioEchoFilterBlend b)
            {
                return new AudioEchoFilterBlend
                {
                    Delay = a.Delay + b.Delay,
                    DecayRatio = a.DecayRatio + b.DecayRatio,
                    WetMix = a.WetMix + b.WetMix,
                    DryMix = a.DryMix + b.DryMix,
                };
            }
        }
    }
}
#endif
