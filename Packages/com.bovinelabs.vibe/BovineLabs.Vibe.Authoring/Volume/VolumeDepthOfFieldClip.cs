// <copyright file="VolumeDepthOfFieldClip.cs" company="BovineLabs">
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
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that applies constant depth of field overrides.
    /// </summary>
    [Serializable]
    public class VolumeDepthOfFieldClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the depth of field override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override depth of field mode while the clip is active.")]
        private bool overrideMode = true;

        [SerializeField]
        [Tooltip("Depth of field mode to apply when override is enabled.")]
        private DepthOfFieldMode mode = DepthOfFieldMode.Bokeh;

        [SerializeField]
        [Tooltip("Override gaussian start while the clip is active.")]
        private bool overrideGaussianStart;

        [SerializeField]
        [Tooltip("Gaussian start distance.")]
        private float gaussianStart = 1f;

        [SerializeField]
        [Tooltip("Override gaussian end while the clip is active.")]
        private bool overrideGaussianEnd;

        [SerializeField]
        [Tooltip("Gaussian end distance.")]
        private float gaussianEnd = 10f;

        [SerializeField]
        [Tooltip("Override gaussian max radius while the clip is active.")]
        private bool overrideGaussianMaxRadius;

        [SerializeField]
        [Tooltip("Gaussian max radius.")]
        private float gaussianMaxRadius = 1f;

        [SerializeField]
        [Tooltip("Override high quality sampling while the clip is active.")]
        private bool overrideHighQualitySampling;

        [SerializeField]
        [Tooltip("Use high quality sampling.")]
        private bool highQualitySampling;

        [SerializeField]
        [Tooltip("Override focus distance while the clip is active.")]
        private bool overrideFocusDistance;

        [SerializeField]
        [Tooltip("Focus distance value.")]
        private float focusDistance = 10f;

        [SerializeField]
        [Tooltip("Override aperture while the clip is active.")]
        private bool overrideAperture;

        [SerializeField]
        [Tooltip("Aperture value.")]
        private float aperture = 5.6f;

        [SerializeField]
        [Tooltip("Override focal length while the clip is active.")]
        private bool overrideFocalLength;

        [SerializeField]
        [Tooltip("Focal length value.")]
        private float focalLength = 50f;

        [SerializeField]
        [Tooltip("Override blade count while the clip is active.")]
        private bool overrideBladeCount;

        [SerializeField]
        [Tooltip("Blade count value.")]
        private int bladeCount = 5;

        [SerializeField]
        [Tooltip("Override blade curvature while the clip is active.")]
        private bool overrideBladeCurvature;

        [SerializeField]
        [Tooltip("Blade curvature value.")]
        private float bladeCurvature = 1f;

        [SerializeField]
        [Tooltip("Override blade rotation while the clip is active.")]
        private bool overrideBladeRotation;

        [SerializeField]
        [Tooltip("Blade rotation value.")]
        private float bladeRotation;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeDepthOfFieldAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeDepthOfFieldClipBlob>();
            blob.Type = VolumeDepthOfFieldClipType.Constant;

            blob.Constant = new VolumeDepthOfFieldConstantData
            {
                Active = this.active,
                ModeOverride = this.overrideMode,
                Mode = this.mode,
                GaussianStartOverride = this.overrideGaussianStart,
                GaussianStart = this.gaussianStart,
                GaussianEndOverride = this.overrideGaussianEnd,
                GaussianEnd = this.gaussianEnd,
                GaussianMaxRadiusOverride = this.overrideGaussianMaxRadius,
                GaussianMaxRadius = this.gaussianMaxRadius,
                HighQualitySamplingOverride = this.overrideHighQualitySampling,
                HighQualitySampling = this.highQualitySampling,
                FocusDistanceOverride = this.overrideFocusDistance,
                FocusDistance = this.focusDistance,
                ApertureOverride = this.overrideAperture,
                Aperture = this.aperture,
                FocalLengthOverride = this.overrideFocalLength,
                FocalLength = this.focalLength,
                BladeCountOverride = this.overrideBladeCount,
                BladeCount = this.bladeCount,
                BladeCurvatureOverride = this.overrideBladeCurvature,
                BladeCurvature = this.bladeCurvature,
                BladeRotationOverride = this.overrideBladeRotation,
                BladeRotation = this.bladeRotation,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeDepthOfFieldClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeDepthOfFieldClipData { Value = blobRef });
        }
    }
}
#endif
#endif
