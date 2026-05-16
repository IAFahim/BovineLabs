// <copyright file="VolumeLensDistortionClipData.cs" company="BovineLabs">
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
    /// Differentiates lens distortion clip behaviour.
    /// </summary>
    public enum VolumeLensDistortionClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for lens distortion clips.
    /// </summary>
    public struct VolumeLensDistortionClipData : IComponentData
    {
        public BlobAssetReference<VolumeLensDistortionClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeLensDistortionClipBlob
    {
        [FieldOffset(0)]
        public VolumeLensDistortionClipType Type;

        [FieldOffset(4)]
        public VolumeLensDistortionConstantData Constant;
    }

    /// <summary>
    /// Constant lens distortion overrides applied while the clip is active.
    /// </summary>
    public struct VolumeLensDistortionConstantData
    {
        public float Intensity;
        public float XMultiplier;
        public float YMultiplier;
        public float Scale;
        public float2 Center;
        public bool Active;
        public bool IntensityOverride;
        public bool XMultiplierOverride;
        public bool YMultiplierOverride;
        public bool CenterOverride;
        public bool ScaleOverride;
    }

    /// <summary>
    /// Blended values for lens distortion animation.
    /// </summary>
    public struct VolumeLensDistortionBlend
    {
        public float Intensity;
        public float XMultiplier;
        public float YMultiplier;
        public float Scale;
        public float2 Center;
        public bool IntensityOverride;
        public bool XMultiplierOverride;
        public bool YMultiplierOverride;
        public bool CenterOverride;
        public bool ScaleOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for lens distortion animation.
    /// </summary>
    public struct VolumeLensDistortionAnimated : IAnimatedComponent<VolumeLensDistortionBlend>
    {
        /// <inheritdoc />
        public VolumeLensDistortionBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial lens distortion values when the track activates.
    /// </summary>
    public struct VolumeLensDistortionInitial : IInitial<VolumeLensDistortion>
    {
        /// <inheritdoc />
        public VolumeLensDistortion Value { get; set; }
    }
}
#endif
