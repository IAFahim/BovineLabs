// <copyright file="CMVolumeSettingsTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Core.Jobs;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Cinemachine;
    using BovineLabs.Vibe.Jobs;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine.Rendering;

    /// <summary>
    /// Blends Cinemachine volume settings timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMVolumeSettingsTrackSystem : ISystem
    {
        private TrackLifeImpl<CMVolumeSettings, CMVolumeSettingsInitial> volumeLife;
        private TrackBlendImpl<CMVolumeSettingsBlend, CMVolumeSettingsAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMVolumeSettingsClipData>();
            this.volumeLife.OnCreate(ref state);
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
            this.volumeLife.OnUpdate(ref state);

            var clipActivateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<CMVolumeSettingsAnimated>()
                .WithAll<CMVolumeSettingsClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMVolumeSettingsAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMVolumeSettingsClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                VolumeSettings = SystemAPI.GetComponentLookup<CMVolumeSettings>(),
                Initials = SystemAPI.GetComponentLookup<CMVolumeSettingsInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                VolumeSettings = SystemAPI.GetComponentLookup<CMVolumeSettings>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMVolumeSettingsAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMVolumeSettingsClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMVolumeSettings> VolumeSettings;

            [ReadOnly]
            public ComponentLookup<CMVolumeSettingsInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMVolumeSettingsAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMVolumeSettingsClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var trackBindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk];
                    ref var clipBlob = ref clipData.Value.Value;
                    ref readonly var trackBinding = ref trackBindings[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipBlob.Type switch
                    {
                        CMVolumeSettingsClipType.Initial => this.SelectInitialValue(clip.Track, trackBinding.Value),
                        CMVolumeSettingsClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, clipBlob, clipData.Profile),
                        _ => animated.Value,
                    };
                }
            }

            private CMVolumeSettingsBlend SelectInitialValue(Entity trackEntity, Entity boundEntity)
            {
                var initial = this.Initials[trackEntity];

                if (this.VolumeSettings.TryGetRefRW(boundEntity, out var settings))
                {
                    ref var value = ref settings.ValueRW;
                    value.FocusTracking = initial.Value.FocusTracking;
                    value.FocusTarget = initial.Value.FocusTarget;
                    value.Profile = initial.Value.Profile;
                }

                return new CMVolumeSettingsBlend
                {
                    Weight = math.clamp(initial.Value.Weight, 0f, 1f),
                    FocusOffset = initial.Value.FocusOffset,
                };
            }

            private CMVolumeSettingsBlend SelectAnimatedValue(
                Entity boundEntity, in CMVolumeSettingsClipBlob clipData, UnityObjectRef<VolumeProfile> profile)
            {
                if (this.VolumeSettings.TryGetRefRW(boundEntity, out var settings))
                {
                    ref var value = ref settings.ValueRW;
                    value.FocusTracking = clipData.FocusTracking;

                    if (clipData.OverrideFocusTarget)
                    {
                        value.FocusTarget = clipData.FocusTarget;
                    }

                    if (clipData.OverrideProfile)
                    {
                        value.Profile = profile;
                    }
                }

                return new CMVolumeSettingsBlend
                {
                    Weight = math.clamp(clipData.Weight, 0f, 1f),
                    FocusOffset = clipData.FocusOffset,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMVolumeSettingsBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMVolumeSettings> VolumeSettings;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.VolumeSettings.TryGetRefRW(entity, out var settings))
                {
                    return;
                }

                ref var value = ref settings.ValueRW;
                var current = new CMVolumeSettingsBlend
                {
                    Weight = math.clamp(value.Weight, 0f, 1f),
                    FocusOffset = value.FocusOffset,
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(CMVolumeSettingsMixer));
                value.Weight = math.clamp(blended.Weight, 0f, 1f);
                value.FocusOffset = blended.FocusOffset;
            }
        }

        private struct CMVolumeSettingsMixer : IMixer<CMVolumeSettingsBlend>
        {
            public CMVolumeSettingsBlend Lerp(in CMVolumeSettingsBlend a, in CMVolumeSettingsBlend b, in float s)
            {
                return new CMVolumeSettingsBlend
                {
                    Weight = math.lerp(a.Weight, b.Weight, s),
                    FocusOffset = math.lerp(a.FocusOffset, b.FocusOffset, s),
                };
            }

            public CMVolumeSettingsBlend Add(in CMVolumeSettingsBlend a, in CMVolumeSettingsBlend b)
            {
                return new CMVolumeSettingsBlend
                {
                    Weight = a.Weight + b.Weight,
                    FocusOffset = a.FocusOffset + b.FocusOffset,
                };
            }
        }
    }
}
#endif
