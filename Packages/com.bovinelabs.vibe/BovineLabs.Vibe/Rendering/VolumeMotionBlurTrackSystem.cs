// <copyright file="VolumeMotionBlurTrackSystem.cs" company="BovineLabs">
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
    /// Blends motion blur clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeMotionBlurTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeMotionBlur, VolumeMotionBlurInitial> lifeImpl;
        private TrackBlendImpl<VolumeMotionBlurBlend, VolumeMotionBlurAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeMotionBlurClipData>();
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
                .WithAllRW<VolumeMotionBlurAnimated>()
                .WithAll<VolumeMotionBlurClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeMotionBlurAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeMotionBlurClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeMotionBlurInitial>(true),
                Blurs = SystemAPI.GetComponentLookup<VolumeMotionBlur>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Blurs = SystemAPI.GetComponentLookup<VolumeMotionBlur>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeMotionBlurBlend CreateBlend(in VolumeMotionBlur data, bool useOverrides)
        {
            return new VolumeMotionBlurBlend
            {
                Intensity = data.Intensity,
                Clamp = data.Clamp,
                IntensityOverride = useOverrides && data.IntensityOverride,
                ClampOverride = useOverrides && data.ClampOverride,
            };
        }

        private static VolumeMotionBlurBlend CreateBlend(in VolumeMotionBlurConstantData data)
        {
            return new VolumeMotionBlurBlend
            {
                Intensity = data.Intensity,
                Clamp = data.Clamp,
                IntensityOverride = data.IntensityOverride,
                ClampOverride = data.ClampOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeMotionBlurAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeMotionBlurClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeMotionBlurInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeMotionBlur> Blurs;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeMotionBlurAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeMotionBlurClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeMotionBlurClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Blurs);
                            break;
                        case VolumeMotionBlurClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyConstantOverrides(binding.Value, in clipBlob.Constant, ref this.Blurs);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyConstantOverrides(Entity entity, in VolumeMotionBlurConstantData data, ref ComponentLookup<VolumeMotionBlur> blurs)
            {
                if (!blurs.TryGetRefRW(entity, out var blur))
                {
                    return;
                }

                ref var value = ref blur.ValueRW;
                value.Active = data.Active;

                if (data.ModeOverride)
                {
                    value.ModeOverride = true;
                    value.Mode = data.Mode;
                }

                if (data.QualityOverride)
                {
                    value.QualityOverride = true;
                    value.Quality = data.Quality;
                }
            }

            private static void ApplyInitial(Entity entity, in VolumeMotionBlur initial, ref ComponentLookup<VolumeMotionBlur> blurs)
            {
                if (!blurs.TryGetRefRW(entity, out var blur))
                {
                    return;
                }

                ref var value = ref blur.ValueRW;
                value.Mode = initial.Mode;
                value.Quality = initial.Quality;
                value.Intensity = initial.Intensity;
                value.Clamp = initial.Clamp;
                value.Active = initial.Active;
                value.ModeOverride = initial.ModeOverride;
                value.QualityOverride = initial.QualityOverride;
                value.IntensityOverride = initial.IntensityOverride;
                value.ClampOverride = initial.ClampOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeMotionBlurBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeMotionBlur> Blurs;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Blurs.TryGetRefRW(entity, out var blur))
                {
                    return;
                }

                ref var value = ref blur.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeMotionBlurMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeMotionBlur blur, in VolumeMotionBlurBlend blend)
            {
                if (blend.IntensityOverride)
                {
                    blur.IntensityOverride = true;
                    blur.Intensity = blend.Intensity;
                }

                if (blend.ClampOverride)
                {
                    blur.ClampOverride = true;
                    blur.Clamp = blend.Clamp;
                }
            }
        }

        private struct VolumeMotionBlurMixer : IMixer<VolumeMotionBlurBlend>
        {
            public VolumeMotionBlurBlend Lerp(in VolumeMotionBlurBlend a, in VolumeMotionBlurBlend b, in float s)
            {
                return new VolumeMotionBlurBlend
                {
                    Intensity = MixUtil.LerpFloat(a.Intensity, b.Intensity, s, a.IntensityOverride, b.IntensityOverride),
                    Clamp = MixUtil.LerpFloat(a.Clamp, b.Clamp, s, a.ClampOverride, b.ClampOverride),
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                    ClampOverride = a.ClampOverride || b.ClampOverride,
                };
            }

            public VolumeMotionBlurBlend Add(in VolumeMotionBlurBlend a, in VolumeMotionBlurBlend b)
            {
                return new VolumeMotionBlurBlend
                {
                    Intensity = MixUtil.AddFloat(a.Intensity, b.Intensity, a.IntensityOverride, b.IntensityOverride),
                    Clamp = MixUtil.AddFloat(a.Clamp, b.Clamp, a.ClampOverride, b.ClampOverride),
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                    ClampOverride = a.ClampOverride || b.ClampOverride,
                };
            }
        }
    }
}
#endif
