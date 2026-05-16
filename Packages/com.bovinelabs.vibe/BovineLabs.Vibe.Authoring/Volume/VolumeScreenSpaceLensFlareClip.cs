// <copyright file="VolumeScreenSpaceLensFlareClip.cs" company="BovineLabs">
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
    using UnityEngine.Rendering.Universal;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that applies constant screen space lens flare overrides.
    /// </summary>
    [Serializable]
    public class VolumeScreenSpaceLensFlareClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the screen space lens flare override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override lens flare intensity while the clip is active.")]
        private bool overrideIntensity = true;

        [SerializeField]
        [Tooltip("Lens flare intensity value.")]
        private float intensity;

        [SerializeField]
        [Tooltip("Override tint color while the clip is active.")]
        private bool overrideTintColor = true;

        [SerializeField]
        [Tooltip("Tint color value.")]
        private Color tintColor = Color.white;

        [SerializeField]
        [Tooltip("Override bloom mip while the clip is active.")]
        private bool overrideBloomMip;

        [SerializeField]
        [Tooltip("Bloom mip value.")]
        private int bloomMip = 1;

        [SerializeField]
        [Tooltip("Override first flare intensity while the clip is active.")]
        private bool overrideFirstFlareIntensity;

        [SerializeField]
        [Tooltip("First flare intensity value.")]
        private float firstFlareIntensity = 1f;

        [SerializeField]
        [Tooltip("Override secondary flare intensity while the clip is active.")]
        private bool overrideSecondaryFlareIntensity;

        [SerializeField]
        [Tooltip("Secondary flare intensity value.")]
        private float secondaryFlareIntensity = 1f;

        [SerializeField]
        [Tooltip("Override warped flare intensity while the clip is active.")]
        private bool overrideWarpedFlareIntensity;

        [SerializeField]
        [Tooltip("Warped flare intensity value.")]
        private float warpedFlareIntensity = 1f;

        [SerializeField]
        [Tooltip("Override warped flare scale while the clip is active.")]
        private bool overrideWarpedFlareScale;

        [SerializeField]
        [Tooltip("Warped flare scale value.")]
        private Vector2 warpedFlareScale = new Vector2(1f, 1f);

        [SerializeField]
        [Tooltip("Override sample count while the clip is active.")]
        private bool overrideSamples;

        [SerializeField]
        [Tooltip("Sample count value.")]
        private int samples = 1;

        [SerializeField]
        [Tooltip("Override sample dimmer while the clip is active.")]
        private bool overrideSampleDimmer;

        [SerializeField]
        [Tooltip("Sample dimmer value.")]
        private float sampleDimmer = 0.5f;

        [SerializeField]
        [Tooltip("Override vignette effect while the clip is active.")]
        private bool overrideVignetteEffect;

        [SerializeField]
        [Tooltip("Vignette effect value.")]
        private float vignetteEffect = 1f;

        [SerializeField]
        [Tooltip("Override starting position while the clip is active.")]
        private bool overrideStartingPosition;

        [SerializeField]
        [Tooltip("Starting position value.")]
        private float startingPosition = 1.25f;

        [SerializeField]
        [Tooltip("Override scale while the clip is active.")]
        private bool overrideScale;

        [SerializeField]
        [Tooltip("Scale value.")]
        private float scale = 1.5f;

        [SerializeField]
        [Tooltip("Override streaks intensity while the clip is active.")]
        private bool overrideStreaksIntensity;

        [SerializeField]
        [Tooltip("Streaks intensity value.")]
        private float streaksIntensity;

        [SerializeField]
        [Tooltip("Override streaks length while the clip is active.")]
        private bool overrideStreaksLength;

        [SerializeField]
        [Tooltip("Streaks length value.")]
        private float streaksLength = 0.5f;

        [SerializeField]
        [Tooltip("Override streaks orientation while the clip is active.")]
        private bool overrideStreaksOrientation;

        [SerializeField]
        [Tooltip("Streaks orientation value.")]
        private float streaksOrientation;

        [SerializeField]
        [Tooltip("Override streaks threshold while the clip is active.")]
        private bool overrideStreaksThreshold;

        [SerializeField]
        [Tooltip("Streaks threshold value.")]
        private float streaksThreshold = 0.25f;

        [SerializeField]
        [Tooltip("Override resolution while the clip is active.")]
        private bool overrideResolution;

        [SerializeField]
        [Tooltip("Resolution value.")]
        private ScreenSpaceLensFlareResolution resolution = ScreenSpaceLensFlareResolution.Quarter;

        [SerializeField]
        [Tooltip("Override chromatic abberation intensity while the clip is active.")]
        private bool overrideChromaticAbberationIntensity;

        [SerializeField]
        [Tooltip("Chromatic abberation intensity value.")]
        private float chromaticAbberationIntensity = 0.5f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeScreenSpaceLensFlareAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeScreenSpaceLensFlareClipBlob>();
            blob.Type = VolumeScreenSpaceLensFlareClipType.Constant;
            blob.Constant = new VolumeScreenSpaceLensFlareConstantData
            {
                Active = this.active,
                IntensityOverride = this.overrideIntensity,
                Intensity = this.intensity,
                TintColorOverride = this.overrideTintColor,
                TintColor = new float4(this.tintColor.r, this.tintColor.g, this.tintColor.b, this.tintColor.a),
                BloomMipOverride = this.overrideBloomMip,
                BloomMip = this.bloomMip,
                FirstFlareIntensityOverride = this.overrideFirstFlareIntensity,
                FirstFlareIntensity = this.firstFlareIntensity,
                SecondaryFlareIntensityOverride = this.overrideSecondaryFlareIntensity,
                SecondaryFlareIntensity = this.secondaryFlareIntensity,
                WarpedFlareIntensityOverride = this.overrideWarpedFlareIntensity,
                WarpedFlareIntensity = this.warpedFlareIntensity,
                WarpedFlareScaleOverride = this.overrideWarpedFlareScale,
                WarpedFlareScale = new float2(this.warpedFlareScale.x, this.warpedFlareScale.y),
                SamplesOverride = this.overrideSamples,
                Samples = this.samples,
                SampleDimmerOverride = this.overrideSampleDimmer,
                SampleDimmer = this.sampleDimmer,
                VignetteEffectOverride = this.overrideVignetteEffect,
                VignetteEffect = this.vignetteEffect,
                StartingPositionOverride = this.overrideStartingPosition,
                StartingPosition = this.startingPosition,
                ScaleOverride = this.overrideScale,
                Scale = this.scale,
                StreaksIntensityOverride = this.overrideStreaksIntensity,
                StreaksIntensity = this.streaksIntensity,
                StreaksLengthOverride = this.overrideStreaksLength,
                StreaksLength = this.streaksLength,
                StreaksOrientationOverride = this.overrideStreaksOrientation,
                StreaksOrientation = this.streaksOrientation,
                StreaksThresholdOverride = this.overrideStreaksThreshold,
                StreaksThreshold = this.streaksThreshold,
                ResolutionOverride = this.overrideResolution,
                Resolution = this.resolution,
                ChromaticAbberationIntensityOverride = this.overrideChromaticAbberationIntensity,
                ChromaticAbberationIntensity = this.chromaticAbberationIntensity,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeScreenSpaceLensFlareClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeScreenSpaceLensFlareClipData { Value = blobRef });
        }
    }
}
#endif
#endif
