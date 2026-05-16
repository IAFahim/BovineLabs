// <copyright file="AudioSourceTriggerTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Audio;
    using BovineLabs.Vibe.Jobs;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Triggers audio source playback actions for timeline clips.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct AudioSourceTriggerTrackSystem : ISystem
    {
        private TrackLifeImpl<AudioSourceData, AudioSourceDataInitial> dataLifeImpl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AudioSourceTriggerClipData>();
            this.dataLifeImpl.OnCreate(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.dataLifeImpl.OnUpdate(ref state);

            state.Dependency = new TrackDeactivateJob
            {
                AudioSources = SystemAPI.GetComponentLookup<AudioSourceDataExtended>(),
            }.Schedule(state.Dependency);

            state.Dependency = new TrackActivateJob
            {
                AudioSources = SystemAPI.GetComponentLookup<AudioSourceDataExtended>(true),
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new ClipActivateJob
            {
                AudioSources = SystemAPI.GetComponentLookup<AudioSourceData>(),
                AudioSourcesExtended = SystemAPI.GetComponentLookup<AudioSourceDataExtended>(),
                AudioSourceEnableds = SystemAPI.GetComponentLookup<AudioSourceEnabled>(),
            }.ScheduleParallel(state.Dependency);
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

            private void Execute(in AudioSourceClipInitial initial, in TrackBinding trackBinding)
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
            public ComponentLookup<AudioSourceData> AudioSources;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<AudioSourceDataExtended> AudioSourcesExtended;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<AudioSourceEnabled> AudioSourceEnableds;

            private void Execute(
                Entity entity,
                in AudioSourceTriggerClipData clipComponent,
                in TrackBinding trackBinding,
                in DynamicBuffer<AudioSourceTriggerClipEntry> clips)
            {
                if (!this.AudioSources.TryGetRefRW(trackBinding.Value, out var audioSource) ||
                    !this.AudioSourcesExtended.TryGetRefRW(trackBinding.Value, out var audioSourceExtended))
                {
                    return;
                }

                ref var clipData = ref clipComponent.Value.Value;
                var seed = clipData.Seed != 0
                    ? clipData.Seed
                    : math.hash(new uint2((uint)entity.Index, (uint)trackBinding.Value.Index));

                if (seed == 0)
                {
                    seed = 1u;
                }

                var random = Random.CreateFromIndex(seed);

                var minVolume = math.min(clipData.MinVolume, clipData.MaxVolume);
                var maxVolume = math.max(clipData.MinVolume, clipData.MaxVolume);
                audioSource.ValueRW.Volume = random.NextFloat(minVolume, maxVolume);

                var minPitch = math.min(clipData.MinPitch, clipData.MaxPitch);
                var maxPitch = math.max(clipData.MinPitch, clipData.MaxPitch);
                audioSource.ValueRW.Pitch = random.NextFloat(minPitch, maxPitch);

                var currentClip = audioSourceExtended.ValueRO.Clip;
                var nextClip = currentClip;
                if (clips.Length > 0)
                {
                    var clipIndex = random.NextInt(clips.Length);
                    nextClip = clips[clipIndex].Clip;
                }

                audioSourceExtended.ValueRW.Clip = nextClip;

                var clipChanged = !currentClip.Equals(nextClip);
                var enable = clipData.Action == AudioSourceTriggerAction.Play || clipData.Action == AudioSourceTriggerAction.Unpause;
                var disable = clipData.Action == AudioSourceTriggerAction.Pause || clipData.Action == AudioSourceTriggerAction.Stop;

                if (!this.AudioSourceEnableds.HasComponent(trackBinding.Value))
                {
                    return;
                }

                if (disable)
                {
                    this.AudioSourceEnableds.SetComponentEnabled(trackBinding.Value, false);
                    return;
                }

                if (enable)
                {
                    if (clipData.ForceRestart || clipChanged)
                    {
                        this.AudioSourceEnableds.SetComponentEnabled(trackBinding.Value, false);
                    }

                    this.AudioSourceEnableds.SetComponentEnabled(trackBinding.Value, true);
                }
            }
        }
    }
}
#endif
