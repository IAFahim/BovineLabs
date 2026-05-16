// <copyright file="VolumeColorAdjustmentsClipData.cs" company="BovineLabs">
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
    /// Differentiates color adjustments clip behaviour.
    /// </summary>
    public enum VolumeColorAdjustmentsClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for color adjustments clips.
    /// </summary>
    public struct VolumeColorAdjustmentsClipData : IComponentData
    {
        public BlobAssetReference<VolumeColorAdjustmentsClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeColorAdjustmentsClipBlob
    {
        [FieldOffset(0)]
        public VolumeColorAdjustmentsClipType Type;

        [FieldOffset(4)]
        public VolumeColorAdjustmentsConstantData Constant;
    }

    /// <summary>
    /// Constant color adjustment overrides applied while the clip is active.
    /// </summary>
    public struct VolumeColorAdjustmentsConstantData
    {
        public float PostExposure;
        public float Contrast;
        public float HueShift;
        public float Saturation;
        public float4 ColorFilter;
        public bool Active;
        public bool PostExposureOverride;
        public bool ContrastOverride;
        public bool ColorFilterOverride;
        public bool HueShiftOverride;
        public bool SaturationOverride;
    }

    /// <summary>
    /// Blended values for color adjustments animation.
    /// </summary>
    public struct VolumeColorAdjustmentsBlend
    {
        public float PostExposure;
        public float Contrast;
        public float HueShift;
        public float Saturation;
        public float4 ColorFilter;
        public bool PostExposureOverride;
        public bool ContrastOverride;
        public bool ColorFilterOverride;
        public bool HueShiftOverride;
        public bool SaturationOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for color adjustments animation.
    /// </summary>
    public struct VolumeColorAdjustmentsAnimated : IAnimatedComponent<VolumeColorAdjustmentsBlend>
    {
        /// <inheritdoc />
        public VolumeColorAdjustmentsBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial color adjustments when the track activates.
    /// </summary>
    public struct VolumeColorAdjustmentsInitial : IInitial<VolumeColorAdjustments>
    {
        /// <inheritdoc />
        public VolumeColorAdjustments Value { get; set; }
    }
}
#endif
