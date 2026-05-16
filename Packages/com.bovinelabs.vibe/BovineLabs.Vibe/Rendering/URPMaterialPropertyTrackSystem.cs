// <copyright file="URPMaterialPropertyTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Vibe
{
    using BovineLabs.Core;
    using BovineLabs.Core.Jobs;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Rendering;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Rendering;

    /// <summary>
    /// Blends URP material property clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct URPMaterialPropertyTrackSystem : ISystem
    {
        private TrackBlendImpl<URPMaterialPropertyBlend, URPMaterialPropertyAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<URPMaterialPropertyClipData>();
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
            state.Dependency = new TrackDeactivateJob
            {
                BumpScales = SystemAPI.GetComponentLookup<URPMaterialPropertyBumpScale>(),
                Cutoffs = SystemAPI.GetComponentLookup<URPMaterialPropertyCutoff>(),
                Metallics = SystemAPI.GetComponentLookup<URPMaterialPropertyMetallic>(),
                OcclusionStrengths = SystemAPI.GetComponentLookup<URPMaterialPropertyOcclusionStrength>(),
                Smoothnesses = SystemAPI.GetComponentLookup<URPMaterialPropertySmoothness>(),
                BaseColors = SystemAPI.GetComponentLookup<URPMaterialPropertyBaseColor>(),
                EmissionColors = SystemAPI.GetComponentLookup<URPMaterialPropertyEmissionColor>(),
                SpecColors = SystemAPI.GetComponentLookup<URPMaterialPropertySpecColor>(),
            }.Schedule(state.Dependency);

            state.Dependency = new TrackActivateJob
            {
                BumpScales = SystemAPI.GetComponentLookup<URPMaterialPropertyBumpScale>(true),
                Cutoffs = SystemAPI.GetComponentLookup<URPMaterialPropertyCutoff>(true),
                Metallics = SystemAPI.GetComponentLookup<URPMaterialPropertyMetallic>(true),
                OcclusionStrengths = SystemAPI.GetComponentLookup<URPMaterialPropertyOcclusionStrength>(true),
                Smoothnesses = SystemAPI.GetComponentLookup<URPMaterialPropertySmoothness>(true),
                BaseColors = SystemAPI.GetComponentLookup<URPMaterialPropertyBaseColor>(true),
                EmissionColors = SystemAPI.GetComponentLookup<URPMaterialPropertyEmissionColor>(true),
                SpecColors = SystemAPI.GetComponentLookup<URPMaterialPropertySpecColor>(true),
            }.ScheduleParallel(state.Dependency);

#if BL_CORE_EXTENSIONS
            var commandBuffer = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
#else
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
#endif

            state.Dependency = new ClipActivateJob
            {
                CommandBuffer = commandBuffer,
                TrackComponents = SystemAPI.GetComponentLookup<URPMaterialPropertyTrackComponents>(),
                BumpScales = SystemAPI.GetComponentLookup<URPMaterialPropertyBumpScale>(true),
                Cutoffs = SystemAPI.GetComponentLookup<URPMaterialPropertyCutoff>(true),
                Metallics = SystemAPI.GetComponentLookup<URPMaterialPropertyMetallic>(true),
                OcclusionStrengths = SystemAPI.GetComponentLookup<URPMaterialPropertyOcclusionStrength>(true),
                Smoothnesses = SystemAPI.GetComponentLookup<URPMaterialPropertySmoothness>(true),
                BaseColors = SystemAPI.GetComponentLookup<URPMaterialPropertyBaseColor>(true),
                EmissionColors = SystemAPI.GetComponentLookup<URPMaterialPropertyEmissionColor>(true),
                SpecColors = SystemAPI.GetComponentLookup<URPMaterialPropertySpecColor>(true),
            }.Schedule(state.Dependency);

            state.Dependency = new ClipDeactivatedJob
            {
                CommandBuffer = commandBuffer,
                TrackComponents = SystemAPI.GetComponentLookup<URPMaterialPropertyTrackComponents>(),
                BumpScales = SystemAPI.GetComponentLookup<URPMaterialPropertyBumpScale>(true),
                Cutoffs = SystemAPI.GetComponentLookup<URPMaterialPropertyCutoff>(true),
                Metallics = SystemAPI.GetComponentLookup<URPMaterialPropertyMetallic>(true),
                OcclusionStrengths = SystemAPI.GetComponentLookup<URPMaterialPropertyOcclusionStrength>(true),
                Smoothnesses = SystemAPI.GetComponentLookup<URPMaterialPropertySmoothness>(true),
                BaseColors = SystemAPI.GetComponentLookup<URPMaterialPropertyBaseColor>(true),
                EmissionColors = SystemAPI.GetComponentLookup<URPMaterialPropertyEmissionColor>(true),
                SpecColors = SystemAPI.GetComponentLookup<URPMaterialPropertySpecColor>(true),
            }.Schedule(state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                BumpScales = SystemAPI.GetComponentLookup<URPMaterialPropertyBumpScale>(),
                Cutoffs = SystemAPI.GetComponentLookup<URPMaterialPropertyCutoff>(),
                Metallics = SystemAPI.GetComponentLookup<URPMaterialPropertyMetallic>(),
                OcclusionStrengths = SystemAPI.GetComponentLookup<URPMaterialPropertyOcclusionStrength>(),
                Smoothnesses = SystemAPI.GetComponentLookup<URPMaterialPropertySmoothness>(),
                BaseColors = SystemAPI.GetComponentLookup<URPMaterialPropertyBaseColor>(),
                EmissionColors = SystemAPI.GetComponentLookup<URPMaterialPropertyEmissionColor>(),
                SpecColors = SystemAPI.GetComponentLookup<URPMaterialPropertySpecColor>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))]
        [WithAll(typeof(TimelineActivePrevious))]
        [WithDisabled(typeof(TimelineActive))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            public ComponentLookup<URPMaterialPropertyBumpScale> BumpScales;
            public ComponentLookup<URPMaterialPropertyCutoff> Cutoffs;
            public ComponentLookup<URPMaterialPropertyMetallic> Metallics;
            public ComponentLookup<URPMaterialPropertyOcclusionStrength> OcclusionStrengths;
            public ComponentLookup<URPMaterialPropertySmoothness> Smoothnesses;
            public ComponentLookup<URPMaterialPropertyBaseColor> BaseColors;
            public ComponentLookup<URPMaterialPropertyEmissionColor> EmissionColors;
            public ComponentLookup<URPMaterialPropertySpecColor> SpecColors;

            private void Execute(in URPMaterialPropertyInitial initial, in TrackBinding trackBinding)
            {
                var value = initial.Value;

                if (value.EnableBumpScale && this.BumpScales.TryGetRefRW(trackBinding.Value, out var bumpScale))
                {
                    bumpScale.ValueRW.Value = value.BumpScale;
                }

                if (value.EnableCutoff && this.Cutoffs.TryGetRefRW(trackBinding.Value, out var cutoff))
                {
                    cutoff.ValueRW.Value = value.Cutoff;
                }

                if (value.EnableMetallic && this.Metallics.TryGetRefRW(trackBinding.Value, out var metallic))
                {
                    metallic.ValueRW.Value = value.Metallic;
                }

                if (value.EnableOcclusionStrength && this.OcclusionStrengths.TryGetRefRW(trackBinding.Value, out var occlusionStrength))
                {
                    occlusionStrength.ValueRW.Value = value.OcclusionStrength;
                }

                if (value.EnableSmoothness && this.Smoothnesses.TryGetRefRW(trackBinding.Value, out var smoothness))
                {
                    smoothness.ValueRW.Value = value.Smoothness;
                }

                if (value.EnableBaseColor && this.BaseColors.TryGetRefRW(trackBinding.Value, out var baseColor))
                {
                    baseColor.ValueRW.Value = value.BaseColor;
                }

                if (value.EnableEmissionColor && this.EmissionColors.TryGetRefRW(trackBinding.Value, out var emissionColor))
                {
                    emissionColor.ValueRW.Value = value.EmissionColor;
                }

                if (value.EnableSpecColor && this.SpecColors.TryGetRefRW(trackBinding.Value, out var specColor))
                {
                    specColor.ValueRW.Value = value.SpecColor;
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        private partial struct TrackActivateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyBumpScale> BumpScales;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyCutoff> Cutoffs;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyMetallic> Metallics;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyOcclusionStrength> OcclusionStrengths;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertySmoothness> Smoothnesses;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyBaseColor> BaseColors;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyEmissionColor> EmissionColors;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertySpecColor> SpecColors;

            private void Execute(ref URPMaterialPropertyInitial initial, in TrackBinding trackBinding)
            {
                var flags = initial.Flags;
                var value = new URPMaterialPropertyBlend();
                if ((flags & URPMaterialPropertyFlags.BumpScale) != 0 && this.BumpScales.TryGetComponent(trackBinding.Value, out var bumpScale))
                {
                    value.BumpScale = bumpScale.Value;
                    value.EnableBumpScale = true;
                }

                if ((flags & URPMaterialPropertyFlags.Cutoff) != 0 && this.Cutoffs.TryGetComponent(trackBinding.Value, out var cutoff))
                {
                    value.Cutoff = cutoff.Value;
                    value.EnableCutoff = true;
                }

                if ((flags & URPMaterialPropertyFlags.Metallic) != 0 && this.Metallics.TryGetComponent(trackBinding.Value, out var metallic))
                {
                    value.Metallic = metallic.Value;
                    value.EnableMetallic = true;
                }

                if ((flags & URPMaterialPropertyFlags.OcclusionStrength) != 0 &&
                    this.OcclusionStrengths.TryGetComponent(trackBinding.Value, out var occlusionStrength))
                {
                    value.OcclusionStrength = occlusionStrength.Value;
                    value.EnableOcclusionStrength = true;
                }

                if ((flags & URPMaterialPropertyFlags.Smoothness) != 0 && this.Smoothnesses.TryGetComponent(trackBinding.Value, out var smoothness))
                {
                    value.Smoothness = smoothness.Value;
                    value.EnableSmoothness = true;
                }

                if ((flags & URPMaterialPropertyFlags.BaseColor) != 0 && this.BaseColors.TryGetComponent(trackBinding.Value, out var baseColor))
                {
                    value.BaseColor = baseColor.Value;
                    value.EnableBaseColor = true;
                }

                if ((flags & URPMaterialPropertyFlags.EmissionColor) != 0 && this.EmissionColors.TryGetComponent(trackBinding.Value, out var emissionColor))
                {
                    value.EmissionColor = emissionColor.Value;
                    value.EnableEmissionColor = true;
                }

                if ((flags & URPMaterialPropertyFlags.SpecColor) != 0 && this.SpecColors.TryGetComponent(trackBinding.Value, out var specColor))
                {
                    value.SpecColor = specColor.Value;
                    value.EnableSpecColor = true;
                }

                initial.Value = value;
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct ClipActivateJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public ComponentLookup<URPMaterialPropertyTrackComponents> TrackComponents;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyBumpScale> BumpScales;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyCutoff> Cutoffs;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyMetallic> Metallics;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyOcclusionStrength> OcclusionStrengths;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertySmoothness> Smoothnesses;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyBaseColor> BaseColors;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyEmissionColor> EmissionColors;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertySpecColor> SpecColors;

            private void Execute(ref URPMaterialPropertyAnimated animated, in URPMaterialPropertyClipData clipData, in TrackBinding trackBinding, in Clip clip)
            {
                animated.Value = CreateBlend(in clipData);

                if (!this.TrackComponents.TryGetRefRW(clip.Track, out var trackComponents))
                {
                    return;
                }

                ref var state = ref trackComponents.ValueRW;
                if (!state.AddComponents)
                {
                    return;
                }

                var target = trackBinding.Value;
                if (target == Entity.Null)
                {
                    return;
                }

                var flags = clipData.Flags;
                if ((flags & URPMaterialPropertyFlags.BumpScale) != 0)
                {
                    state.BumpScaleCount++;
                    if (state.BumpScaleCount == 1 && !this.BumpScales.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new URPMaterialPropertyBumpScale { Value = clipData.BumpScale });
                        state.AddedFlags |= URPMaterialPropertyFlags.BumpScale;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.Cutoff) != 0)
                {
                    state.CutoffCount++;
                    if (state.CutoffCount == 1 && !this.Cutoffs.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new URPMaterialPropertyCutoff { Value = clipData.Cutoff });
                        state.AddedFlags |= URPMaterialPropertyFlags.Cutoff;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.Metallic) != 0)
                {
                    state.MetallicCount++;
                    if (state.MetallicCount == 1 && !this.Metallics.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new URPMaterialPropertyMetallic { Value = clipData.Metallic });
                        state.AddedFlags |= URPMaterialPropertyFlags.Metallic;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.OcclusionStrength) != 0)
                {
                    state.OcclusionStrengthCount++;
                    if (state.OcclusionStrengthCount == 1 && !this.OcclusionStrengths.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new URPMaterialPropertyOcclusionStrength { Value = clipData.OcclusionStrength });
                        state.AddedFlags |= URPMaterialPropertyFlags.OcclusionStrength;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.Smoothness) != 0)
                {
                    state.SmoothnessCount++;
                    if (state.SmoothnessCount == 1 && !this.Smoothnesses.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new URPMaterialPropertySmoothness { Value = clipData.Smoothness });
                        state.AddedFlags |= URPMaterialPropertyFlags.Smoothness;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.BaseColor) != 0)
                {
                    state.BaseColorCount++;
                    if (state.BaseColorCount == 1 && !this.BaseColors.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new URPMaterialPropertyBaseColor { Value = clipData.BaseColor });
                        state.AddedFlags |= URPMaterialPropertyFlags.BaseColor;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.EmissionColor) != 0)
                {
                    state.EmissionColorCount++;
                    if (state.EmissionColorCount == 1 && !this.EmissionColors.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new URPMaterialPropertyEmissionColor { Value = clipData.EmissionColor });
                        state.AddedFlags |= URPMaterialPropertyFlags.EmissionColor;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.SpecColor) != 0)
                {
                    state.SpecColorCount++;
                    if (state.SpecColorCount == 1 && !this.SpecColors.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new URPMaterialPropertySpecColor { Value = clipData.SpecColor });
                        state.AddedFlags |= URPMaterialPropertyFlags.SpecColor;
                    }
                }
            }

            private static URPMaterialPropertyBlend CreateBlend(in URPMaterialPropertyClipData clipData)
            {
                var blend = new URPMaterialPropertyBlend();
                var flags = clipData.Flags;

                if ((flags & URPMaterialPropertyFlags.BumpScale) != 0)
                {
                    blend.BumpScale = clipData.BumpScale;
                    blend.EnableBumpScale = true;
                }

                if ((flags & URPMaterialPropertyFlags.Cutoff) != 0)
                {
                    blend.Cutoff = clipData.Cutoff;
                    blend.EnableCutoff = true;
                }

                if ((flags & URPMaterialPropertyFlags.Metallic) != 0)
                {
                    blend.Metallic = clipData.Metallic;
                    blend.EnableMetallic = true;
                }

                if ((flags & URPMaterialPropertyFlags.OcclusionStrength) != 0)
                {
                    blend.OcclusionStrength = clipData.OcclusionStrength;
                    blend.EnableOcclusionStrength = true;
                }

                if ((flags & URPMaterialPropertyFlags.Smoothness) != 0)
                {
                    blend.Smoothness = clipData.Smoothness;
                    blend.EnableSmoothness = true;
                }

                if ((flags & URPMaterialPropertyFlags.BaseColor) != 0)
                {
                    blend.BaseColor = clipData.BaseColor;
                    blend.EnableBaseColor = true;
                }

                if ((flags & URPMaterialPropertyFlags.EmissionColor) != 0)
                {
                    blend.EmissionColor = clipData.EmissionColor;
                    blend.EnableEmissionColor = true;
                }

                if ((flags & URPMaterialPropertyFlags.SpecColor) != 0)
                {
                    blend.SpecColor = clipData.SpecColor;
                    blend.EnableSpecColor = true;
                }

                return blend;
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActivePrevious))]
        [WithDisabled(typeof(ClipActive))]
        private partial struct ClipDeactivatedJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public ComponentLookup<URPMaterialPropertyTrackComponents> TrackComponents;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyBumpScale> BumpScales;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyCutoff> Cutoffs;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyMetallic> Metallics;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyOcclusionStrength> OcclusionStrengths;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertySmoothness> Smoothnesses;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyBaseColor> BaseColors;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertyEmissionColor> EmissionColors;

            [ReadOnly]
            public ComponentLookup<URPMaterialPropertySpecColor> SpecColors;

            private void Execute(in URPMaterialPropertyClipData clipData, in TrackBinding trackBinding, in Clip clip)
            {
                if (!this.TrackComponents.TryGetRefRW(clip.Track, out var trackComponents))
                {
                    return;
                }

                ref var state = ref trackComponents.ValueRW;
                if (!state.AddComponents)
                {
                    return;
                }

                var target = trackBinding.Value;
                if (target == Entity.Null)
                {
                    return;
                }

                var flags = clipData.Flags;
                if ((flags & URPMaterialPropertyFlags.BumpScale) != 0)
                {
                    if (state.BumpScaleCount > 0)
                    {
                        state.BumpScaleCount--;
                    }

                    if (state.BumpScaleCount == 0 && (state.AddedFlags & URPMaterialPropertyFlags.BumpScale) != 0)
                    {
                        if (this.BumpScales.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<URPMaterialPropertyBumpScale>(target);
                        }

                        state.AddedFlags &= ~URPMaterialPropertyFlags.BumpScale;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.Cutoff) != 0)
                {
                    if (state.CutoffCount > 0)
                    {
                        state.CutoffCount--;
                    }

                    if (state.CutoffCount == 0 && (state.AddedFlags & URPMaterialPropertyFlags.Cutoff) != 0)
                    {
                        if (this.Cutoffs.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<URPMaterialPropertyCutoff>(target);
                        }

                        state.AddedFlags &= ~URPMaterialPropertyFlags.Cutoff;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.Metallic) != 0)
                {
                    if (state.MetallicCount > 0)
                    {
                        state.MetallicCount--;
                    }

                    if (state.MetallicCount == 0 && (state.AddedFlags & URPMaterialPropertyFlags.Metallic) != 0)
                    {
                        if (this.Metallics.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<URPMaterialPropertyMetallic>(target);
                        }

                        state.AddedFlags &= ~URPMaterialPropertyFlags.Metallic;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.OcclusionStrength) != 0)
                {
                    if (state.OcclusionStrengthCount > 0)
                    {
                        state.OcclusionStrengthCount--;
                    }

                    if (state.OcclusionStrengthCount == 0 && (state.AddedFlags & URPMaterialPropertyFlags.OcclusionStrength) != 0)
                    {
                        if (this.OcclusionStrengths.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<URPMaterialPropertyOcclusionStrength>(target);
                        }

                        state.AddedFlags &= ~URPMaterialPropertyFlags.OcclusionStrength;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.Smoothness) != 0)
                {
                    if (state.SmoothnessCount > 0)
                    {
                        state.SmoothnessCount--;
                    }

                    if (state.SmoothnessCount == 0 && (state.AddedFlags & URPMaterialPropertyFlags.Smoothness) != 0)
                    {
                        if (this.Smoothnesses.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<URPMaterialPropertySmoothness>(target);
                        }

                        state.AddedFlags &= ~URPMaterialPropertyFlags.Smoothness;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.BaseColor) != 0)
                {
                    if (state.BaseColorCount > 0)
                    {
                        state.BaseColorCount--;
                    }

                    if (state.BaseColorCount == 0 && (state.AddedFlags & URPMaterialPropertyFlags.BaseColor) != 0)
                    {
                        if (this.BaseColors.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<URPMaterialPropertyBaseColor>(target);
                        }

                        state.AddedFlags &= ~URPMaterialPropertyFlags.BaseColor;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.EmissionColor) != 0)
                {
                    if (state.EmissionColorCount > 0)
                    {
                        state.EmissionColorCount--;
                    }

                    if (state.EmissionColorCount == 0 && (state.AddedFlags & URPMaterialPropertyFlags.EmissionColor) != 0)
                    {
                        if (this.EmissionColors.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<URPMaterialPropertyEmissionColor>(target);
                        }

                        state.AddedFlags &= ~URPMaterialPropertyFlags.EmissionColor;
                    }
                }

                if ((flags & URPMaterialPropertyFlags.SpecColor) != 0)
                {
                    if (state.SpecColorCount > 0)
                    {
                        state.SpecColorCount--;
                    }

                    if (state.SpecColorCount == 0 && (state.AddedFlags & URPMaterialPropertyFlags.SpecColor) != 0)
                    {
                        if (this.SpecColors.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<URPMaterialPropertySpecColor>(target);
                        }

                        state.AddedFlags &= ~URPMaterialPropertyFlags.SpecColor;
                    }
                }
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<URPMaterialPropertyBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<URPMaterialPropertyBumpScale> BumpScales;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<URPMaterialPropertyCutoff> Cutoffs;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<URPMaterialPropertyMetallic> Metallics;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<URPMaterialPropertyOcclusionStrength> OcclusionStrengths;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<URPMaterialPropertySmoothness> Smoothnesses;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<URPMaterialPropertyBaseColor> BaseColors;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<URPMaterialPropertyEmissionColor> EmissionColors;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<URPMaterialPropertySpecColor> SpecColors;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                var current = new URPMaterialPropertyBlend();
                var enableBumpScale = mixData.Value1.EnableBumpScale || mixData.Value2.EnableBumpScale || mixData.Value3.EnableBumpScale ||
                    mixData.Value4.EnableBumpScale;

                var enableCutoff = mixData.Value1.EnableCutoff || mixData.Value2.EnableCutoff || mixData.Value3.EnableCutoff || mixData.Value4.EnableCutoff;

                var enableMetallic = mixData.Value1.EnableMetallic || mixData.Value2.EnableMetallic || mixData.Value3.EnableMetallic ||
                    mixData.Value4.EnableMetallic;

                var enableOcclusionStrength = mixData.Value1.EnableOcclusionStrength || mixData.Value2.EnableOcclusionStrength ||
                    mixData.Value3.EnableOcclusionStrength || mixData.Value4.EnableOcclusionStrength;

                var enableSmoothness = mixData.Value1.EnableSmoothness || mixData.Value2.EnableSmoothness || mixData.Value3.EnableSmoothness ||
                    mixData.Value4.EnableSmoothness;

                var enableBaseColor = mixData.Value1.EnableBaseColor || mixData.Value2.EnableBaseColor || mixData.Value3.EnableBaseColor ||
                    mixData.Value4.EnableBaseColor;

                var enableEmissionColor = mixData.Value1.EnableEmissionColor || mixData.Value2.EnableEmissionColor || mixData.Value3.EnableEmissionColor ||
                    mixData.Value4.EnableEmissionColor;

                var enableSpecColor = mixData.Value1.EnableSpecColor || mixData.Value2.EnableSpecColor || mixData.Value3.EnableSpecColor ||
                    mixData.Value4.EnableSpecColor;

                RefRW<URPMaterialPropertyBumpScale> bumpScaleRw = default;
                RefRW<URPMaterialPropertyCutoff> cutoffRw = default;
                RefRW<URPMaterialPropertyMetallic> metallicRw = default;
                RefRW<URPMaterialPropertyOcclusionStrength> occlusionStrengthRw = default;
                RefRW<URPMaterialPropertySmoothness> smoothnessRw = default;
                RefRW<URPMaterialPropertyBaseColor> baseColorRw = default;
                RefRW<URPMaterialPropertyEmissionColor> emissionColorRw = default;
                RefRW<URPMaterialPropertySpecColor> specColorRw = default;

                if (enableBumpScale && this.BumpScales.TryGetRefRW(entity, out bumpScaleRw))
                {
                    current.BumpScale = bumpScaleRw.ValueRO.Value;
                    current.EnableBumpScale = true;
                }

                if (enableCutoff && this.Cutoffs.TryGetRefRW(entity, out cutoffRw))
                {
                    current.Cutoff = cutoffRw.ValueRO.Value;
                    current.EnableCutoff = true;
                }

                if (enableMetallic && this.Metallics.TryGetRefRW(entity, out metallicRw))
                {
                    current.Metallic = metallicRw.ValueRO.Value;
                    current.EnableMetallic = true;
                }

                if (enableOcclusionStrength && this.OcclusionStrengths.TryGetRefRW(entity, out occlusionStrengthRw))
                {
                    current.OcclusionStrength = occlusionStrengthRw.ValueRO.Value;
                    current.EnableOcclusionStrength = true;
                }

                if (enableSmoothness && this.Smoothnesses.TryGetRefRW(entity, out smoothnessRw))
                {
                    current.Smoothness = smoothnessRw.ValueRO.Value;
                    current.EnableSmoothness = true;
                }

                if (enableBaseColor && this.BaseColors.TryGetRefRW(entity, out baseColorRw))
                {
                    current.BaseColor = baseColorRw.ValueRO.Value;
                    current.EnableBaseColor = true;
                }

                if (enableEmissionColor && this.EmissionColors.TryGetRefRW(entity, out emissionColorRw))
                {
                    current.EmissionColor = emissionColorRw.ValueRO.Value;
                    current.EnableEmissionColor = true;
                }

                if (enableSpecColor && this.SpecColors.TryGetRefRW(entity, out specColorRw))
                {
                    current.SpecColor = specColorRw.ValueRO.Value;
                    current.EnableSpecColor = true;
                }

                var blended = JobHelpers.Blend(ref mixData, current, default(URPMaterialPropertyMixer));

                if (enableBumpScale && blended.EnableBumpScale && bumpScaleRw.IsValid)
                {
                    bumpScaleRw.ValueRW.Value = blended.BumpScale;
                }

                if (enableCutoff && blended.EnableCutoff && cutoffRw.IsValid)
                {
                    cutoffRw.ValueRW.Value = blended.Cutoff;
                }

                if (enableMetallic && blended.EnableMetallic && metallicRw.IsValid)
                {
                    metallicRw.ValueRW.Value = blended.Metallic;
                }

                if (enableOcclusionStrength && blended.EnableOcclusionStrength && occlusionStrengthRw.IsValid)
                {
                    occlusionStrengthRw.ValueRW.Value = blended.OcclusionStrength;
                }

                if (enableSmoothness && blended.EnableSmoothness && smoothnessRw.IsValid)
                {
                    smoothnessRw.ValueRW.Value = blended.Smoothness;
                }

                if (enableBaseColor && blended.EnableBaseColor && baseColorRw.IsValid)
                {
                    baseColorRw.ValueRW.Value = blended.BaseColor;
                }

                if (enableEmissionColor && blended.EnableEmissionColor && emissionColorRw.IsValid)
                {
                    emissionColorRw.ValueRW.Value = blended.EmissionColor;
                }

                if (enableSpecColor && blended.EnableSpecColor && specColorRw.IsValid)
                {
                    specColorRw.ValueRW.Value = blended.SpecColor;
                }
            }
        }

        private struct URPMaterialPropertyMixer : IMixer<URPMaterialPropertyBlend>
        {
            public URPMaterialPropertyBlend Lerp(in URPMaterialPropertyBlend a, in URPMaterialPropertyBlend b, in float s)
            {
                var bumpScale = MaterialPropertyBlendUtility.BlendValue(a.BumpScale, a.EnableBumpScale, b.BumpScale, b.EnableBumpScale, s,
                    out var enableBumpScale);
                var cutoff = MaterialPropertyBlendUtility.BlendValue(a.Cutoff, a.EnableCutoff, b.Cutoff, b.EnableCutoff, s, out var enableCutoff);
                var metallic = MaterialPropertyBlendUtility.BlendValue(a.Metallic, a.EnableMetallic, b.Metallic, b.EnableMetallic, s, out var enableMetallic);
                var occlusionStrength = MaterialPropertyBlendUtility.BlendValue(a.OcclusionStrength, a.EnableOcclusionStrength, b.OcclusionStrength,
                    b.EnableOcclusionStrength, s, out var enableOcclusionStrength);
                var smoothness = MaterialPropertyBlendUtility.BlendValue(a.Smoothness, a.EnableSmoothness, b.Smoothness, b.EnableSmoothness, s,
                    out var enableSmoothness);
                var baseColor = MaterialPropertyBlendUtility.BlendValue(a.BaseColor, a.EnableBaseColor, b.BaseColor, b.EnableBaseColor, s,
                    out var enableBaseColor);
                var emissionColor = MaterialPropertyBlendUtility.BlendValue(a.EmissionColor, a.EnableEmissionColor, b.EmissionColor, b.EnableEmissionColor, s,
                    out var enableEmissionColor);
                var specColor = MaterialPropertyBlendUtility.BlendValue(a.SpecColor, a.EnableSpecColor, b.SpecColor, b.EnableSpecColor, s,
                    out var enableSpecColor);

                return new URPMaterialPropertyBlend
                {
                    BumpScale = bumpScale,
                    Cutoff = cutoff,
                    Metallic = metallic,
                    OcclusionStrength = occlusionStrength,
                    Smoothness = smoothness,
                    BaseColor = baseColor,
                    EmissionColor = emissionColor,
                    SpecColor = specColor,
                    EnableBumpScale = enableBumpScale,
                    EnableCutoff = enableCutoff,
                    EnableMetallic = enableMetallic,
                    EnableOcclusionStrength = enableOcclusionStrength,
                    EnableSmoothness = enableSmoothness,
                    EnableBaseColor = enableBaseColor,
                    EnableEmissionColor = enableEmissionColor,
                    EnableSpecColor = enableSpecColor,
                };
            }

            public URPMaterialPropertyBlend Add(in URPMaterialPropertyBlend a, in URPMaterialPropertyBlend b)
            {
                var bumpScale = MaterialPropertyBlendUtility.AddValue(a.BumpScale, a.EnableBumpScale, b.BumpScale, b.EnableBumpScale, out var enableBumpScale);
                var cutoff = MaterialPropertyBlendUtility.AddValue(a.Cutoff, a.EnableCutoff, b.Cutoff, b.EnableCutoff, out var enableCutoff);
                var metallic = MaterialPropertyBlendUtility.AddValue(a.Metallic, a.EnableMetallic, b.Metallic, b.EnableMetallic, out var enableMetallic);
                var occlusionStrength = MaterialPropertyBlendUtility.AddValue(a.OcclusionStrength, a.EnableOcclusionStrength, b.OcclusionStrength,
                    b.EnableOcclusionStrength, out var enableOcclusionStrength);
                var smoothness =
                    MaterialPropertyBlendUtility.AddValue(a.Smoothness, a.EnableSmoothness, b.Smoothness, b.EnableSmoothness, out var enableSmoothness);
                var baseColor = MaterialPropertyBlendUtility.AddValue(a.BaseColor, a.EnableBaseColor, b.BaseColor, b.EnableBaseColor, out var enableBaseColor);
                var emissionColor = MaterialPropertyBlendUtility.AddValue(a.EmissionColor, a.EnableEmissionColor, b.EmissionColor, b.EnableEmissionColor,
                    out var enableEmissionColor);
                var specColor = MaterialPropertyBlendUtility.AddValue(a.SpecColor, a.EnableSpecColor, b.SpecColor, b.EnableSpecColor, out var enableSpecColor);

                return new URPMaterialPropertyBlend
                {
                    BumpScale = bumpScale,
                    Cutoff = cutoff,
                    Metallic = metallic,
                    OcclusionStrength = occlusionStrength,
                    Smoothness = smoothness,
                    BaseColor = baseColor,
                    EmissionColor = emissionColor,
                    SpecColor = specColor,
                    EnableBumpScale = enableBumpScale,
                    EnableCutoff = enableCutoff,
                    EnableMetallic = enableMetallic,
                    EnableOcclusionStrength = enableOcclusionStrength,
                    EnableSmoothness = enableSmoothness,
                    EnableBaseColor = enableBaseColor,
                    EnableEmissionColor = enableEmissionColor,
                    EnableSpecColor = enableSpecColor,
                };
            }
        }
    }
}
#endif
