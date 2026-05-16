// <copyright file="VolumeScreenSpaceLensFlareClipData.cs" company="BovineLabs">
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
    using UnityEngine.Rendering.Universal;

    /// <summary>
    /// Differentiates screen space lens flare clip behaviour.
    /// </summary>
    public enum VolumeScreenSpaceLensFlareClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for screen space lens flare clips.
    /// </summary>
    public struct VolumeScreenSpaceLensFlareClipData : IComponentData
    {
        public BlobAssetReference<VolumeScreenSpaceLensFlareClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeScreenSpaceLensFlareClipBlob
    {
        [FieldOffset(0)]
        public VolumeScreenSpaceLensFlareClipType Type;

        [FieldOffset(4)]
        public VolumeScreenSpaceLensFlareConstantData Constant;
    }

    /// <summary>
    /// Constant screen space lens flare overrides applied while the clip is active.
    /// </summary>
    public struct VolumeScreenSpaceLensFlareConstantData
    {
        public float Intensity;
        public float4 TintColor;
        public int BloomMip;
        public float FirstFlareIntensity;
        public float SecondaryFlareIntensity;
        public float WarpedFlareIntensity;
        public float2 WarpedFlareScale;
        public int Samples;
        public float SampleDimmer;
        public float VignetteEffect;
        public float StartingPosition;
        public float Scale;
        public float StreaksIntensity;
        public float StreaksLength;
        public float StreaksOrientation;
        public float StreaksThreshold;
        public ScreenSpaceLensFlareResolution Resolution;
        public float ChromaticAbberationIntensity;
        public bool Active;
        public bool IntensityOverride;
        public bool TintColorOverride;
        public bool BloomMipOverride;
        public bool FirstFlareIntensityOverride;
        public bool SecondaryFlareIntensityOverride;
        public bool WarpedFlareIntensityOverride;
        public bool WarpedFlareScaleOverride;
        public bool SamplesOverride;
        public bool SampleDimmerOverride;
        public bool VignetteEffectOverride;
        public bool StartingPositionOverride;
        public bool ScaleOverride;
        public bool StreaksIntensityOverride;
        public bool StreaksLengthOverride;
        public bool StreaksOrientationOverride;
        public bool StreaksThresholdOverride;
        public bool ResolutionOverride;
        public bool ChromaticAbberationIntensityOverride;
    }

    /// <summary>
    /// Blended values for screen space lens flare animation.
    /// </summary>
    public struct VolumeScreenSpaceLensFlareBlend
    {
        public float Intensity;
        public float4 TintColor;
        public float FirstFlareIntensity;
        public float SecondaryFlareIntensity;
        public float WarpedFlareIntensity;
        public float2 WarpedFlareScale;
        public float SampleDimmer;
        public float VignetteEffect;
        public float StartingPosition;
        public float Scale;
        public float StreaksIntensity;
        public float StreaksLength;
        public float StreaksOrientation;
        public float StreaksThreshold;
        public float ChromaticAbberationIntensity;
        public bool IntensityOverride;
        public bool TintColorOverride;
        public bool FirstFlareIntensityOverride;
        public bool SecondaryFlareIntensityOverride;
        public bool WarpedFlareIntensityOverride;
        public bool WarpedFlareScaleOverride;
        public bool SampleDimmerOverride;
        public bool VignetteEffectOverride;
        public bool StartingPositionOverride;
        public bool ScaleOverride;
        public bool StreaksIntensityOverride;
        public bool StreaksLengthOverride;
        public bool StreaksOrientationOverride;
        public bool StreaksThresholdOverride;
        public bool ChromaticAbberationIntensityOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for screen space lens flare animation.
    /// </summary>
    public struct VolumeScreenSpaceLensFlareAnimated : IAnimatedComponent<VolumeScreenSpaceLensFlareBlend>
    {
        /// <inheritdoc />
        public VolumeScreenSpaceLensFlareBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial screen space lens flare values when the track activates.
    /// </summary>
    public struct VolumeScreenSpaceLensFlareInitial : IInitial<VolumeScreenSpaceLensFlare>
    {
        /// <inheritdoc />
        public VolumeScreenSpaceLensFlare Value { get; set; }
    }
}
#endif
