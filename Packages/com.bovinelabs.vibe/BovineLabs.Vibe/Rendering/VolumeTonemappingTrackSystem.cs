// <copyright file="VolumeTonemappingTrackSystem.cs" company="BovineLabs">
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
    /// Blends tonemapping clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeTonemappingTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeTonemapping, VolumeTonemappingInitial> lifeImpl;
        private TrackBlendImpl<VolumeTonemappingBlend, VolumeTonemappingAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeTonemappingClipData>();
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
                .WithAllRW<VolumeTonemappingAnimated>()
                .WithAll<VolumeTonemappingClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeTonemappingAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeTonemappingClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeTonemappingInitial>(true),
                Tonemappings = SystemAPI.GetComponentLookup<VolumeTonemapping>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Tonemappings = SystemAPI.GetComponentLookup<VolumeTonemapping>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeTonemappingBlend CreateBlend(in VolumeTonemapping data, bool useOverrides)
        {
            return new VolumeTonemappingBlend
            {
                HueShiftAmount = data.HueShiftAmount,
                PaperWhite = data.PaperWhite,
                MinNits = data.MinNits,
                MaxNits = data.MaxNits,
                HueShiftAmountOverride = useOverrides && data.HueShiftAmountOverride,
                PaperWhiteOverride = useOverrides && data.PaperWhiteOverride,
                MinNitsOverride = useOverrides && data.MinNitsOverride,
                MaxNitsOverride = useOverrides && data.MaxNitsOverride,
            };
        }

        private static VolumeTonemappingBlend CreateBlend(in VolumeTonemappingConstantData data)
        {
            return new VolumeTonemappingBlend
            {
                HueShiftAmount = data.HueShiftAmount,
                PaperWhite = data.PaperWhite,
                MinNits = data.MinNits,
                MaxNits = data.MaxNits,
                HueShiftAmountOverride = data.HueShiftAmountOverride,
                PaperWhiteOverride = data.PaperWhiteOverride,
                MinNitsOverride = data.MinNitsOverride,
                MaxNitsOverride = data.MaxNitsOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeTonemappingAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeTonemappingClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeTonemappingInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeTonemapping> Tonemappings;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeTonemappingAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeTonemappingClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeTonemappingClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Tonemappings);
                            break;
                        case VolumeTonemappingClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyConstantOverrides(binding.Value, in clipBlob.Constant, ref this.Tonemappings);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyConstantOverrides(Entity entity, in VolumeTonemappingConstantData data, ref ComponentLookup<VolumeTonemapping> tonemappings)
            {
                if (!tonemappings.TryGetRefRW(entity, out var tonemapping))
                {
                    return;
                }

                ref var value = ref tonemapping.ValueRW;
                value.Active = data.Active;

                if (data.ModeOverride)
                {
                    value.ModeOverride = true;
                    value.Mode = data.Mode;
                }

                if (data.NeutralHDRRangeReductionModeOverride)
                {
                    value.NeutralHDRRangeReductionModeOverride = true;
                    value.NeutralHDRRangeReductionMode = data.NeutralHDRRangeReductionMode;
                }

                if (data.AcesPresetOverride)
                {
                    value.AcesPresetOverride = true;
                    value.AcesPreset = data.AcesPreset;
                }

                if (data.DetectPaperWhiteOverride)
                {
                    value.DetectPaperWhiteOverride = true;
                    value.DetectPaperWhite = data.DetectPaperWhite;
                }

                if (data.DetectBrightnessLimitsOverride)
                {
                    value.DetectBrightnessLimitsOverride = true;
                    value.DetectBrightnessLimits = data.DetectBrightnessLimits;
                }
            }

            private static void ApplyInitial(Entity entity, in VolumeTonemapping initial, ref ComponentLookup<VolumeTonemapping> tonemappings)
            {
                if (!tonemappings.TryGetRefRW(entity, out var tonemapping))
                {
                    return;
                }

                ref var value = ref tonemapping.ValueRW;
                value.Mode = initial.Mode;
                value.NeutralHDRRangeReductionMode = initial.NeutralHDRRangeReductionMode;
                value.AcesPreset = initial.AcesPreset;
                value.HueShiftAmount = initial.HueShiftAmount;
                value.DetectPaperWhite = initial.DetectPaperWhite;
                value.PaperWhite = initial.PaperWhite;
                value.DetectBrightnessLimits = initial.DetectBrightnessLimits;
                value.MinNits = initial.MinNits;
                value.MaxNits = initial.MaxNits;
                value.Active = initial.Active;
                value.ModeOverride = initial.ModeOverride;
                value.NeutralHDRRangeReductionModeOverride = initial.NeutralHDRRangeReductionModeOverride;
                value.AcesPresetOverride = initial.AcesPresetOverride;
                value.HueShiftAmountOverride = initial.HueShiftAmountOverride;
                value.DetectPaperWhiteOverride = initial.DetectPaperWhiteOverride;
                value.PaperWhiteOverride = initial.PaperWhiteOverride;
                value.DetectBrightnessLimitsOverride = initial.DetectBrightnessLimitsOverride;
                value.MinNitsOverride = initial.MinNitsOverride;
                value.MaxNitsOverride = initial.MaxNitsOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeTonemappingBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeTonemapping> Tonemappings;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Tonemappings.TryGetRefRW(entity, out var tonemapping))
                {
                    return;
                }

                ref var value = ref tonemapping.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeTonemappingMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeTonemapping tonemapping, in VolumeTonemappingBlend blend)
            {
                if (blend.HueShiftAmountOverride)
                {
                    tonemapping.HueShiftAmountOverride = true;
                    tonemapping.HueShiftAmount = blend.HueShiftAmount;
                }

                if (blend.PaperWhiteOverride)
                {
                    tonemapping.PaperWhiteOverride = true;
                    tonemapping.PaperWhite = blend.PaperWhite;
                }

                if (blend.MinNitsOverride)
                {
                    tonemapping.MinNitsOverride = true;
                    tonemapping.MinNits = blend.MinNits;
                }

                if (blend.MaxNitsOverride)
                {
                    tonemapping.MaxNitsOverride = true;
                    tonemapping.MaxNits = blend.MaxNits;
                }
            }
        }

        private struct VolumeTonemappingMixer : IMixer<VolumeTonemappingBlend>
        {
            public VolumeTonemappingBlend Lerp(in VolumeTonemappingBlend a, in VolumeTonemappingBlend b, in float s)
            {
                return new VolumeTonemappingBlend
                {
                    HueShiftAmount = MixUtil.LerpFloat(a.HueShiftAmount, b.HueShiftAmount, s, a.HueShiftAmountOverride, b.HueShiftAmountOverride),
                    PaperWhite = MixUtil.LerpFloat(a.PaperWhite, b.PaperWhite, s, a.PaperWhiteOverride, b.PaperWhiteOverride),
                    MinNits = MixUtil.LerpFloat(a.MinNits, b.MinNits, s, a.MinNitsOverride, b.MinNitsOverride),
                    MaxNits = MixUtil.LerpFloat(a.MaxNits, b.MaxNits, s, a.MaxNitsOverride, b.MaxNitsOverride),
                    HueShiftAmountOverride = a.HueShiftAmountOverride || b.HueShiftAmountOverride,
                    PaperWhiteOverride = a.PaperWhiteOverride || b.PaperWhiteOverride,
                    MinNitsOverride = a.MinNitsOverride || b.MinNitsOverride,
                    MaxNitsOverride = a.MaxNitsOverride || b.MaxNitsOverride,
                };
            }

            public VolumeTonemappingBlend Add(in VolumeTonemappingBlend a, in VolumeTonemappingBlend b)
            {
                return new VolumeTonemappingBlend
                {
                    HueShiftAmount = MixUtil.AddFloat(a.HueShiftAmount, b.HueShiftAmount, a.HueShiftAmountOverride, b.HueShiftAmountOverride),
                    PaperWhite = MixUtil.AddFloat(a.PaperWhite, b.PaperWhite, a.PaperWhiteOverride, b.PaperWhiteOverride),
                    MinNits = MixUtil.AddFloat(a.MinNits, b.MinNits, a.MinNitsOverride, b.MinNitsOverride),
                    MaxNits = MixUtil.AddFloat(a.MaxNits, b.MaxNits, a.MaxNitsOverride, b.MaxNitsOverride),
                    HueShiftAmountOverride = a.HueShiftAmountOverride || b.HueShiftAmountOverride,
                    PaperWhiteOverride = a.PaperWhiteOverride || b.PaperWhiteOverride,
                    MinNitsOverride = a.MinNitsOverride || b.MinNitsOverride,
                    MaxNitsOverride = a.MaxNitsOverride || b.MaxNitsOverride,
                };
            }
        }
    }
}
#endif
