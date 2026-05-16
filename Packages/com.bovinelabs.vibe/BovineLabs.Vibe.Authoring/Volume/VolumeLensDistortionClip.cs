// <copyright file="VolumeLensDistortionClip.cs" company="BovineLabs">
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
    /// Timeline clip that applies constant lens distortion overrides.
    /// </summary>
    [Serializable]
    public class VolumeLensDistortionClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the lens distortion override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override lens distortion intensity while the clip is active.")]
        private bool overrideIntensity = true;

        [SerializeField]
        [Tooltip("Lens distortion intensity value.")]
        private float intensity;

        [SerializeField]
        [Tooltip("Override x multiplier while the clip is active.")]
        private bool overrideXMultiplier;

        [SerializeField]
        [Tooltip("X multiplier value.")]
        private float xMultiplier = 1f;

        [SerializeField]
        [Tooltip("Override y multiplier while the clip is active.")]
        private bool overrideYMultiplier;

        [SerializeField]
        [Tooltip("Y multiplier value.")]
        private float yMultiplier = 1f;

        [SerializeField]
        [Tooltip("Override center while the clip is active.")]
        private bool overrideCenter;

        [SerializeField]
        [Tooltip("Center offset value.")]
        private Vector2 center;

        [SerializeField]
        [Tooltip("Override scale while the clip is active.")]
        private bool overrideScale;

        [SerializeField]
        [Tooltip("Scale value.")]
        private float scale = 1f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeLensDistortionAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeLensDistortionClipBlob>();
            blob.Type = VolumeLensDistortionClipType.Constant;

            blob.Constant = new VolumeLensDistortionConstantData
            {
                Active = this.active,
                IntensityOverride = this.overrideIntensity,
                Intensity = this.intensity,
                XMultiplierOverride = this.overrideXMultiplier,
                XMultiplier = this.xMultiplier,
                YMultiplierOverride = this.overrideYMultiplier,
                YMultiplier = this.yMultiplier,
                CenterOverride = this.overrideCenter,
                Center = new float2(this.center.x, this.center.y),
                ScaleOverride = this.overrideScale,
                Scale = this.scale,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeLensDistortionClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeLensDistortionClipData { Value = blobRef });
        }
    }
}
#endif
#endif
