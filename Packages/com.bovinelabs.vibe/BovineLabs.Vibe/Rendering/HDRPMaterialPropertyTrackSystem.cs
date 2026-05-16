// <copyright file="HDRPMaterialPropertyTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_HDRP
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
    /// Blends HDRP material property clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct HDRPMaterialPropertyTrackSystem : ISystem
    {
        private TrackBlendImpl<HDRPMaterialPropertyBlend, HDRPMaterialPropertyAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HDRPMaterialPropertyClipData>();
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
                AlphaCutoffs = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAlphaCutoff>(),
                AORemapMaxes = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAORemapMax>(),
                AORemapMins = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAORemapMin>(),
                DetailAlbedoScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailAlbedoScale>(),
                DetailNormalScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailNormalScale>(),
                DetailSmoothnessScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailSmoothnessScale>(),
                DiffusionProfileHashes = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDiffusionProfileHash>(),
                Metallics = SystemAPI.GetComponentLookup<HDRPMaterialPropertyMetallic>(),
                Smoothnesses = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothness>(),
                SmoothnessRemapMaxes = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothnessRemapMax>(),
                SmoothnessRemapMins = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothnessRemapMin>(),
                Thicknesses = SystemAPI.GetComponentLookup<HDRPMaterialPropertyThickness>(),
                EmissiveColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyEmissiveColor>(),
                BaseColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyBaseColor>(),
                SpecularColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertySpecularColor>(),
                ThicknessRemaps = SystemAPI.GetComponentLookup<HDRPMaterialPropertyThicknessRemap>(),
                UnlitColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyUnlitColor>(),
            }.Schedule(state.Dependency);

            state.Dependency = new TrackActivateJob
            {
                AlphaCutoffs = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAlphaCutoff>(true),
                AORemapMaxes = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAORemapMax>(true),
                AORemapMins = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAORemapMin>(true),
                DetailAlbedoScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailAlbedoScale>(true),
                DetailNormalScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailNormalScale>(true),
                DetailSmoothnessScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailSmoothnessScale>(true),
                DiffusionProfileHashes = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDiffusionProfileHash>(true),
                Metallics = SystemAPI.GetComponentLookup<HDRPMaterialPropertyMetallic>(true),
                Smoothnesses = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothness>(true),
                SmoothnessRemapMaxes = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothnessRemapMax>(true),
                SmoothnessRemapMins = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothnessRemapMin>(true),
                Thicknesses = SystemAPI.GetComponentLookup<HDRPMaterialPropertyThickness>(true),
                EmissiveColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyEmissiveColor>(true),
                BaseColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyBaseColor>(true),
                SpecularColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertySpecularColor>(true),
                ThicknessRemaps = SystemAPI.GetComponentLookup<HDRPMaterialPropertyThicknessRemap>(true),
                UnlitColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyUnlitColor>(true),
            }.ScheduleParallel(state.Dependency);

#if BL_CORE_EXTENSIONS
            var commandBuffer = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
