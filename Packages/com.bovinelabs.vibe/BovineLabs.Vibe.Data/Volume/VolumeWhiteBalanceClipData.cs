// <copyright file="VolumeWhiteBalanceClipData.cs" company="BovineLabs">
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

    /// <summary>
    /// Differentiates white balance clip behaviour.
    /// </summary>
    public enum VolumeWhiteBalanceClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for white balance clips.
    /// </summary>
    public struct VolumeWhiteBalanceClipData : IComponentData
    {
        public BlobAssetReference<VolumeWhiteBalanceClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeWhiteBalanceClipBlob
    {
        [FieldOffset(0)]
        public VolumeWhiteBalanceClipType Type;

        [FieldOffset(4)]
        public VolumeWhiteBalanceConstantData Constant;
    }

    /// <summary>
    /// Constant white balance overrides applied while the clip is active.
    /// </summary>
    public struct VolumeWhiteBalanceConstantData
    {
        public float Temperature;
        public float Tint;
        public bool Active;
        public bool TemperatureOverride;
        public bool TintOverride;
    }

    /// <summary>
    /// Blended values for white balance animation.
    /// </summary>
    public struct VolumeWhiteBalanceBlend
    {
        public float Temperature;
        public float Tint;
        public bool TemperatureOverride;
        public bool TintOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for white balance animation.
    /// </summary>
    public struct VolumeWhiteBalanceAnimated : IAnimatedComponent<VolumeWhiteBalanceBlend>
    {
        /// <inheritdoc />
        public VolumeWhiteBalanceBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial white balance values when the track activates.
    /// </summary>
    public struct VolumeWhiteBalanceInitial : IInitial<VolumeWhiteBalance>
    {
        /// <inheritdoc />
        public VolumeWhiteBalance Value { get; set; }
    }
}
#endif
