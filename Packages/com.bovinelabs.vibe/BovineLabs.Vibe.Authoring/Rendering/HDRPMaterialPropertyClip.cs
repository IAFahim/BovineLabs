// <copyright file="HDRPMaterialPropertyClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_HDRP
namespace BovineLabs.Vibe.Authoring.Rendering
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Rendering;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that sets HDRP material properties.
    /// </summary>
    [Serializable]
    public class HDRPMaterialPropertyClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("HDRP material properties to override.")]
        private HDRPMaterialPropertyFlags properties;

        [SerializeField]
        [Tooltip("Alpha cutoff value.")]
        private float alphaCutoff;

        [SerializeField]
        [Tooltip("AO remap max value.")]
        private float aoRemapMax;

        [SerializeField]
        [Tooltip("AO remap min value.")]
        private float aoRemapMin;

        [SerializeField]
        [Tooltip("Detail albedo scale value.")]
        private float detailAlbedoScale;

        [SerializeField]
        [Tooltip("Detail normal scale value.")]
        private float detailNormalScale;

        [SerializeField]
        [Tooltip("Detail smoothness scale value.")]
        private float detailSmoothnessScale;

        [SerializeField]
        [Tooltip("Diffusion profile hash value.")]
        private float diffusionProfileHash;

        [SerializeField]
        [Tooltip("Metallic value.")]
        private float metallic;

        [SerializeField]
        [Tooltip("Smoothness value.")]
        private float smoothness;

        [SerializeField]
        [Tooltip("Smoothness remap max value.")]
        private float smoothnessRemapMax;

        [SerializeField]
        [Tooltip("Smoothness remap min value.")]
        private float smoothnessRemapMin;

        [SerializeField]
        [Tooltip("Thickness value.")]
        private float thickness;

        [SerializeField]
        [Tooltip("Emissive color value.")]
        private Vector3 emissiveColor;

        [SerializeField]
        [Tooltip("Base color value.")]
        private Vector4 baseColor;

        [SerializeField]
        [Tooltip("Specular color value.")]
        private Vector4 specularColor;

        [SerializeField]
        [Tooltip("Thickness remap value.")]
        private Vector4 thicknessRemap;

        [SerializeField]
        [Tooltip("Unlit color value.")]
        private Vector4 unlitColor;

        internal HDRPMaterialPropertyFlags Properties => this.properties;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<HDRPMaterialPropertyAnimated>(clipEntity);
            context.Baker.AddComponent(clipEntity, new HDRPMaterialPropertyClipData
            {
                Flags = this.properties,
                AlphaCutoff = this.alphaCutoff,
                AORemapMax = this.aoRemapMax,
                AORemapMin = this.aoRemapMin,
                DetailAlbedoScale = this.detailAlbedoScale,
                DetailNormalScale = this.detailNormalScale,
                DetailSmoothnessScale = this.detailSmoothnessScale,
                DiffusionProfileHash = this.diffusionProfileHash,
                Metallic = this.metallic,
                Smoothness = this.smoothness,
                SmoothnessRemapMax = this.smoothnessRemapMax,
                SmoothnessRemapMin = this.smoothnessRemapMin,
                Thickness = this.thickness,
                EmissiveColor = new float3(this.emissiveColor.x, this.emissiveColor.y, this.emissiveColor.z),
                BaseColor = new float4(this.baseColor.x, this.baseColor.y, this.baseColor.z, this.baseColor.w),
                SpecularColor = new float4(this.specularColor.x, this.specularColor.y, this.specularColor.z, this.specularColor.w),
                ThicknessRemap = new float4(this.thicknessRemap.x, this.thicknessRemap.y, this.thicknessRemap.z, this.thicknessRemap.w),
                UnlitColor = new float4(this.unlitColor.x, this.unlitColor.y, this.unlitColor.z, this.unlitColor.w),
            });
        }
    }
}
#endif
