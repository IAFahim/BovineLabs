// <copyright file="VolumeVignetteClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Volume
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Volume;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that applies constant vignette overrides.
    /// </summary>
    [Serializable]
    public class VolumeVignetteClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the vignette override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override vignette color while the clip is active.")]
        private bool overrideColor;

        [SerializeField]
        [Tooltip("Vignette color.")]
        private Color color = Color.black;

        [SerializeField]
        [Tooltip("Override vignette center while the clip is active.")]
        private bool overrideCenter;

        [SerializeField]
        [Tooltip("Vignette center value.")]
        private Vector2 center = new Vector2(0.5f, 0.5f);

        [SerializeField]
        [Tooltip("Override vignette intensity while the clip is active.")]
        private bool overrideIntensity = true;

        [SerializeField]
        [Tooltip("Vignette intensity value.")]
        private float intensity;

        [SerializeField]
        [Tooltip("Override vignette smoothness while the clip is active.")]
        private bool overrideSmoothness;

        [SerializeField]
        [Tooltip("Vignette smoothness value.")]
        private float smoothness = 0.2f;

        [SerializeField]
        [Tooltip("Override rounded while the clip is active.")]
        private bool overrideRounded;

        [SerializeField]
        [Tooltip("Whether the vignette is rounded.")]
        private bool rounded;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeVignetteAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeVignetteClipBlob>();
            blob.Type = VolumeVignetteClipType.Constant;
            blob.Constant = new VolumeVignetteConstantData
            {
                Active = this.active,
                ColorOverride = this.overrideColor,
                Color = new float4(this.color.r, this.color.g, this.color.b, this.color.a),
                CenterOverride = this.overrideCenter,
                Center = new float2(this.center.x, this.center.y),
                IntensityOverride = this.overrideIntensity,
                Intensity = this.intensity,
                SmoothnessOverride = this.overrideSmoothness,
                Smoothness = this.smoothness,
                RoundedOverride = this.overrideRounded,
                Rounded = this.rounded,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeVignetteClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeVignetteClipData { Value = blobRef });
        }
    }
}
#endif
#endif