#else
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
#endif

            state.Dependency = new ClipComponentActivateJob
            {
                CommandBuffer = commandBuffer,
                TrackComponents = SystemAPI.GetComponentLookup<HDRPMaterialPropertyTrackComponents>(),
                AlphaCutoffs = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAlphaCutoff>(true),
                AORemapMaxes = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAORemapMax>(true),
                AORemapMins = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAORemapMin>(true),
                DetailAlbedoScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailAlbedoScale>(true),
                DetailNormalScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailNormalScale>(true),
                DetailSmoothnessScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailSmoothnessScale>(true),
                DiffusionProfileHashes = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDiffusionProfileHash>(true),
                Metallics = SystemAPI.GetComponentLookup<HDRPMaterialPropertyMetallic>(true),
                Smoothnesses = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothness>(true),
                SmoothnessRemapMaxes = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothnessRemapMax>(true),
                SmoothnessRemapMins = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothnessRemapMin>(true),
                Thicknesses = SystemAPI.GetComponentLookup<HDRPMaterialPropertyThickness>(true),
                EmissiveColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyEmissiveColor>(true),
                BaseColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyBaseColor>(true),
                SpecularColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertySpecularColor>(true),
                ThicknessRemaps = SystemAPI.GetComponentLookup<HDRPMaterialPropertyThicknessRemap>(true),
                UnlitColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyUnlitColor>(true),
            }.Schedule(state.Dependency);

            state.Dependency = new ClipComponentDeactivateJob
            {
                CommandBuffer = commandBuffer,
                TrackComponents = SystemAPI.GetComponentLookup<HDRPMaterialPropertyTrackComponents>(),
                AlphaCutoffs = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAlphaCutoff>(true),
                AORemapMaxes = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAORemapMax>(true),
                AORemapMins = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAORemapMin>(true),
                DetailAlbedoScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailAlbedoScale>(true),
                DetailNormalScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailNormalScale>(true),
                DetailSmoothnessScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailSmoothnessScale>(true),
                DiffusionProfileHashes = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDiffusionProfileHash>(true),
                Metallics = SystemAPI.GetComponentLookup<HDRPMaterialPropertyMetallic>(true),
                Smoothnesses = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothness>(true),
                SmoothnessRemapMaxes = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothnessRemapMax>(true),
                SmoothnessRemapMins = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothnessRemapMin>(true),
                Thicknesses = SystemAPI.GetComponentLookup<HDRPMaterialPropertyThickness>(true),
                EmissiveColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyEmissiveColor>(true),
                BaseColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyBaseColor>(true),
                SpecularColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertySpecularColor>(true),
                ThicknessRemaps = SystemAPI.GetComponentLookup<HDRPMaterialPropertyThicknessRemap>(true),
                UnlitColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyUnlitColor>(true),
            }.Schedule(state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                AlphaCutoffs = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAlphaCutoff>(),
                AORemapMaxes = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAORemapMax>(),
                AORemapMins = SystemAPI.GetComponentLookup<HDRPMaterialPropertyAORemapMin>(),
                DetailAlbedoScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailAlbedoScale>(),
                DetailNormalScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailNormalScale>(),
                DetailSmoothnessScales = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDetailSmoothnessScale>(),
                DiffusionProfileHashes = SystemAPI.GetComponentLookup<HDRPMaterialPropertyDiffusionProfileHash>(),
                Metallics = SystemAPI.GetComponentLookup<HDRPMaterialPropertyMetallic>(),
                Smoothnesses = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothness>(),
                SmoothnessRemapMaxes = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothnessRemapMax>(),
                SmoothnessRemapMins = SystemAPI.GetComponentLookup<HDRPMaterialPropertySmoothnessRemapMin>(),
                Thicknesses = SystemAPI.GetComponentLookup<HDRPMaterialPropertyThickness>(),
                EmissiveColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyEmissiveColor>(),
                BaseColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyBaseColor>(),
                SpecularColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertySpecularColor>(),
                ThicknessRemaps = SystemAPI.GetComponentLookup<HDRPMaterialPropertyThicknessRemap>(),
                UnlitColors = SystemAPI.GetComponentLookup<HDRPMaterialPropertyUnlitColor>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))]
        [WithAll(typeof(TimelineActivePrevious))]
        [WithDisabled(typeof(TimelineActive))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            public ComponentLookup<HDRPMaterialPropertyAlphaCutoff> AlphaCutoffs;
            public ComponentLookup<HDRPMaterialPropertyAORemapMax> AORemapMaxes;
            public ComponentLookup<HDRPMaterialPropertyAORemapMin> AORemapMins;
            public ComponentLookup<HDRPMaterialPropertyDetailAlbedoScale> DetailAlbedoScales;
            public ComponentLookup<HDRPMaterialPropertyDetailNormalScale> DetailNormalScales;
            public ComponentLookup<HDRPMaterialPropertyDetailSmoothnessScale> DetailSmoothnessScales;
            public ComponentLookup<HDRPMaterialPropertyDiffusionProfileHash> DiffusionProfileHashes;
            public ComponentLookup<HDRPMaterialPropertyMetallic> Metallics;
            public ComponentLookup<HDRPMaterialPropertySmoothness> Smoothnesses;
            public ComponentLookup<HDRPMaterialPropertySmoothnessRemapMax> SmoothnessRemapMaxes;
            public ComponentLookup<HDRPMaterialPropertySmoothnessRemapMin> SmoothnessRemapMins;
            public ComponentLookup<HDRPMaterialPropertyThickness> Thicknesses;
            public ComponentLookup<HDRPMaterialPropertyEmissiveColor> EmissiveColors;
            public ComponentLookup<HDRPMaterialPropertyBaseColor> BaseColors;
            public ComponentLookup<HDRPMaterialPropertySpecularColor> SpecularColors;
            public ComponentLookup<HDRPMaterialPropertyThicknessRemap> ThicknessRemaps;
            public ComponentLookup<HDRPMaterialPropertyUnlitColor> UnlitColors;

            private void Execute(in HDRPMaterialPropertyInitial initial, in TrackBinding trackBinding)
            {
                var value = initial.Value;

                if (value.EnableAlphaCutoff && this.AlphaCutoffs.TryGetRefRW(trackBinding.Value, out var alphaCutoff))
                {
                    alphaCutoff.ValueRW.Value = value.AlphaCutoff;
                }

                if (value.EnableAORemapMax && this.AORemapMaxes.TryGetRefRW(trackBinding.Value, out var aoRemapMax))
                {
                    aoRemapMax.ValueRW.Value = value.AORemapMax;
                }

                if (value.EnableAORemapMin && this.AORemapMins.TryGetRefRW(trackBinding.Value, out var aoRemapMin))
                {
                    aoRemapMin.ValueRW.Value = value.AORemapMin;
                }

                if (value.EnableDetailAlbedoScale && this.DetailAlbedoScales.TryGetRefRW(trackBinding.Value, out var detailAlbedoScale))
                {
                    detailAlbedoScale.ValueRW.Value = value.DetailAlbedoScale;
                }

                if (value.EnableDetailNormalScale && this.DetailNormalScales.TryGetRefRW(trackBinding.Value, out var detailNormalScale))
                {
                    detailNormalScale.ValueRW.Value = value.DetailNormalScale;
                }

                if (value.EnableDetailSmoothnessScale && this.DetailSmoothnessScales.TryGetRefRW(trackBinding.Value, out var detailSmoothnessScale))
                {
                    detailSmoothnessScale.ValueRW.Value = value.DetailSmoothnessScale;
                }

                if (value.EnableDiffusionProfileHash && this.DiffusionProfileHashes.TryGetRefRW(trackBinding.Value, out var diffusionProfileHash))
                {
                    diffusionProfileHash.ValueRW.Value = value.DiffusionProfileHash;
                }

                if (value.EnableMetallic && this.Metallics.TryGetRefRW(trackBinding.Value, out var metallic))
                {
                    metallic.ValueRW.Value = value.Metallic;
                }

                if (value.EnableSmoothness && this.Smoothnesses.TryGetRefRW(trackBinding.Value, out var smoothness))
                {
                    smoothness.ValueRW.Value = value.Smoothness;
                }

                if (value.EnableSmoothnessRemapMax && this.SmoothnessRemapMaxes.TryGetRefRW(trackBinding.Value, out var smoothnessRemapMax))
                {
                    smoothnessRemapMax.ValueRW.Value = value.SmoothnessRemapMax;
                }

                if (value.EnableSmoothnessRemapMin && this.SmoothnessRemapMins.TryGetRefRW(trackBinding.Value, out var smoothnessRemapMin))
                {
                    smoothnessRemapMin.ValueRW.Value = value.SmoothnessRemapMin;
                }

                if (value.EnableThickness && this.Thicknesses.TryGetRefRW(trackBinding.Value, out var thickness))
                {
                    thickness.ValueRW.Value = value.Thickness;
                }

                if (value.EnableEmissiveColor && this.EmissiveColors.TryGetRefRW(trackBinding.Value, out var emissiveColor))
                {
                    emissiveColor.ValueRW.Value = value.EmissiveColor;
                }

                if (value.EnableBaseColor && this.BaseColors.TryGetRefRW(trackBinding.Value, out var baseColor))
                {
                    baseColor.ValueRW.Value = value.BaseColor;
                }

                if (value.EnableSpecularColor && this.SpecularColors.TryGetRefRW(trackBinding.Value, out var specularColor))
                {
                    specularColor.ValueRW.Value = value.SpecularColor;
                }

                if (value.EnableThicknessRemap && this.ThicknessRemaps.TryGetRefRW(trackBinding.Value, out var thicknessRemap))
                {
                    thicknessRemap.ValueRW.Value = value.ThicknessRemap;
                }

                if (value.EnableUnlitColor && this.UnlitColors.TryGetRefRW(trackBinding.Value, out var unlitColor))
                {
                    unlitColor.ValueRW.Value = value.UnlitColor;
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        private partial struct TrackActivateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyAlphaCutoff> AlphaCutoffs;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyAORemapMax> AORemapMaxes;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyAORemapMin> AORemapMins;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyDetailAlbedoScale> DetailAlbedoScales;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyDetailNormalScale> DetailNormalScales;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyDetailSmoothnessScale> DetailSmoothnessScales;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyDiffusionProfileHash> DiffusionProfileHashes;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyMetallic> Metallics;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertySmoothness> Smoothnesses;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertySmoothnessRemapMax> SmoothnessRemapMaxes;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertySmoothnessRemapMin> SmoothnessRemapMins;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyThickness> Thicknesses;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyEmissiveColor> EmissiveColors;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyBaseColor> BaseColors;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertySpecularColor> SpecularColors;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyThicknessRemap> ThicknessRemaps;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyUnlitColor> UnlitColors;

            private void Execute(ref HDRPMaterialPropertyInitial initial, in TrackBinding trackBinding)
            {
                var flags = initial.Flags;
                var value = new HDRPMaterialPropertyBlend();
                if ((flags & HDRPMaterialPropertyFlags.AlphaCutoff) != 0 &&
                    this.AlphaCutoffs.TryGetComponent(trackBinding.Value, out var alphaCutoff))
                {
                    value.AlphaCutoff = alphaCutoff.Value;
                    value.EnableAlphaCutoff = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.AORemapMax) != 0 &&
                    this.AORemapMaxes.TryGetComponent(trackBinding.Value, out var aoRemapMax))
                {
                    value.AORemapMax = aoRemapMax.Value;
                    value.EnableAORemapMax = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.AORemapMin) != 0 &&
                    this.AORemapMins.TryGetComponent(trackBinding.Value, out var aoRemapMin))
                {
                    value.AORemapMin = aoRemapMin.Value;
                    value.EnableAORemapMin = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.DetailAlbedoScale) != 0 &&
                    this.DetailAlbedoScales.TryGetComponent(trackBinding.Value, out var detailAlbedoScale))
                {
                    value.DetailAlbedoScale = detailAlbedoScale.Value;
                    value.EnableDetailAlbedoScale = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.DetailNormalScale) != 0 &&
                    this.DetailNormalScales.TryGetComponent(trackBinding.Value, out var detailNormalScale))
                {
                    value.DetailNormalScale = detailNormalScale.Value;
                    value.EnableDetailNormalScale = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.DetailSmoothnessScale) != 0 &&
                    this.DetailSmoothnessScales.TryGetComponent(trackBinding.Value, out var detailSmoothnessScale))
                {
                    value.DetailSmoothnessScale = detailSmoothnessScale.Value;
                    value.EnableDetailSmoothnessScale = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.DiffusionProfileHash) != 0 &&
                    this.DiffusionProfileHashes.TryGetComponent(trackBinding.Value, out var diffusionProfileHash))
                {
                    value.DiffusionProfileHash = diffusionProfileHash.Value;
                    value.EnableDiffusionProfileHash = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.Metallic) != 0 &&
                    this.Metallics.TryGetComponent(trackBinding.Value, out var metallic))
                {
                    value.Metallic = metallic.Value;
                    value.EnableMetallic = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.Smoothness) != 0 &&
                    this.Smoothnesses.TryGetComponent(trackBinding.Value, out var smoothness))
                {
                    value.Smoothness = smoothness.Value;
                    value.EnableSmoothness = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.SmoothnessRemapMax) != 0 &&
                    this.SmoothnessRemapMaxes.TryGetComponent(trackBinding.Value, out var smoothnessRemapMax))
                {
                    value.SmoothnessRemapMax = smoothnessRemapMax.Value;
                    value.EnableSmoothnessRemapMax = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.SmoothnessRemapMin) != 0 &&
                    this.SmoothnessRemapMins.TryGetComponent(trackBinding.Value, out var smoothnessRemapMin))
                {
                    value.SmoothnessRemapMin = smoothnessRemapMin.Value;
                    value.EnableSmoothnessRemapMin = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.Thickness) != 0 &&
                    this.Thicknesses.TryGetComponent(trackBinding.Value, out var thickness))
                {
                    value.Thickness = thickness.Value;
                    value.EnableThickness = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.EmissiveColor) != 0 &&
                    this.EmissiveColors.TryGetComponent(trackBinding.Value, out var emissiveColor))
                {
                    value.EmissiveColor = emissiveColor.Value;
                    value.EnableEmissiveColor = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.BaseColor) != 0 &&
                    this.BaseColors.TryGetComponent(trackBinding.Value, out var baseColor))
                {
                    value.BaseColor = baseColor.Value;
                    value.EnableBaseColor = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.SpecularColor) != 0 &&
                    this.SpecularColors.TryGetComponent(trackBinding.Value, out var specularColor))
                {
                    value.SpecularColor = specularColor.Value;
                    value.EnableSpecularColor = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.ThicknessRemap) != 0 &&
                    this.ThicknessRemaps.TryGetComponent(trackBinding.Value, out var thicknessRemap))
                {
                    value.ThicknessRemap = thicknessRemap.Value;
                    value.EnableThicknessRemap = true;
                }

                if ((flags & HDRPMaterialPropertyFlags.UnlitColor) != 0 &&
                    this.UnlitColors.TryGetComponent(trackBinding.Value, out var unlitColor))
                {
                    value.UnlitColor = unlitColor.Value;
                    value.EnableUnlitColor = true;
                }

                initial.Value = value;
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct ClipComponentActivateJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public ComponentLookup<HDRPMaterialPropertyTrackComponents> TrackComponents;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyAlphaCutoff> AlphaCutoffs;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyAORemapMax> AORemapMaxes;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyAORemapMin> AORemapMins;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyDetailAlbedoScale> DetailAlbedoScales;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyDetailNormalScale> DetailNormalScales;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyDetailSmoothnessScale> DetailSmoothnessScales;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyDiffusionProfileHash> DiffusionProfileHashes;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyMetallic> Metallics;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertySmoothness> Smoothnesses;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertySmoothnessRemapMax> SmoothnessRemapMaxes;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertySmoothnessRemapMin> SmoothnessRemapMins;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyThickness> Thicknesses;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyEmissiveColor> EmissiveColors;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyBaseColor> BaseColors;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertySpecularColor> SpecularColors;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyThicknessRemap> ThicknessRemaps;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyUnlitColor> UnlitColors;

            private void Execute(
                ref HDRPMaterialPropertyAnimated animated, in HDRPMaterialPropertyClipData clipData, in TrackBinding trackBinding, in Clip clip)
            {
                animated.Value = CreateBlend(in clipData);

                if (!this.TrackComponents.TryGetRefRW(clip.Track, out var trackComponents))
                {
                    return;
                }

                ref var state = ref trackComponents.ValueRW;
                if (!state.ManageComponents)
                {
                    return;
                }

                var target = trackBinding.Value;
                if (target == Entity.Null)
                {
                    return;
                }

                var flags = clipData.Flags;
                if ((flags & HDRPMaterialPropertyFlags.AlphaCutoff) != 0)
                {
                    state.AlphaCutoffCount++;
                    if (state.AlphaCutoffCount == 1 && !this.AlphaCutoffs.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyAlphaCutoff { Value = clipData.AlphaCutoff });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.AlphaCutoff;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.AORemapMax) != 0)
                {
                    state.AORemapMaxCount++;
                    if (state.AORemapMaxCount == 1 && !this.AORemapMaxes.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyAORemapMax { Value = clipData.AORemapMax });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.AORemapMax;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.AORemapMin) != 0)
                {
                    state.AORemapMinCount++;
                    if (state.AORemapMinCount == 1 && !this.AORemapMins.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyAORemapMin { Value = clipData.AORemapMin });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.AORemapMin;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.DetailAlbedoScale) != 0)
                {
                    state.DetailAlbedoScaleCount++;
                    if (state.DetailAlbedoScaleCount == 1 && !this.DetailAlbedoScales.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyDetailAlbedoScale { Value = clipData.DetailAlbedoScale });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.DetailAlbedoScale;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.DetailNormalScale) != 0)
                {
                    state.DetailNormalScaleCount++;
                    if (state.DetailNormalScaleCount == 1 && !this.DetailNormalScales.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyDetailNormalScale { Value = clipData.DetailNormalScale });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.DetailNormalScale;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.DetailSmoothnessScale) != 0)
                {
                    state.DetailSmoothnessScaleCount++;
                    if (state.DetailSmoothnessScaleCount == 1 && !this.DetailSmoothnessScales.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyDetailSmoothnessScale { Value = clipData.DetailSmoothnessScale });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.DetailSmoothnessScale;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.DiffusionProfileHash) != 0)
                {
                    state.DiffusionProfileHashCount++;
                    if (state.DiffusionProfileHashCount == 1 && !this.DiffusionProfileHashes.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyDiffusionProfileHash { Value = clipData.DiffusionProfileHash });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.DiffusionProfileHash;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.Metallic) != 0)
                {
                    state.MetallicCount++;
                    if (state.MetallicCount == 1 && !this.Metallics.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyMetallic { Value = clipData.Metallic });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.Metallic;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.Smoothness) != 0)
                {
                    state.SmoothnessCount++;
                    if (state.SmoothnessCount == 1 && !this.Smoothnesses.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertySmoothness { Value = clipData.Smoothness });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.Smoothness;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.SmoothnessRemapMax) != 0)
                {
                    state.SmoothnessRemapMaxCount++;
                    if (state.SmoothnessRemapMaxCount == 1 && !this.SmoothnessRemapMaxes.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertySmoothnessRemapMax { Value = clipData.SmoothnessRemapMax });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.SmoothnessRemapMax;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.SmoothnessRemapMin) != 0)
                {
                    state.SmoothnessRemapMinCount++;
                    if (state.SmoothnessRemapMinCount == 1 && !this.SmoothnessRemapMins.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertySmoothnessRemapMin { Value = clipData.SmoothnessRemapMin });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.SmoothnessRemapMin;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.Thickness) != 0)
                {
                    state.ThicknessCount++;
                    if (state.ThicknessCount == 1 && !this.Thicknesses.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyThickness { Value = clipData.Thickness });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.Thickness;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.EmissiveColor) != 0)
                {
                    state.EmissiveColorCount++;
                    if (state.EmissiveColorCount == 1 && !this.EmissiveColors.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyEmissiveColor { Value = clipData.EmissiveColor });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.EmissiveColor;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.BaseColor) != 0)
                {
                    state.BaseColorCount++;
                    if (state.BaseColorCount == 1 && !this.BaseColors.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyBaseColor { Value = clipData.BaseColor });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.BaseColor;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.SpecularColor) != 0)
                {
                    state.SpecularColorCount++;
                    if (state.SpecularColorCount == 1 && !this.SpecularColors.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertySpecularColor { Value = clipData.SpecularColor });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.SpecularColor;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.ThicknessRemap) != 0)
                {
                    state.ThicknessRemapCount++;
                    if (state.ThicknessRemapCount == 1 && !this.ThicknessRemaps.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyThicknessRemap { Value = clipData.ThicknessRemap });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.ThicknessRemap;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.UnlitColor) != 0)
                {
                    state.UnlitColorCount++;
                    if (state.UnlitColorCount == 1 && !this.UnlitColors.HasComponent(target))
                    {
                        this.CommandBuffer.AddComponent(target, new HDRPMaterialPropertyUnlitColor { Value = clipData.UnlitColor });
                        state.AddedFlags |= HDRPMaterialPropertyFlags.UnlitColor;
                    }
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActivePrevious))]
        [WithDisabled(typeof(ClipActive))]
        private partial struct ClipComponentDeactivateJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public ComponentLookup<HDRPMaterialPropertyTrackComponents> TrackComponents;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyAlphaCutoff> AlphaCutoffs;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyAORemapMax> AORemapMaxes;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyAORemapMin> AORemapMins;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyDetailAlbedoScale> DetailAlbedoScales;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyDetailNormalScale> DetailNormalScales;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyDetailSmoothnessScale> DetailSmoothnessScales;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyDiffusionProfileHash> DiffusionProfileHashes;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyMetallic> Metallics;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertySmoothness> Smoothnesses;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertySmoothnessRemapMax> SmoothnessRemapMaxes;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertySmoothnessRemapMin> SmoothnessRemapMins;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyThickness> Thicknesses;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyEmissiveColor> EmissiveColors;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyBaseColor> BaseColors;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertySpecularColor> SpecularColors;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyThicknessRemap> ThicknessRemaps;

            [ReadOnly]
            public ComponentLookup<HDRPMaterialPropertyUnlitColor> UnlitColors;

            private void Execute(in HDRPMaterialPropertyClipData clipData, in TrackBinding trackBinding, in Clip clip)
            {
                if (!this.TrackComponents.TryGetRefRW(clip.Track, out var trackComponents))
                {
                    return;
                }

                ref var state = ref trackComponents.ValueRW;
                if (!state.ManageComponents)
                {
                    return;
                }

                var target = trackBinding.Value;
                if (target == Entity.Null)
                {
                    return;
                }

                var flags = clipData.Flags;
                if ((flags & HDRPMaterialPropertyFlags.AlphaCutoff) != 0)
                {
                    if (state.AlphaCutoffCount > 0)
                    {
                        state.AlphaCutoffCount--;
                    }

                    if (state.AlphaCutoffCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.AlphaCutoff) != 0)
                    {
                        if (this.AlphaCutoffs.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyAlphaCutoff>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.AlphaCutoff;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.AORemapMax) != 0)
                {
                    if (state.AORemapMaxCount > 0)
                    {
                        state.AORemapMaxCount--;
                    }

                    if (state.AORemapMaxCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.AORemapMax) != 0)
                    {
                        if (this.AORemapMaxes.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyAORemapMax>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.AORemapMax;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.AORemapMin) != 0)
                {
                    if (state.AORemapMinCount > 0)
                    {
                        state.AORemapMinCount--;
                    }

                    if (state.AORemapMinCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.AORemapMin) != 0)
                    {
                        if (this.AORemapMins.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyAORemapMin>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.AORemapMin;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.DetailAlbedoScale) != 0)
                {
                    if (state.DetailAlbedoScaleCount > 0)
                    {
                        state.DetailAlbedoScaleCount--;
                    }

                    if (state.DetailAlbedoScaleCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.DetailAlbedoScale) != 0)
                    {
                        if (this.DetailAlbedoScales.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyDetailAlbedoScale>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.DetailAlbedoScale;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.DetailNormalScale) != 0)
                {
                    if (state.DetailNormalScaleCount > 0)
                    {
                        state.DetailNormalScaleCount--;
                    }

                    if (state.DetailNormalScaleCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.DetailNormalScale) != 0)
                    {
                        if (this.DetailNormalScales.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyDetailNormalScale>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.DetailNormalScale;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.DetailSmoothnessScale) != 0)
                {
                    if (state.DetailSmoothnessScaleCount > 0)
                    {
                        state.DetailSmoothnessScaleCount--;
                    }

                    if (state.DetailSmoothnessScaleCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.DetailSmoothnessScale) != 0)
                    {
                        if (this.DetailSmoothnessScales.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyDetailSmoothnessScale>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.DetailSmoothnessScale;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.DiffusionProfileHash) != 0)
                {
                    if (state.DiffusionProfileHashCount > 0)
                    {
                        state.DiffusionProfileHashCount--;
                    }

                    if (state.DiffusionProfileHashCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.DiffusionProfileHash) != 0)
                    {
                        if (this.DiffusionProfileHashes.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyDiffusionProfileHash>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.DiffusionProfileHash;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.Metallic) != 0)
                {
                    if (state.MetallicCount > 0)
                    {
                        state.MetallicCount--;
                    }

                    if (state.MetallicCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.Metallic) != 0)
                    {
                        if (this.Metallics.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyMetallic>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.Metallic;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.Smoothness) != 0)
                {
                    if (state.SmoothnessCount > 0)
                    {
                        state.SmoothnessCount--;
                    }

                    if (state.SmoothnessCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.Smoothness) != 0)
                    {
                        if (this.Smoothnesses.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertySmoothness>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.Smoothness;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.SmoothnessRemapMax) != 0)
                {
                    if (state.SmoothnessRemapMaxCount > 0)
                    {
                        state.SmoothnessRemapMaxCount--;
                    }

                    if (state.SmoothnessRemapMaxCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.SmoothnessRemapMax) != 0)
                    {
                        if (this.SmoothnessRemapMaxes.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertySmoothnessRemapMax>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.SmoothnessRemapMax;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.SmoothnessRemapMin) != 0)
                {
                    if (state.SmoothnessRemapMinCount > 0)
                    {
                        state.SmoothnessRemapMinCount--;
                    }

                    if (state.SmoothnessRemapMinCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.SmoothnessRemapMin) != 0)
                    {
                        if (this.SmoothnessRemapMins.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertySmoothnessRemapMin>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.SmoothnessRemapMin;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.Thickness) != 0)
                {
                    if (state.ThicknessCount > 0)
                    {
                        state.ThicknessCount--;
                    }

                    if (state.ThicknessCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.Thickness) != 0)
                    {
                        if (this.Thicknesses.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyThickness>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.Thickness;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.EmissiveColor) != 0)
                {
                    if (state.EmissiveColorCount > 0)
                    {
                        state.EmissiveColorCount--;
                    }

                    if (state.EmissiveColorCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.EmissiveColor) != 0)
                    {
                        if (this.EmissiveColors.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyEmissiveColor>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.EmissiveColor;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.BaseColor) != 0)
                {
                    if (state.BaseColorCount > 0)
                    {
                        state.BaseColorCount--;
                    }

                    if (state.BaseColorCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.BaseColor) != 0)
                    {
                        if (this.BaseColors.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyBaseColor>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.BaseColor;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.SpecularColor) != 0)
                {
                    if (state.SpecularColorCount > 0)
                    {
                        state.SpecularColorCount--;
                    }

                    if (state.SpecularColorCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.SpecularColor) != 0)
                    {
                        if (this.SpecularColors.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertySpecularColor>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.SpecularColor;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.ThicknessRemap) != 0)
                {
                    if (state.ThicknessRemapCount > 0)
                    {
                        state.ThicknessRemapCount--;
                    }

                    if (state.ThicknessRemapCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.ThicknessRemap) != 0)
                    {
                        if (this.ThicknessRemaps.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyThicknessRemap>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.ThicknessRemap;
                    }
                }

                if ((flags & HDRPMaterialPropertyFlags.UnlitColor) != 0)
                {
                    if (state.UnlitColorCount > 0)
                    {
                        state.UnlitColorCount--;
                    }

                    if (state.UnlitColorCount == 0 && (state.AddedFlags & HDRPMaterialPropertyFlags.UnlitColor) != 0)
                    {
                        if (this.UnlitColors.HasComponent(target))
                        {
                            this.CommandBuffer.RemoveComponent<HDRPMaterialPropertyUnlitColor>(target);
                        }

                        state.AddedFlags &= ~HDRPMaterialPropertyFlags.UnlitColor;
                    }
                }
            }
        }

        private static HDRPMaterialPropertyBlend CreateBlend(in HDRPMaterialPropertyClipData clipData)
        {
            var blend = new HDRPMaterialPropertyBlend();
            var flags = clipData.Flags;

            if ((flags & HDRPMaterialPropertyFlags.AlphaCutoff) != 0)
            {
                blend.AlphaCutoff = clipData.AlphaCutoff;
                blend.EnableAlphaCutoff = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.AORemapMax) != 0)
            {
                blend.AORemapMax = clipData.AORemapMax;
                blend.EnableAORemapMax = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.AORemapMin) != 0)
            {
                blend.AORemapMin = clipData.AORemapMin;
                blend.EnableAORemapMin = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.DetailAlbedoScale) != 0)
            {
                blend.DetailAlbedoScale = clipData.DetailAlbedoScale;
                blend.EnableDetailAlbedoScale = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.DetailNormalScale) != 0)
            {
                blend.DetailNormalScale = clipData.DetailNormalScale;
                blend.EnableDetailNormalScale = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.DetailSmoothnessScale) != 0)
            {
                blend.DetailSmoothnessScale = clipData.DetailSmoothnessScale;
                blend.EnableDetailSmoothnessScale = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.DiffusionProfileHash) != 0)
            {
                blend.DiffusionProfileHash = clipData.DiffusionProfileHash;
                blend.EnableDiffusionProfileHash = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.Metallic) != 0)
            {
                blend.Metallic = clipData.Metallic;
                blend.EnableMetallic = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.Smoothness) != 0)
            {
                blend.Smoothness = clipData.Smoothness;
                blend.EnableSmoothness = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.SmoothnessRemapMax) != 0)
            {
                blend.SmoothnessRemapMax = clipData.SmoothnessRemapMax;
                blend.EnableSmoothnessRemapMax = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.SmoothnessRemapMin) != 0)
            {
                blend.SmoothnessRemapMin = clipData.SmoothnessRemapMin;
                blend.EnableSmoothnessRemapMin = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.Thickness) != 0)
            {
                blend.Thickness = clipData.Thickness;
                blend.EnableThickness = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.EmissiveColor) != 0)
            {
                blend.EmissiveColor = clipData.EmissiveColor;
                blend.EnableEmissiveColor = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.BaseColor) != 0)
            {
                blend.BaseColor = clipData.BaseColor;
                blend.EnableBaseColor = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.SpecularColor) != 0)
            {
                blend.SpecularColor = clipData.SpecularColor;
                blend.EnableSpecularColor = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.ThicknessRemap) != 0)
            {
                blend.ThicknessRemap = clipData.ThicknessRemap;
                blend.EnableThicknessRemap = true;
            }

            if ((flags & HDRPMaterialPropertyFlags.UnlitColor) != 0)
            {
                blend.UnlitColor = clipData.UnlitColor;
                blend.EnableUnlitColor = true;
            }

            return blend;
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<HDRPMaterialPropertyBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyAlphaCutoff> AlphaCutoffs;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyAORemapMax> AORemapMaxes;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyAORemapMin> AORemapMins;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyDetailAlbedoScale> DetailAlbedoScales;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyDetailNormalScale> DetailNormalScales;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyDetailSmoothnessScale> DetailSmoothnessScales;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyDiffusionProfileHash> DiffusionProfileHashes;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyMetallic> Metallics;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertySmoothness> Smoothnesses;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertySmoothnessRemapMax> SmoothnessRemapMaxes;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertySmoothnessRemapMin> SmoothnessRemapMins;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyThickness> Thicknesses;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyEmissiveColor> EmissiveColors;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyBaseColor> BaseColors;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertySpecularColor> SpecularColors;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyThicknessRemap> ThicknessRemaps;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<HDRPMaterialPropertyUnlitColor> UnlitColors;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                var current = new HDRPMaterialPropertyBlend();
                var enableAlphaCutoff = mixData.Value1.EnableAlphaCutoff || mixData.Value2.EnableAlphaCutoff ||
                    mixData.Value3.EnableAlphaCutoff || mixData.Value4.EnableAlphaCutoff;
                var enableAORemapMax = mixData.Value1.EnableAORemapMax || mixData.Value2.EnableAORemapMax ||
                    mixData.Value3.EnableAORemapMax || mixData.Value4.EnableAORemapMax;
                var enableAORemapMin = mixData.Value1.EnableAORemapMin || mixData.Value2.EnableAORemapMin ||
                    mixData.Value3.EnableAORemapMin || mixData.Value4.EnableAORemapMin;
                var enableDetailAlbedoScale = mixData.Value1.EnableDetailAlbedoScale || mixData.Value2.EnableDetailAlbedoScale ||
                    mixData.Value3.EnableDetailAlbedoScale || mixData.Value4.EnableDetailAlbedoScale;
                var enableDetailNormalScale = mixData.Value1.EnableDetailNormalScale || mixData.Value2.EnableDetailNormalScale ||
                    mixData.Value3.EnableDetailNormalScale || mixData.Value4.EnableDetailNormalScale;
                var enableDetailSmoothnessScale = mixData.Value1.EnableDetailSmoothnessScale || mixData.Value2.EnableDetailSmoothnessScale ||
                    mixData.Value3.EnableDetailSmoothnessScale || mixData.Value4.EnableDetailSmoothnessScale;
                var enableDiffusionProfileHash = mixData.Value1.EnableDiffusionProfileHash || mixData.Value2.EnableDiffusionProfileHash ||
                    mixData.Value3.EnableDiffusionProfileHash || mixData.Value4.EnableDiffusionProfileHash;
                var enableMetallic = mixData.Value1.EnableMetallic || mixData.Value2.EnableMetallic ||
                    mixData.Value3.EnableMetallic || mixData.Value4.EnableMetallic;
                var enableSmoothness = mixData.Value1.EnableSmoothness || mixData.Value2.EnableSmoothness ||
                    mixData.Value3.EnableSmoothness || mixData.Value4.EnableSmoothness;
                var enableSmoothnessRemapMax = mixData.Value1.EnableSmoothnessRemapMax || mixData.Value2.EnableSmoothnessRemapMax ||
                    mixData.Value3.EnableSmoothnessRemapMax || mixData.Value4.EnableSmoothnessRemapMax;
                var enableSmoothnessRemapMin = mixData.Value1.EnableSmoothnessRemapMin || mixData.Value2.EnableSmoothnessRemapMin ||
                    mixData.Value3.EnableSmoothnessRemapMin || mixData.Value4.EnableSmoothnessRemapMin;
                var enableThickness = mixData.Value1.EnableThickness || mixData.Value2.EnableThickness ||
                    mixData.Value3.EnableThickness || mixData.Value4.EnableThickness;
                var enableEmissiveColor = mixData.Value1.EnableEmissiveColor || mixData.Value2.EnableEmissiveColor ||
                    mixData.Value3.EnableEmissiveColor || mixData.Value4.EnableEmissiveColor;
                var enableBaseColor = mixData.Value1.EnableBaseColor || mixData.Value2.EnableBaseColor ||
                    mixData.Value3.EnableBaseColor || mixData.Value4.EnableBaseColor;
                var enableSpecularColor = mixData.Value1.EnableSpecularColor || mixData.Value2.EnableSpecularColor ||
                    mixData.Value3.EnableSpecularColor || mixData.Value4.EnableSpecularColor;
                var enableThicknessRemap = mixData.Value1.EnableThicknessRemap || mixData.Value2.EnableThicknessRemap ||
                    mixData.Value3.EnableThicknessRemap || mixData.Value4.EnableThicknessRemap;
                var enableUnlitColor = mixData.Value1.EnableUnlitColor || mixData.Value2.EnableUnlitColor ||
                    mixData.Value3.EnableUnlitColor || mixData.Value4.EnableUnlitColor;

                RefRW<HDRPMaterialPropertyAlphaCutoff> alphaCutoffRw = default;
                RefRW<HDRPMaterialPropertyAORemapMax> aoRemapMaxRw = default;
                RefRW<HDRPMaterialPropertyAORemapMin> aoRemapMinRw = default;
                RefRW<HDRPMaterialPropertyDetailAlbedoScale> detailAlbedoScaleRw = default;
                RefRW<HDRPMaterialPropertyDetailNormalScale> detailNormalScaleRw = default;
                RefRW<HDRPMaterialPropertyDetailSmoothnessScale> detailSmoothnessScaleRw = default;
                RefRW<HDRPMaterialPropertyDiffusionProfileHash> diffusionProfileHashRw = default;
                RefRW<HDRPMaterialPropertyMetallic> metallicRw = default;
                RefRW<HDRPMaterialPropertySmoothness> smoothnessRw = default;
                RefRW<HDRPMaterialPropertySmoothnessRemapMax> smoothnessRemapMaxRw = default;
                RefRW<HDRPMaterialPropertySmoothnessRemapMin> smoothnessRemapMinRw = default;
                RefRW<HDRPMaterialPropertyThickness> thicknessRw = default;
                RefRW<HDRPMaterialPropertyEmissiveColor> emissiveColorRw = default;
                RefRW<HDRPMaterialPropertyBaseColor> baseColorRw = default;
                RefRW<HDRPMaterialPropertySpecularColor> specularColorRw = default;
                RefRW<HDRPMaterialPropertyThicknessRemap> thicknessRemapRw = default;
                RefRW<HDRPMaterialPropertyUnlitColor> unlitColorRw = default;

                if (enableAlphaCutoff)
                {
                    this.AlphaCutoffs.TryGetRefRW(entity, out alphaCutoffRw);
                    if (alphaCutoffRw.IsValid)
                    {
                        current.AlphaCutoff = alphaCutoffRw.ValueRO.Value;
                        current.EnableAlphaCutoff = true;
                    }
                }

                if (enableAORemapMax)
                {
                    this.AORemapMaxes.TryGetRefRW(entity, out aoRemapMaxRw);
                    if (aoRemapMaxRw.IsValid)
                    {
                        current.AORemapMax = aoRemapMaxRw.ValueRO.Value;
                        current.EnableAORemapMax = true;
                    }
                }

                if (enableAORemapMin)
                {
                    this.AORemapMins.TryGetRefRW(entity, out aoRemapMinRw);
                    if (aoRemapMinRw.IsValid)
                    {
                        current.AORemapMin = aoRemapMinRw.ValueRO.Value;
                        current.EnableAORemapMin = true;
                    }
                }

                if (enableDetailAlbedoScale)
                {
                    this.DetailAlbedoScales.TryGetRefRW(entity, out detailAlbedoScaleRw);
                    if (detailAlbedoScaleRw.IsValid)
                    {
                        current.DetailAlbedoScale = detailAlbedoScaleRw.ValueRO.Value;
                        current.EnableDetailAlbedoScale = true;
                    }
                }

                if (enableDetailNormalScale)
                {
                    this.DetailNormalScales.TryGetRefRW(entity, out detailNormalScaleRw);
                    if (detailNormalScaleRw.IsValid)
                    {
                        current.DetailNormalScale = detailNormalScaleRw.ValueRO.Value;
                        current.EnableDetailNormalScale = true;
                    }
                }

                if (enableDetailSmoothnessScale)
                {
                    this.DetailSmoothnessScales.TryGetRefRW(entity, out detailSmoothnessScaleRw);
                    if (detailSmoothnessScaleRw.IsValid)
                    {
                        current.DetailSmoothnessScale = detailSmoothnessScaleRw.ValueRO.Value;
                        current.EnableDetailSmoothnessScale = true;
                    }
                }

                if (enableDiffusionProfileHash)
                {
                    this.DiffusionProfileHashes.TryGetRefRW(entity, out diffusionProfileHashRw);
                    if (diffusionProfileHashRw.IsValid)
                    {
                        current.DiffusionProfileHash = diffusionProfileHashRw.ValueRO.Value;
                        current.EnableDiffusionProfileHash = true;
                    }
                }

                if (enableMetallic)
                {
                    this.Metallics.TryGetRefRW(entity, out metallicRw);
                    if (metallicRw.IsValid)
                    {
                        current.Metallic = metallicRw.ValueRO.Value;
                        current.EnableMetallic = true;
                    }
                }

                if (enableSmoothness)
                {
                    this.Smoothnesses.TryGetRefRW(entity, out smoothnessRw);
                    if (smoothnessRw.IsValid)
                    {
                        current.Smoothness = smoothnessRw.ValueRO.Value;
                        current.EnableSmoothness = true;
                    }
                }

                if (enableSmoothnessRemapMax)
                {
                    this.SmoothnessRemapMaxes.TryGetRefRW(entity, out smoothnessRemapMaxRw);
                    if (smoothnessRemapMaxRw.IsValid)
                    {
                        current.SmoothnessRemapMax = smoothnessRemapMaxRw.ValueRO.Value;
                        current.EnableSmoothnessRemapMax = true;
                    }
                }

                if (enableSmoothnessRemapMin)
                {
                    this.SmoothnessRemapMins.TryGetRefRW(entity, out smoothnessRemapMinRw);
                    if (smoothnessRemapMinRw.IsValid)
                    {
                        current.SmoothnessRemapMin = smoothnessRemapMinRw.ValueRO.Value;
                        current.EnableSmoothnessRemapMin = true;
                    }
                }

                if (enableThickness)
                {
                    this.Thicknesses.TryGetRefRW(entity, out thicknessRw);
                    if (thicknessRw.IsValid)
                    {
                        current.Thickness = thicknessRw.ValueRO.Value;
                        current.EnableThickness = true;
                    }
                }

                if (enableEmissiveColor)
                {
                    this.EmissiveColors.TryGetRefRW(entity, out emissiveColorRw);
                    if (emissiveColorRw.IsValid)
                    {
                        current.EmissiveColor = emissiveColorRw.ValueRO.Value;
                        current.EnableEmissiveColor = true;
                    }
                }

                if (enableBaseColor)
                {
                    this.BaseColors.TryGetRefRW(entity, out baseColorRw);
                    if (baseColorRw.IsValid)
                    {
                        current.BaseColor = baseColorRw.ValueRO.Value;
                        current.EnableBaseColor = true;
                    }
                }

                if (enableSpecularColor)
                {
                    this.SpecularColors.TryGetRefRW(entity, out specularColorRw);
                    if (specularColorRw.IsValid)
                    {
                        current.SpecularColor = specularColorRw.ValueRO.Value;
                        current.EnableSpecularColor = true;
                    }
                }

                if (enableThicknessRemap)
                {
                    this.ThicknessRemaps.TryGetRefRW(entity, out thicknessRemapRw);
                    if (thicknessRemapRw.IsValid)
                    {
                        current.ThicknessRemap = thicknessRemapRw.ValueRO.Value;
                        current.EnableThicknessRemap = true;
                    }
                }

                if (enableUnlitColor)
                {
                    this.UnlitColors.TryGetRefRW(entity, out unlitColorRw);
                    if (unlitColorRw.IsValid)
                    {
                        current.UnlitColor = unlitColorRw.ValueRO.Value;
                        current.EnableUnlitColor = true;
                    }
                }

                var blended = JobHelpers.Blend(ref mixData, current, default(HDRPMaterialPropertyMixer));

                if (enableAlphaCutoff && blended.EnableAlphaCutoff && alphaCutoffRw.IsValid)
                {
                    alphaCutoffRw.ValueRW.Value = blended.AlphaCutoff;
                }

                if (enableAORemapMax && blended.EnableAORemapMax && aoRemapMaxRw.IsValid)
                {
                    aoRemapMaxRw.ValueRW.Value = blended.AORemapMax;
                }

                if (enableAORemapMin && blended.EnableAORemapMin && aoRemapMinRw.IsValid)
                {
                    aoRemapMinRw.ValueRW.Value = blended.AORemapMin;
                }

                if (enableDetailAlbedoScale && blended.EnableDetailAlbedoScale && detailAlbedoScaleRw.IsValid)
                {
                    detailAlbedoScaleRw.ValueRW.Value = blended.DetailAlbedoScale;
                }

                if (enableDetailNormalScale && blended.EnableDetailNormalScale && detailNormalScaleRw.IsValid)
                {
                    detailNormalScaleRw.ValueRW.Value = blended.DetailNormalScale;
                }

                if (enableDetailSmoothnessScale && blended.EnableDetailSmoothnessScale && detailSmoothnessScaleRw.IsValid)
                {
                    detailSmoothnessScaleRw.ValueRW.Value = blended.DetailSmoothnessScale;
                }

                if (enableDiffusionProfileHash && blended.EnableDiffusionProfileHash && diffusionProfileHashRw.IsValid)
                {
                    diffusionProfileHashRw.ValueRW.Value = blended.DiffusionProfileHash;
                }

                if (enableMetallic && blended.EnableMetallic && metallicRw.IsValid)
                {
                    metallicRw.ValueRW.Value = blended.Metallic;
                }

                if (enableSmoothness && blended.EnableSmoothness && smoothnessRw.IsValid)
                {
                    smoothnessRw.ValueRW.Value = blended.Smoothness;
                }

                if (enableSmoothnessRemapMax && blended.EnableSmoothnessRemapMax && smoothnessRemapMaxRw.IsValid)
                {
                    smoothnessRemapMaxRw.ValueRW.Value = blended.SmoothnessRemapMax;
                }

                if (enableSmoothnessRemapMin && blended.EnableSmoothnessRemapMin && smoothnessRemapMinRw.IsValid)
                {
                    smoothnessRemapMinRw.ValueRW.Value = blended.SmoothnessRemapMin;
                }

                if (enableThickness && blended.EnableThickness && thicknessRw.IsValid)
                {
                    thicknessRw.ValueRW.Value = blended.Thickness;
                }

                if (enableEmissiveColor && blended.EnableEmissiveColor && emissiveColorRw.IsValid)
                {
                    emissiveColorRw.ValueRW.Value = blended.EmissiveColor;
                }

                if (enableBaseColor && blended.EnableBaseColor && baseColorRw.IsValid)
                {
                    baseColorRw.ValueRW.Value = blended.BaseColor;
                }

                if (enableSpecularColor && blended.EnableSpecularColor && specularColorRw.IsValid)
                {
                    specularColorRw.ValueRW.Value = blended.SpecularColor;
                }

                if (enableThicknessRemap && blended.EnableThicknessRemap && thicknessRemapRw.IsValid)
                {
                    thicknessRemapRw.ValueRW.Value = blended.ThicknessRemap;
                }

                if (enableUnlitColor && blended.EnableUnlitColor && unlitColorRw.IsValid)
                {
                    unlitColorRw.ValueRW.Value = blended.UnlitColor;
                }
            }
        }

        private struct HDRPMaterialPropertyMixer : IMixer<HDRPMaterialPropertyBlend>
        {
            public HDRPMaterialPropertyBlend Lerp(in HDRPMaterialPropertyBlend a, in HDRPMaterialPropertyBlend b, in float s)
            {
                var alphaCutoff = MaterialPropertyBlendUtility.BlendValue(a.AlphaCutoff, a.EnableAlphaCutoff, b.AlphaCutoff, b.EnableAlphaCutoff, s, out var enableAlphaCutoff);
                var aoRemapMax = MaterialPropertyBlendUtility.BlendValue(a.AORemapMax, a.EnableAORemapMax, b.AORemapMax, b.EnableAORemapMax, s, out var enableAORemapMax);
                var aoRemapMin = MaterialPropertyBlendUtility.BlendValue(a.AORemapMin, a.EnableAORemapMin, b.AORemapMin, b.EnableAORemapMin, s, out var enableAORemapMin);
                var detailAlbedoScale = MaterialPropertyBlendUtility.BlendValue(
                    a.DetailAlbedoScale, a.EnableDetailAlbedoScale, b.DetailAlbedoScale, b.EnableDetailAlbedoScale, s, out var enableDetailAlbedoScale);
                var detailNormalScale = MaterialPropertyBlendUtility.BlendValue(
                    a.DetailNormalScale, a.EnableDetailNormalScale, b.DetailNormalScale, b.EnableDetailNormalScale, s, out var enableDetailNormalScale);
                var detailSmoothnessScale = MaterialPropertyBlendUtility.BlendValue(
                    a.DetailSmoothnessScale, a.EnableDetailSmoothnessScale, b.DetailSmoothnessScale, b.EnableDetailSmoothnessScale, s,
                    out var enableDetailSmoothnessScale);
                var diffusionProfileHash = MaterialPropertyBlendUtility.BlendValue(
                    a.DiffusionProfileHash, a.EnableDiffusionProfileHash, b.DiffusionProfileHash, b.EnableDiffusionProfileHash, s,
                    out var enableDiffusionProfileHash);
                var metallic = MaterialPropertyBlendUtility.BlendValue(a.Metallic, a.EnableMetallic, b.Metallic, b.EnableMetallic, s, out var enableMetallic);
                var smoothness = MaterialPropertyBlendUtility.BlendValue(a.Smoothness, a.EnableSmoothness, b.Smoothness, b.EnableSmoothness, s, out var enableSmoothness);
                var smoothnessRemapMax = MaterialPropertyBlendUtility.BlendValue(
                    a.SmoothnessRemapMax, a.EnableSmoothnessRemapMax, b.SmoothnessRemapMax, b.EnableSmoothnessRemapMax, s, out var enableSmoothnessRemapMax);
                var smoothnessRemapMin = MaterialPropertyBlendUtility.BlendValue(
                    a.SmoothnessRemapMin, a.EnableSmoothnessRemapMin, b.SmoothnessRemapMin, b.EnableSmoothnessRemapMin, s, out var enableSmoothnessRemapMin);
                var thickness = MaterialPropertyBlendUtility.BlendValue(a.Thickness, a.EnableThickness, b.Thickness, b.EnableThickness, s, out var enableThickness);
                var emissiveColor = MaterialPropertyBlendUtility.BlendValue(
                    a.EmissiveColor, a.EnableEmissiveColor, b.EmissiveColor, b.EnableEmissiveColor, s, out var enableEmissiveColor);
                var baseColor = MaterialPropertyBlendUtility.BlendValue(a.BaseColor, a.EnableBaseColor, b.BaseColor, b.EnableBaseColor, s, out var enableBaseColor);
                var specularColor = MaterialPropertyBlendUtility.BlendValue(
                    a.SpecularColor, a.EnableSpecularColor, b.SpecularColor, b.EnableSpecularColor, s, out var enableSpecularColor);
                var thicknessRemap = MaterialPropertyBlendUtility.BlendValue(
                    a.ThicknessRemap, a.EnableThicknessRemap, b.ThicknessRemap, b.EnableThicknessRemap, s, out var enableThicknessRemap);
                var unlitColor = MaterialPropertyBlendUtility.BlendValue(a.UnlitColor, a.EnableUnlitColor, b.UnlitColor, b.EnableUnlitColor, s, out var enableUnlitColor);

                return new HDRPMaterialPropertyBlend
                {
                    AlphaCutoff = alphaCutoff,
                    AORemapMax = aoRemapMax,
                    AORemapMin = aoRemapMin,
                    DetailAlbedoScale = detailAlbedoScale,
                    DetailNormalScale = detailNormalScale,
                    DetailSmoothnessScale = detailSmoothnessScale,
                    DiffusionProfileHash = diffusionProfileHash,
                    Metallic = metallic,
                    Smoothness = smoothness,
                    SmoothnessRemapMax = smoothnessRemapMax,
                    SmoothnessRemapMin = smoothnessRemapMin,
                    Thickness = thickness,
                    EmissiveColor = emissiveColor,
                    BaseColor = baseColor,
                    SpecularColor = specularColor,
                    ThicknessRemap = thicknessRemap,
                    UnlitColor = unlitColor,
                    EnableAlphaCutoff = enableAlphaCutoff,
                    EnableAORemapMax = enableAORemapMax,
                    EnableAORemapMin = enableAORemapMin,
                    EnableDetailAlbedoScale = enableDetailAlbedoScale,
                    EnableDetailNormalScale = enableDetailNormalScale,
                    EnableDetailSmoothnessScale = enableDetailSmoothnessScale,
                    EnableDiffusionProfileHash = enableDiffusionProfileHash,
                    EnableMetallic = enableMetallic,
                    EnableSmoothness = enableSmoothness,
                    EnableSmoothnessRemapMax = enableSmoothnessRemapMax,
                    EnableSmoothnessRemapMin = enableSmoothnessRemapMin,
                    EnableThickness = enableThickness,
                    EnableEmissiveColor = enableEmissiveColor,
                    EnableBaseColor = enableBaseColor,
                    EnableSpecularColor = enableSpecularColor,
                    EnableThicknessRemap = enableThicknessRemap,
                    EnableUnlitColor = enableUnlitColor,
                };
            }

            public HDRPMaterialPropertyBlend Add(in HDRPMaterialPropertyBlend a, in HDRPMaterialPropertyBlend b)
            {
                var alphaCutoff = MaterialPropertyBlendUtility.AddValue(a.AlphaCutoff, a.EnableAlphaCutoff, b.AlphaCutoff, b.EnableAlphaCutoff, out var enableAlphaCutoff);
                var aoRemapMax = MaterialPropertyBlendUtility.AddValue(a.AORemapMax, a.EnableAORemapMax, b.AORemapMax, b.EnableAORemapMax, out var enableAORemapMax);
                var aoRemapMin = MaterialPropertyBlendUtility.AddValue(a.AORemapMin, a.EnableAORemapMin, b.AORemapMin, b.EnableAORemapMin, out var enableAORemapMin);
                var detailAlbedoScale = MaterialPropertyBlendUtility.AddValue(
                    a.DetailAlbedoScale, a.EnableDetailAlbedoScale, b.DetailAlbedoScale, b.EnableDetailAlbedoScale, out var enableDetailAlbedoScale);
                var detailNormalScale = MaterialPropertyBlendUtility.AddValue(
                    a.DetailNormalScale, a.EnableDetailNormalScale, b.DetailNormalScale, b.EnableDetailNormalScale, out var enableDetailNormalScale);
                var detailSmoothnessScale = MaterialPropertyBlendUtility.AddValue(
                    a.DetailSmoothnessScale, a.EnableDetailSmoothnessScale, b.DetailSmoothnessScale, b.EnableDetailSmoothnessScale, out var enableDetailSmoothnessScale);
                var diffusionProfileHash = MaterialPropertyBlendUtility.AddValue(
                    a.DiffusionProfileHash, a.EnableDiffusionProfileHash, b.DiffusionProfileHash, b.EnableDiffusionProfileHash, out var enableDiffusionProfileHash);
                var metallic = MaterialPropertyBlendUtility.AddValue(a.Metallic, a.EnableMetallic, b.Metallic, b.EnableMetallic, out var enableMetallic);
                var smoothness = MaterialPropertyBlendUtility.AddValue(a.Smoothness, a.EnableSmoothness, b.Smoothness, b.EnableSmoothness, out var enableSmoothness);
                var smoothnessRemapMax = MaterialPropertyBlendUtility.AddValue(
                    a.SmoothnessRemapMax, a.EnableSmoothnessRemapMax, b.SmoothnessRemapMax, b.EnableSmoothnessRemapMax, out var enableSmoothnessRemapMax);
                var smoothnessRemapMin = MaterialPropertyBlendUtility.AddValue(
                    a.SmoothnessRemapMin, a.EnableSmoothnessRemapMin, b.SmoothnessRemapMin, b.EnableSmoothnessRemapMin, out var enableSmoothnessRemapMin);
                var thickness = MaterialPropertyBlendUtility.AddValue(a.Thickness, a.EnableThickness, b.Thickness, b.EnableThickness, out var enableThickness);
                var emissiveColor = MaterialPropertyBlendUtility.AddValue(
                    a.EmissiveColor, a.EnableEmissiveColor, b.EmissiveColor, b.EnableEmissiveColor, out var enableEmissiveColor);
                var baseColor = MaterialPropertyBlendUtility.AddValue(a.BaseColor, a.EnableBaseColor, b.BaseColor, b.EnableBaseColor, out var enableBaseColor);
                var specularColor = MaterialPropertyBlendUtility.AddValue(
                    a.SpecularColor, a.EnableSpecularColor, b.SpecularColor, b.EnableSpecularColor, out var enableSpecularColor);
                var thicknessRemap = MaterialPropertyBlendUtility.AddValue(
                    a.ThicknessRemap, a.EnableThicknessRemap, b.ThicknessRemap, b.EnableThicknessRemap, out var enableThicknessRemap);
                var unlitColor = MaterialPropertyBlendUtility.AddValue(a.UnlitColor, a.EnableUnlitColor, b.UnlitColor, b.EnableUnlitColor, out var enableUnlitColor);

                return new HDRPMaterialPropertyBlend
                {
                    AlphaCutoff = alphaCutoff,
                    AORemapMax = aoRemapMax,
                    AORemapMin = aoRemapMin,
                    DetailAlbedoScale = detailAlbedoScale,
                    DetailNormalScale = detailNormalScale,
                    DetailSmoothnessScale = detailSmoothnessScale,
                    DiffusionProfileHash = diffusionProfileHash,
                    Metallic = metallic,
                    Smoothness = smoothness,
                    SmoothnessRemapMax = smoothnessRemapMax,
                    SmoothnessRemapMin = smoothnessRemapMin,
                    Thickness = thickness,
                    EmissiveColor = emissiveColor,
                    BaseColor = baseColor,
                    SpecularColor = specularColor,
                    ThicknessRemap = thicknessRemap,
                    UnlitColor = unlitColor,
                    EnableAlphaCutoff = enableAlphaCutoff,
                    EnableAORemapMax = enableAORemapMax,
                    EnableAORemapMin = enableAORemapMin,
                    EnableDetailAlbedoScale = enableDetailAlbedoScale,
                    EnableDetailNormalScale = enableDetailNormalScale,
                    EnableDetailSmoothnessScale = enableDetailSmoothnessScale,
                    EnableDiffusionProfileHash = enableDiffusionProfileHash,
                    EnableMetallic = enableMetallic,
                    EnableSmoothness = enableSmoothness,
                    EnableSmoothnessRemapMax = enableSmoothnessRemapMax,
                    EnableSmoothnessRemapMin = enableSmoothnessRemapMin,
                    EnableThickness = enableThickness,
                    EnableEmissiveColor = enableEmissiveColor,
                    EnableBaseColor = enableBaseColor,
                    EnableSpecularColor = enableSpecularColor,
                    EnableThicknessRemap = enableThicknessRemap,
                    EnableUnlitColor = enableUnlitColor,
                };
            }
        }
    }
}
#endif
