// <copyright file="URPMaterialPropertyClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
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
    /// Timeline clip that sets URP material properties.
    /// </summary>
    [Serializable]
    public class URPMaterialPropertyClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("URP material properties to override.")]
        private URPMaterialPropertyFlags properties;

        [SerializeField]
        [Tooltip("Bump scale value.")]
        private float bumpScale;

        [SerializeField]
        [Tooltip("Cutoff value.")]
        private float cutoff;

        [SerializeField]
        [Tooltip("Metallic value.")]
        private float metallic;

        [SerializeField]
        [Tooltip("Occlusion strength value.")]
        private float occlusionStrength;

        [SerializeField]
        [Tooltip("Smoothness value.")]
        private float smoothness;

        [SerializeField]
        [Tooltip("Base color value.")]
        private Color baseColor = Color.white;

        [SerializeField]
        [Tooltip("Emission color value.")]
        private Color emissionColor = Color.white;

        [SerializeField]
        [Tooltip("Spec color value.")]
        private Color specColor = Color.white;

        internal URPMaterialPropertyFlags Properties => this.properties;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            var baseLinear = this.baseColor.linear;
            var emissionLinear = this.emissionColor.linear;
            var specLinear = this.specColor.linear;

            context.Baker.AddComponent<URPMaterialPropertyAnimated>(clipEntity);
            context.Baker.AddComponent(clipEntity, new URPMaterialPropertyClipData
            {
                Flags = this.properties,
                BumpScale = this.bumpScale,
                Cutoff = this.cutoff,
                Metallic = this.metallic,
                OcclusionStrength = this.occlusionStrength,
                Smoothness = this.smoothness,
                BaseColor = new float4(baseLinear.r, baseLinear.g, baseLinear.b, baseLinear.a),
                EmissionColor = new float4(emissionLinear.r, emissionLinear.g, emissionLinear.b, emissionLinear.a),
                SpecColor = new float4(specLinear.r, specLinear.g, specLinear.b, specLinear.a),
            });
        }
    }
}
#endif
