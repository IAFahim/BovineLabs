// <copyright file="VolumeVignetteClipData.cs" company="BovineLabs">
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

    /// <summary>
    /// Differentiates vignette clip behaviour.
    /// </summary>
    public enum VolumeVignetteClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for vignette clips.
    /// </summary>
    public struct VolumeVignetteClipData : IComponentData
    {
        public BlobAssetReference<VolumeVignetteClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeVignetteClipBlob
    {
        [FieldOffset(0)]
        public VolumeVignetteClipType Type;

        [FieldOffset(4)]
        public VolumeVignetteConstantData Constant;
    }

    /// <summary>
    /// Constant vignette overrides applied while the clip is active.
    /// </summary>
    public struct VolumeVignetteConstantData
    {
        public float4 Color;
        public float2 Center;
        public float Intensity;
        public float Smoothness;
        public bool Rounded;
        public bool Active;
        public bool ColorOverride;
        public bool CenterOverride;
        public bool IntensityOverride;
        public bool SmoothnessOverride;
        public bool RoundedOverride;
    }

    /// <summary>
    /// Blended values for vignette animation.
    /// </summary>
    public struct VolumeVignetteBlend
    {
        public float4 Color;
        public float2 Center;
        public float Intensity;
        public float Smoothness;
        public bool ColorOverride;
        public bool CenterOverride;
        public bool IntensityOverride;
        public bool SmoothnessOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for vignette animation.
    /// </summary>
    public struct VolumeVignetteAnimated : IAnimatedComponent<VolumeVignetteBlend>
    {
        /// <inheritdoc />
        public VolumeVignetteBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial vignette values when the track activates.
    /// </summary>
    public struct VolumeVignetteInitial : IInitial<VolumeVignette>
    {
        /// <inheritdoc />
        public VolumeVignette Value { get; set; }
    }
}
#endif
