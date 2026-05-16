// <copyright file="VolumeSettingsTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Volume;
    using BovineLabs.Core.Jobs;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Volume;
    using BovineLabs.Vibe.Jobs;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Blends volume settings clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeSettingsTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeSettings, VolumeSettingsInitial> lifeImpl;
        private TrackBlendImpl<VolumeSettingsBlend, VolumeSettingsAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeSettingsClipData>();
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
                .WithAllRW<VolumeSettingsAnimated>()
                .WithAll<VolumeSettingsClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeSettingsAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeSettingsClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeSettingsInitial>(true),
                Settings = SystemAPI.GetComponentLookup<VolumeSettings>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Settings = SystemAPI.GetComponentLookup<VolumeSettings>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeSettingsAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeSettingsClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeSettingsInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeSettings> Settings;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeSettingsAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeSettingsClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var bindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk];
                    ref var clipBlob = ref clipData.Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    ref readonly var binding = ref bindings[entityIndexInChunk];

                    var initial = this.Initials.TryGetComponent(clip.Track, out var initialData)
                        ? initialData.Value
                        : default;

                    switch (clipBlob.Type)
                    {
                        case VolumeSettingsClipType.Initial:
                            animated.Value = new VolumeSettingsBlend
                            {
                                Weight = math.clamp(initial.Weight, 0f, 1f),
                            };
                            ApplyInitialSettings(binding.Value, in initial, ref this.Settings);
                            break;
                        case VolumeSettingsClipType.Constant:
                            animated.Value = new VolumeSettingsBlend
                            {
                                Weight = math.clamp(clipBlob.Weight, 0f, 1f),
                            };
                            ApplyOverrides(binding.Value, in clipBlob, clipData.Profile, ref this.Settings);
                            break;
                        default:
                            animated.Value = new VolumeSettingsBlend
                            {
                                Weight = math.clamp(clipBlob.Weight, 0f, 1f),
                            };
                            break;
                    }
                }
            }

            private static void ApplyOverrides(Entity entity, in VolumeSettingsClipBlob data, UnityObjectRef<UnityEngine.Rendering.VolumeProfile> profile,
                ref ComponentLookup<VolumeSettings> settingsLookup)
            {
                if (!settingsLookup.TryGetRefRW(entity, out var settings))
                {
                    return;
                }

                ref var value = ref settings.ValueRW;

                if (data.OverridePriority)
                {
                    value.Priority = data.Priority;
                }

                if (data.OverrideBlendDistance)
                {
                    value.BlendDistance = data.BlendDistance;
                }

                if (data.OverrideIsGlobal)
                {
                    value.IsGlobal = data.IsGlobal;
                }

                if (data.OverrideProfile)
                {
                    value.Profile = profile;
                }
            }

            private static void ApplyInitialSettings(Entity entity, in VolumeSettings initial, ref ComponentLookup<VolumeSettings> settingsLookup)
            {
                if (!settingsLookup.TryGetRefRW(entity, out var settings))
                {
                    return;
                }

                ref var value = ref settings.ValueRW;
                value.Priority = initial.Priority;
                value.BlendDistance = initial.BlendDistance;
                value.IsGlobal = initial.IsGlobal;
                value.Profile = initial.Profile;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeSettingsBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeSettings> Settings;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Settings.TryGetRefRW(entity, out var settings))
                {
                    return;
                }

                ref var value = ref settings.ValueRW;
                var current = new VolumeSettingsBlend
                {
                    Weight = math.clamp(value.Weight, 0f, 1f),
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeSettingsMixer));
                value.Weight = math.clamp(blended.Weight, 0f, 1f);
            }
        }

        private struct VolumeSettingsMixer : IMixer<VolumeSettingsBlend>
        {
            public VolumeSettingsBlend Lerp(in VolumeSettingsBlend a, in VolumeSettingsBlend b, in float s)
            {
                return new VolumeSettingsBlend
                {
                    Weight = math.lerp(a.Weight, b.Weight, s),
                };
            }

            public VolumeSettingsBlend Add(in VolumeSettingsBlend a, in VolumeSettingsBlend b)
            {
                return new VolumeSettingsBlend
                {
                    Weight = a.Weight + b.Weight,
                };
            }
        }
    }
}
#endif
