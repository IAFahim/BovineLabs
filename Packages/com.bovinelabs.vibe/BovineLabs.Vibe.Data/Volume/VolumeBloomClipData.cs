// <copyright file="VolumeBloomClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Volume
{
    using System.Runtime.InteropServices;
    using BovineLabs.Bridge.Data.Volume;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    /// <summary>
    /// Differentiates volume bloom clip behaviour.
    /// </summary>
    public enum VolumeBloomClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for volume bloom clips.
    /// </summary>
    public struct VolumeBloomClipData : IComponentData
    {
        public BlobAssetReference<VolumeBloomClipBlob> Value;
        public UnityObjectRef<Texture> DirtTexture;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeBloomClipBlob
    {
        [FieldOffset(0)]
        public VolumeBloomClipType Type;

        [FieldOffset(4)]
        public VolumeBloomConstantData Constant;
    }

    /// <summary>
    /// Constant bloom overrides applied while the clip is active.
    /// </summary>
    public struct VolumeBloomConstantData
    {
        public float Threshold;
        public float Intensity;
        public float Scatter;
        public float Clamp;
        public float DirtIntensity;
        public float4 Tint;
        public bool Active;
        public bool ThresholdOverride;
        public bool IntensityOverride;
        public bool ScatterOverride;
        public bool ClampOverride;
        public bool TintOverride;
        public bool DirtIntensityOverride;
        public bool HighQualityFilteringOverride;
        public bool FilterOverride;
        public bool DownscaleOverride;
        public bool MaxIterationsOverride;
        public bool DirtTextureOverride;
        public bool HighQualityFiltering;
        public BloomFilterMode Filter;
        public BloomDownscaleMode Downscale;
        public int MaxIterations;
    }

    /// <summary>
    /// Blended values for bloom animation.
    /// </summary>
    public struct VolumeBloomBlend
    {
        public float Threshold;
        public float Intensity;
        public float Scatter;
        public float Clamp;
        public float DirtIntensity;
        public float4 Tint;
        public bool ThresholdOverride;
        public bool IntensityOverride;
        public bool ScatterOverride;
        public bool ClampOverride;
        public bool TintOverride;
        public bool DirtIntensityOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for bloom animation.
    /// </summary>
    public struct VolumeBloomAnimated : IAnimatedComponent<VolumeBloomBlend>
    {
        /// <inheritdoc />
        public VolumeBloomBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial bloom settings when the track activates.
    /// </summary>
    public struct VolumeBloomInitial : IInitial<VolumeBloom>
    {
        /// <inheritdoc />
        public VolumeBloom Value { get; set; }
    }
}
#endif
