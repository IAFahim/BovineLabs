// <copyright file="VolumeMotionBlurClipData.cs" company="BovineLabs">
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
    using UnityEngine.Rendering.Universal;

    /// <summary>
    /// Differentiates motion blur clip behaviour.
    /// </summary>
    public enum VolumeMotionBlurClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for motion blur clips.
    /// </summary>
    public struct VolumeMotionBlurClipData : IComponentData
    {
        public BlobAssetReference<VolumeMotionBlurClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeMotionBlurClipBlob
    {
        [FieldOffset(0)]
        public VolumeMotionBlurClipType Type;

        [FieldOffset(4)]
        public VolumeMotionBlurConstantData Constant;
    }

    /// <summary>
    /// Constant motion blur overrides applied while the clip is active.
    /// </summary>
    public struct VolumeMotionBlurConstantData
    {
        public MotionBlurMode Mode;
        public MotionBlurQuality Quality;
        public float Intensity;
        public float Clamp;
        public bool Active;
        public bool ModeOverride;
        public bool QualityOverride;
        public bool IntensityOverride;
        public bool ClampOverride;
    }

    /// <summary>
    /// Blended values for motion blur animation.
    /// </summary>
    public struct VolumeMotionBlurBlend
    {
        public float Intensity;
        public float Clamp;
        public bool IntensityOverride;
        public bool ClampOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for motion blur animation.
    /// </summary>
    public struct VolumeMotionBlurAnimated : IAnimatedComponent<VolumeMotionBlurBlend>
    {
        /// <inheritdoc />
        public VolumeMotionBlurBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial motion blur values when the track activates.
    /// </summary>
    public struct VolumeMotionBlurInitial : IInitial<VolumeMotionBlur>
    {
        /// <inheritdoc />
        public VolumeMotionBlur Value { get; set; }
    }
}
#endif
