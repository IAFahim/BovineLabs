// <copyright file="AudioSourceTrackSystem.cs" company="BovineLabs">
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
    /// Blends audio source data clips and applies audio source clip assignments.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct AudioSourceTrackSystem : ISystem
    {
        private TrackLifeImpl<AudioSourceData, AudioSourceDataInitial> dataLifeImpl;
        private TrackBlendImpl<AudioSourceDataBlend, AudioSourceDataAnimated> dataImpl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<AudioSourceDataClipData, AudioSourceClipData>()
                .Build(ref state);

            state.RequireForUpdate(query);
            this.dataLifeImpl.OnCreate(ref state);
            this.dataImpl.OnCreate(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            this.dataImpl.OnDestroy(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.dataLifeImpl.OnUpdate(ref state);

            var clipActivateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioSourceDataAnimated>()
                .WithAll<AudioSourceDataClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new DataClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioSourceDataAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioSourceDataClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<AudioSourceDataInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AudioSourceDataAnimated>()
                .WithAll<AudioSourceDataClipData, Clip, ClipActive, LocalTime, ClipBlobCurveCache>()
                .Build();

            state.Dependency = new DataClipUpdateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<AudioSourceDataAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<AudioSourceDataClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                TrackInitials = SystemAPI.GetComponentLookup<AudioSourceDataInitial>(true),
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var blendData = this.dataImpl.Update(ref state);

            state.Dependency = new DataWriteJob
            {
                BlendData = blendData,
                AudioSources = SystemAPI.GetComponentLookup<AudioSourceData>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);

            new TrackDeactivateJob { AudioSources = SystemAPI.GetComponentLookup<AudioSourceDataExtended>() }.Schedule();
            new TrackActivateJob { AudioSources = SystemAPI.GetComponentLookup<AudioSourceDataExtended>(true) }.ScheduleParallel();

            new ClipActivateJob { AudioSources = SystemAPI.GetComponentLookup<AudioSourceDataExtended>() }.ScheduleParallel();
        }

        private static AudioSourceDataBlend CreateVolumeBlend(float volume)
        {
            return new AudioSourceDataBlend
            {
                Volume = volume,
                EnableVolume = true,
            };
        }

        private static AudioSourceDataBlend CreatePitchBlend(float pitch)
        {
            return new AudioSourceDataBlend
            {
                Pitch = pitch,
                EnablePitch = true,
            };
        }

        [BurstCompile]
        private struct DataClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioSourceDataAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioSourceDataClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<AudioSourceDataInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioSourceDataAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (AudioSourceDataClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        AudioSourceDataClipType.Initial => this.SelectInitialValue(clip.Track),
                        AudioSourceDataClipType.Animated => new AudioSourceDataBlend
                        {
                            Volume = clipData.Data.Volume,
                            Pitch = clipData.Data.Pitch,
                            EnableVolume = true,
                            EnablePitch = true,
                        },
                        AudioSourceDataClipType.VolumeSweep => CreateVolumeBlend(this.SelectInitialVolume(clip.Track)),
                        AudioSourceDataClipType.PitchSweep => CreatePitchBlend(this.SelectInitialPitch(clip.Track)),
                        _ => animated.Value,
                    };
                }
            }

            private AudioSourceDataBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return new AudioSourceDataBlend
                {
                    Volume = initial.Value.Volume,
                    Pitch = initial.Value.Pitch,
                    EnableVolume = true,
                    EnablePitch = true,
                };
            }

            private float SelectInitialVolume(Entity trackEntity)
            {
                return this.Initials.TryGetComponent(trackEntity, out var initial) ? initial.Value.Volume : 1f;
            }

            private float SelectInitialPitch(Entity trackEntity)
            {
                return this.Initials.TryGetComponent(trackEntity, out var initial) ? initial.Value.Pitch : 1f;
            }
        }

        [BurstCompile]
        private struct DataClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<AudioSourceDataAnimated> AnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<AudioSourceDataClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentLookup<AudioSourceDataInitial> TrackInitials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (AudioSourceDataAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var clipDatas = (AudioSourceDataClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk].Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    ref readonly var localTime = ref localTimes[entityIndexInChunk];

                    switch (clipData.Type)
                    {
                        case AudioSourceDataClipType.VolumeSweep:
                        {
                            var baseValue = this.TrackInitials.TryGetComponent(clip.Track, out var initial)
                                ? initial.Value.Volume
                                : 1f;

                            var volume = AudioCurveSweepUtility.Evaluate(
                                ref clipData.Sweep,
                                (float)localTime.Value,
                                ref clipBlobCaches[entityIndexInChunk],
                                baseValue);

                            animated.Value = CreateVolumeBlend(volume);
                            break;
                        }

                        case AudioSourceDataClipType.PitchSweep:
                        {
                            var baseValue = this.TrackInitials.TryGetComponent(clip.Track, out var initial)
                                ? initial.Value.Pitch
                                : 1f;

                            var pitch = AudioCurveSweepUtility.Evaluate(
                                ref clipData.Sweep,
                                (float)localTime.Value,
                                ref clipBlobCaches[entityIndexInChunk],
                                baseValue);

                            animated.Value = CreatePitchBlend(pitch);
                            break;
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct DataWriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<AudioSourceDataBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<AudioSourceData> AudioSources;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.AudioSources.TryGetRefRW(entity, out var audioSource))
                {
                    return;
                }

                ref var value = ref audioSource.ValueRW;
                var current = new AudioSourceDataBlend
                {
                    Volume = value.Volume,
                    Pitch = value.Pitch,
                    EnableVolume = true,
                    EnablePitch = true,
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(AudioSourceDataMixer));
                if (blended.EnableVolume)
                {
                    value.Volume = blended.Volume;
                }

                if (blended.EnablePitch)
                {
                    value.Pitch = blended.Pitch;
                }
            }
        }

        private struct AudioSourceDataMixer : IMixer<AudioSourceDataBlend>
        {
            public AudioSourceDataBlend Lerp(in AudioSourceDataBlend a, in AudioSourceDataBlend b, in float s)
            {
                var volume = BlendValue(a.Volume, a.EnableVolume, b.Volume, b.EnableVolume, s, out var enableVolume);
                var pitch = BlendValue(a.Pitch, a.EnablePitch, b.Pitch, b.EnablePitch, s, out var enablePitch);

                return new AudioSourceDataBlend
                {
                    Volume = volume,
                    Pitch = pitch,
                    EnableVolume = enableVolume,
                    EnablePitch = enablePitch,
                };
            }

            public AudioSourceDataBlend Add(in AudioSourceDataBlend a, in AudioSourceDataBlend b)
            {
                var volume = AddValue(a.Volume, a.EnableVolume, b.Volume, b.EnableVolume, out var enableVolume);
                var pitch = AddValue(a.Pitch, a.EnablePitch, b.Pitch, b.EnablePitch, out var enablePitch);

                return new AudioSourceDataBlend
                {
                    Volume = volume,
                    Pitch = pitch,
                    EnableVolume = enableVolume,
                    EnablePitch = enablePitch,
                };
            }

            private static float BlendValue(float a, bool aEnabled, float b, bool bEnabled, float s, out bool enabled)
            {
                if (aEnabled)
                {
                    if (bEnabled)
                    {
                        enabled = true;
                        return math.lerp(a, b, s);
                    }

                    enabled = true;
                    return a;
                }

                if (bEnabled)
                {
                    enabled = true;
                    return b;
                }

                enabled = false;
                return 0f;
            }

            private static float AddValue(float a, bool aEnabled, float b, bool bEnabled, out bool enabled)
            {
                if (aEnabled)
                {
                    if (bEnabled)
                    {
                        enabled = true;
                        return a + b;
                    }

                    enabled = true;
                    return a;
                }

                if (bEnabled)
                {
                    enabled = true;
                    return b;
                }

                enabled = false;
                return 0f;
            }
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        private partial struct TrackActivateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<AudioSourceDataExtended> AudioSources;

            private void Execute(ref AudioSourceClipInitial initial, in TrackBinding trackBinding)
            {
                if (!this.AudioSources.TryGetComponent(trackBinding.Value, out var audioSource))
                {
                    return;
                }

                initial.Clip = audioSource.Clip;
            }
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))]
        [WithAll(typeof(TimelineActivePrevious))]
        [WithDisabled(typeof(TimelineActive))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            public ComponentLookup<AudioSourceDataExtended> AudioSources;

            private void Execute(ref AudioSourceClipInitial initial, in TrackBinding trackBinding)
            {
                if (!this.AudioSources.TryGetRefRW(trackBinding.Value, out var audioSource))
                {
                    return;
                }

                audioSource.ValueRW.Clip = initial.Clip;
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct ClipActivateJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<AudioSourceDataExtended> AudioSources;

            private void Execute(in AudioSourceClipData clipData, in TrackBinding trackBinding)
            {
                if (!this.AudioSources.TryGetRefRW(trackBinding.Value, out var audioSource))
                {
                    return;
                }

                audioSource.ValueRW.Clip = clipData.Clip;
            }
        }
    }
}
#endif
