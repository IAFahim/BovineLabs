// <copyright file="VolumeChromaticAberrationTrackSystem.cs" company="BovineLabs">
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

    /// <summary>
    /// Blends chromatic aberration clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeChromaticAberrationTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeChromaticAberration, VolumeChromaticAberrationInitial> lifeImpl;
        private TrackBlendImpl<VolumeChromaticAberrationBlend, VolumeChromaticAberrationAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeChromaticAberrationClipData>();
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
                .WithAllRW<VolumeChromaticAberrationAnimated>()
                .WithAll<VolumeChromaticAberrationClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeChromaticAberrationAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeChromaticAberrationClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeChromaticAberrationInitial>(true),
                Aberrations = SystemAPI.GetComponentLookup<VolumeChromaticAberration>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Aberrations = SystemAPI.GetComponentLookup<VolumeChromaticAberration>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeChromaticAberrationBlend CreateBlend(in VolumeChromaticAberration data, bool useOverrides)
        {
            return new VolumeChromaticAberrationBlend
            {
                Intensity = data.Intensity,
                IntensityOverride = useOverrides && data.IntensityOverride,
            };
        }

        private static VolumeChromaticAberrationBlend CreateBlend(in VolumeChromaticAberrationConstantData data)
        {
            return new VolumeChromaticAberrationBlend
            {
                Intensity = data.Intensity,
                IntensityOverride = data.IntensityOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeChromaticAberrationAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeChromaticAberrationClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeChromaticAberrationInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeChromaticAberration> Aberrations;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeChromaticAberrationAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeChromaticAberrationClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeChromaticAberrationClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Aberrations);
                            break;
                        case VolumeChromaticAberrationClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyActive(binding.Value, clipBlob.Constant.Active, ref this.Aberrations);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyActive(Entity entity, bool active, ref ComponentLookup<VolumeChromaticAberration> aberrations)
            {
                if (!aberrations.TryGetRefRW(entity, out var aberration))
                {
                    return;
                }

                aberration.ValueRW.Active = active;
            }

            private static void ApplyInitial(Entity entity, in VolumeChromaticAberration initial, ref ComponentLookup<VolumeChromaticAberration> aberrations)
            {
                if (!aberrations.TryGetRefRW(entity, out var aberration))
                {
                    return;
                }

                ref var value = ref aberration.ValueRW;
                value.Intensity = initial.Intensity;
                value.IntensityOverride = initial.IntensityOverride;
                value.Active = initial.Active;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeChromaticAberrationBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeChromaticAberration> Aberrations;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Aberrations.TryGetRefRW(entity, out var aberration))
                {
                    return;
                }

                ref var value = ref aberration.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeChromaticAberrationMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeChromaticAberration aberration, in VolumeChromaticAberrationBlend blend)
            {
                if (blend.IntensityOverride)
                {
                    aberration.IntensityOverride = true;
                    aberration.Intensity = blend.Intensity;
                }
            }
        }

        private struct VolumeChromaticAberrationMixer : IMixer<VolumeChromaticAberrationBlend>
        {
            public VolumeChromaticAberrationBlend Lerp(in VolumeChromaticAberrationBlend a, in VolumeChromaticAberrationBlend b, in float s)
            {
                return new VolumeChromaticAberrationBlend
                {
                    Intensity = MixUtil.LerpFloat(a.Intensity, b.Intensity, s, a.IntensityOverride, b.IntensityOverride),
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                };
            }

            public VolumeChromaticAberrationBlend Add(in VolumeChromaticAberrationBlend a, in VolumeChromaticAberrationBlend b)
            {
                return new VolumeChromaticAberrationBlend
                {
                    Intensity = MixUtil.AddFloat(a.Intensity, b.Intensity, a.IntensityOverride, b.IntensityOverride),
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                };
            }
        }
    }
}
#endif
