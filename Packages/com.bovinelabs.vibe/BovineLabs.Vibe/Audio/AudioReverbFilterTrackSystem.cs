// <copyright file="AudioReverbFilterTrackSystem.cs" company="BovineLabs">
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
    /// Blends audio reverb filter clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct AudioReverbFilterTrackSystem : ISystem
    {
        private TrackLifeImpl<AudioReverbFilterData, AudioReverbFilterInitial> lifeImpl;
        private TrackBlendImpl<AudioReverbFilterBlend, AudioReverbFilterAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AudioReverbFilterClipData>();
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
                .WithAllRW<AudioReverbFilterAnimated>()
                .WithAll<AudioReverbFilterClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioReverbFilterAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioReverbFilterClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<AudioReverbFilterInitial>(true),
                Filters = SystemAPI.GetComponentLookup<AudioReverbFilterData>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioReverbFilterAnimated>()
                .WithAll<AudioReverbFilterClipData, Clip, ClipActive, LocalTime, ClipBlobCurveCache>()
                .Build();

            state.Dependency = new ClipUpdateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioReverbFilterAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioReverbFilterClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                Initials = SystemAPI.GetComponentLookup<AudioReverbFilterInitial>(true),
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Filters = SystemAPI.GetComponentLookup<AudioReverbFilterData>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioReverbFilterAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioReverbFilterClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<AudioReverbFilterInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<AudioReverbFilterData> Filters;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioReverbFilterAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (AudioReverbFilterClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var bindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    ref readonly var binding = ref bindings[entityIndexInChunk];

                    switch (clipData.Type)
                    {
                        case AudioReverbFilterClipType.Initial:
                        {
                            var initial = this.Initials[clip.Track];

                            if (this.Filters.TryGetRefRW(binding.Value, out var filter))
                            {
                                filter.ValueRW.ReverbPreset = initial.Value.ReverbPreset;
                            }

                            animated.Value = new AudioReverbFilterBlend
                            {
                                DryLevel = initial.Value.DryLevel,
                                Room = initial.Value.Room,
                                RoomHF = initial.Value.RoomHF,
                                RoomLF = initial.Value.RoomLF,
                                DecayTime = initial.Value.DecayTime,
                                DecayHFRatio = initial.Value.DecayHFRatio,
                                ReflectionsLevel = initial.Value.ReflectionsLevel,
                                ReflectionsDelay = initial.Value.ReflectionsDelay,
                                ReverbLevel = initial.Value.ReverbLevel,
                                ReverbDelay = initial.Value.ReverbDelay,
                                HFReference = initial.Value.HFReference,
                                LFReference = initial.Value.LFReference,
                                Diffusion = initial.Value.Diffusion,
                                Density = initial.Value.Density,
                            };
                            break;
                        }

                        case AudioReverbFilterClipType.Animated:
                        {
                            if (clipData.Data.OverrideReverbPreset && this.Filters.TryGetRefRW(binding.Value, out var filterRef))
                            {
                                filterRef.ValueRW.ReverbPreset = clipData.Data.ReverbPreset;
                            }

                            animated.Value = new AudioReverbFilterBlend
                            {
                                DryLevel = clipData.Data.DryLevel,
                                Room = clipData.Data.Room,
                                RoomHF = clipData.Data.RoomHF,
                                RoomLF = clipData.Data.RoomLF,
                                DecayTime = clipData.Data.DecayTime,
                                DecayHFRatio = clipData.Data.DecayHFRatio,
                                ReflectionsLevel = clipData.Data.ReflectionsLevel,
                                ReflectionsDelay = clipData.Data.ReflectionsDelay,
                                ReverbLevel = clipData.Data.ReverbLevel,
                                ReverbDelay = clipData.Data.ReverbDelay,
                                HFReference = clipData.Data.HFReference,
                                LFReference = clipData.Data.LFReference,
                                Diffusion = clipData.Data.Diffusion,
                                Density = clipData.Data.Density,
                            };
                            break;
                        }

                        case AudioReverbFilterClipType.Sweep:
                        {
                            var initial = this.Initials[clip.Track];

                            if (this.Filters.TryGetRefRW(binding.Value, out var filter))
                            {
                                filter.ValueRW.ReverbPreset = initial.Value.ReverbPreset;
                            }

                            animated.Value = new AudioReverbFilterBlend
                            {
                                DryLevel = initial.Value.DryLevel,
                                Room = initial.Value.Room,
                                RoomHF = initial.Value.RoomHF,
                                RoomLF = initial.Value.RoomLF,
                                DecayTime = initial.Value.DecayTime,
                                DecayHFRatio = initial.Value.DecayHFRatio,
                                ReflectionsLevel = initial.Value.ReflectionsLevel,
                                ReflectionsDelay = initial.Value.ReflectionsDelay,
                                ReverbLevel = initial.Value.ReverbLevel,
                                ReverbDelay = initial.Value.ReverbDelay,
                                HFReference = initial.Value.HFReference,
                                LFReference = initial.Value.LFReference,
                                Diffusion = initial.Value.Diffusion,
                                Density = initial.Value.Density,
                            };
                            break;
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioReverbFilterAnimated> AnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioReverbFilterClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentLookup<AudioReverbFilterInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioReverbFilterAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var clipDatas = (AudioReverbFilterClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    if (clipData.Type != AudioReverbFilterClipType.Sweep)
                    {
                        continue;
                    }

                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    ref readonly var localTime = ref localTimes[entityIndexInChunk];

                    var initial = this.Initials.TryGetComponent(clip.Track, out var initialData)
                        ? initialData.Value
                        : default;

                    var reverbLevel = AudioCurveSweepUtility.Evaluate(
                        ref clipData.Sweep,
                        (float)localTime.Value,
                        ref clipBlobCaches[entityIndexInChunk],
                        initial.ReverbLevel);

                    animated.Value = new AudioReverbFilterBlend
                    {
                        DryLevel = initial.DryLevel,
                        Room = initial.Room,
                        RoomHF = initial.RoomHF,
                        RoomLF = initial.RoomLF,
                        DecayTime = initial.DecayTime,
                        DecayHFRatio = initial.DecayHFRatio,
                        ReflectionsLevel = initial.ReflectionsLevel,
                        ReflectionsDelay = initial.ReflectionsDelay,
                        ReverbLevel = reverbLevel,
                        ReverbDelay = initial.ReverbDelay,
                        HFReference = initial.HFReference,
                        LFReference = initial.LFReference,
                        Diffusion = initial.Diffusion,
                        Density = initial.Density,
                    };
                }
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<AudioReverbFilterBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<AudioReverbFilterData> Filters;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Filters.TryGetRefRW(entity, out var filter))
                {
                    return;
                }

                ref var value = ref filter.ValueRW;
                var current = new AudioReverbFilterBlend
                {
                    DryLevel = value.DryLevel,
                    Room = value.Room,
                    RoomHF = value.RoomHF,
                    RoomLF = value.RoomLF,
                    DecayTime = value.DecayTime,
                    DecayHFRatio = value.DecayHFRatio,
                    ReflectionsLevel = value.ReflectionsLevel,
                    ReflectionsDelay = value.ReflectionsDelay,
                    ReverbLevel = value.ReverbLevel,
                    ReverbDelay = value.ReverbDelay,
                    HFReference = value.HFReference,
                    LFReference = value.LFReference,
                    Diffusion = value.Diffusion,
                    Density = value.Density,
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(AudioReverbFilterMixer));
                value.DryLevel = blended.DryLevel;
                value.Room = blended.Room;
                value.RoomHF = blended.RoomHF;
                value.RoomLF = blended.RoomLF;
                value.DecayTime = blended.DecayTime;
                value.DecayHFRatio = blended.DecayHFRatio;
                value.ReflectionsLevel = blended.ReflectionsLevel;
                value.ReflectionsDelay = blended.ReflectionsDelay;
                value.ReverbLevel = blended.ReverbLevel;
                value.ReverbDelay = blended.ReverbDelay;
                value.HFReference = blended.HFReference;
                value.LFReference = blended.LFReference;
                value.Diffusion = blended.Diffusion;
                value.Density = blended.Density;
            }
        }

        private struct AudioReverbFilterMixer : IMixer<AudioReverbFilterBlend>
        {
            public AudioReverbFilterBlend Lerp(in AudioReverbFilterBlend a, in AudioReverbFilterBlend b, in float s)
            {
                return new AudioReverbFilterBlend
                {
                    DryLevel = math.lerp(a.DryLevel, b.DryLevel, s),
                    Room = math.lerp(a.Room, b.Room, s),
                    RoomHF = math.lerp(a.RoomHF, b.RoomHF, s),
                    RoomLF = math.lerp(a.RoomLF, b.RoomLF, s),
                    DecayTime = math.lerp(a.DecayTime, b.DecayTime, s),
                    DecayHFRatio = math.lerp(a.DecayHFRatio, b.DecayHFRatio, s),
                    ReflectionsLevel = math.lerp(a.ReflectionsLevel, b.ReflectionsLevel, s),
                    ReflectionsDelay = math.lerp(a.ReflectionsDelay, b.ReflectionsDelay, s),
                    ReverbLevel = math.lerp(a.ReverbLevel, b.ReverbLevel, s),
                    ReverbDelay = math.lerp(a.ReverbDelay, b.ReverbDelay, s),
                    HFReference = math.lerp(a.HFReference, b.HFReference, s),
                    LFReference = math.lerp(a.LFReference, b.LFReference, s),
                    Diffusion = math.lerp(a.Diffusion, b.Diffusion, s),
                    Density = math.lerp(a.Density, b.Density, s),
                };
            }

            public AudioReverbFilterBlend Add(in AudioReverbFilterBlend a, in AudioReverbFilterBlend b)
            {
                return new AudioReverbFilterBlend
                {
                    DryLevel = a.DryLevel + b.DryLevel,
                    Room = a.Room + b.Room,
                    RoomHF = a.RoomHF + b.RoomHF,
                    RoomLF = a.RoomLF + b.RoomLF,
                    DecayTime = a.DecayTime + b.DecayTime,
                    DecayHFRatio = a.DecayHFRatio + b.DecayHFRatio,
                    ReflectionsLevel = a.ReflectionsLevel + b.ReflectionsLevel,
                    ReflectionsDelay = a.ReflectionsDelay + b.ReflectionsDelay,
                    ReverbLevel = a.ReverbLevel + b.ReverbLevel,
                    ReverbDelay = a.ReverbDelay + b.ReverbDelay,
                    HFReference = a.HFReference + b.HFReference,
                    LFReference = a.LFReference + b.LFReference,
                    Diffusion = a.Diffusion + b.Diffusion,
                    Density = a.Density + b.Density,
                };
            }
        }
    }
}
#endif
