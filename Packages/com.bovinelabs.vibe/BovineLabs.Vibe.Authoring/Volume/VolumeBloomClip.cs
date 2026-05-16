// <copyright file="VolumeBloomClip.cs" company="BovineLabs">
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
    /// Timeline clip that applies constant bloom overrides.
    /// </summary>
    [Serializable]
    public class VolumeBloomClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the bloom override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override the bloom threshold while the clip is active.")]
        private bool overrideThreshold = true;

        [SerializeField]
        [Tooltip("Bloom threshold value.")]
        private float threshold = 1f;

        [SerializeField]
        [Tooltip("Override the bloom intensity while the clip is active.")]
        private bool overrideIntensity = true;

        [SerializeField]
        [Tooltip("Bloom intensity value.")]
        private float intensity = 0.8f;

        [SerializeField]
        [Tooltip("Override the bloom scatter while the clip is active.")]
        private bool overrideScatter;

        [SerializeField]
        [Tooltip("Bloom scatter value.")]
        private float scatter = 0.7f;

        [SerializeField]
        [Tooltip("Override the bloom clamp while the clip is active.")]
        private bool overrideClamp;

        [SerializeField]
        [Tooltip("Bloom clamp value.")]
        private float clamp = 65472f;

        [SerializeField]
        [Tooltip("Override the bloom tint while the clip is active.")]
        private bool overrideTint;

        [SerializeField]
        [Tooltip("Bloom tint color.")]
        private Color tint = Color.white;

        [SerializeField]
        [Tooltip("Override the dirt intensity while the clip is active.")]
        private bool overrideDirtIntensity;

        [SerializeField]
        [Tooltip("Dirt intensity value.")]
        private float dirtIntensity;

        [SerializeField]
        [Tooltip("Override the dirt texture while the clip is active.")]
        private bool overrideDirtTexture;

        [SerializeField]
        [Tooltip("Dirt texture to apply when override is enabled.")]
        private Texture dirtTexture;

        [SerializeField]
        [Tooltip("Override high quality filtering while the clip is active.")]
        private bool overrideHighQualityFiltering;

        [SerializeField]
        [Tooltip("Use high quality filtering.")]
        private bool highQualityFiltering;

        [SerializeField]
        [Tooltip("Override the bloom filter mode while the clip is active.")]
        private bool overrideFilter;

        [SerializeField]
        [Tooltip("Bloom filter mode to apply when override is enabled.")]
        private BloomFilterMode filter = BloomFilterMode.Gaussian;

        [SerializeField]
        [Tooltip("Override the downscale mode while the clip is active.")]
        private bool overrideDownscale;

        [SerializeField]
        [Tooltip("Downscale mode to apply when override is enabled.")]
        private BloomDownscaleMode downscale = BloomDownscaleMode.Half;

        [SerializeField]
        [Tooltip("Override the maximum iterations while the clip is active.")]
        private bool overrideMaxIterations;

        [SerializeField]
        [Tooltip("Maximum bloom iterations.")]
        private int maxIterations = 6;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeBloomAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeBloomClipBlob>();
            blob.Type = VolumeBloomClipType.Constant;

            ref var data = ref blob.Constant;
            data.Active = this.active;
            data.ThresholdOverride = this.overrideThreshold;
            data.Threshold = this.threshold;
            data.IntensityOverride = this.overrideIntensity;
            data.Intensity = this.intensity;
            data.ScatterOverride = this.overrideScatter;
            data.Scatter = this.scatter;
            data.ClampOverride = this.overrideClamp;
            data.Clamp = this.clamp;
            data.TintOverride = this.overrideTint;
            data.Tint = new float4(this.tint.r, this.tint.g, this.tint.b, this.tint.a);
            data.DirtIntensityOverride = this.overrideDirtIntensity;
            data.DirtIntensity = this.dirtIntensity;
            data.DirtTextureOverride = this.overrideDirtTexture;
            data.HighQualityFilteringOverride = this.overrideHighQualityFiltering;
            data.HighQualityFiltering = this.highQualityFiltering;
            data.FilterOverride = this.overrideFilter;
            data.Filter = this.filter;
            data.DownscaleOverride = this.overrideDownscale;
            data.Downscale = this.downscale;
            data.MaxIterationsOverride = this.overrideMaxIterations;
            data.MaxIterations = this.maxIterations;

            var blobRef = builder.CreateBlobAssetReference<VolumeBloomClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(
                clipEntity,
                new VolumeBloomClipData
                {
                    Value = blobRef,
                    DirtTexture = this.dirtTexture,
                });
        }
    }
}
#endif
#endif
