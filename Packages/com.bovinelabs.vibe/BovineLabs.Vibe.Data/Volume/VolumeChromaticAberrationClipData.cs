// <copyright file="VolumeChromaticAberrationClipData.cs" company="BovineLabs">
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
    /// Differentiates chromatic aberration clip behaviour.
    /// </summary>
    public enum VolumeChromaticAberrationClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for chromatic aberration clips.
    /// </summary>
    public struct VolumeChromaticAberrationClipData : IComponentData
    {
        public BlobAssetReference<VolumeChromaticAberrationClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeChromaticAberrationClipBlob
    {
        [FieldOffset(0)]
        public VolumeChromaticAberrationClipType Type;

        [FieldOffset(4)]
        public VolumeChromaticAberrationConstantData Constant;
    }

    /// <summary>
    /// Constant chromatic aberration overrides applied while the clip is active.
    /// </summary>
    public struct VolumeChromaticAberrationConstantData
    {
        public float Intensity;
        public bool Active;
        public bool IntensityOverride;
    }

    /// <summary>
    /// Blended values for chromatic aberration animation.
    /// </summary>
    public struct VolumeChromaticAberrationBlend
    {
        public float Intensity;
        public bool IntensityOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for chromatic aberration animation.
    /// </summary>
    public struct VolumeChromaticAberrationAnimated : IAnimatedComponent<VolumeChromaticAberrationBlend>
    {
        /// <inheritdoc />
        public VolumeChromaticAberrationBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial chromatic aberration values when the track activates.
    /// </summary>
    public struct VolumeChromaticAberrationInitial : IInitial<VolumeChromaticAberration>
    {
        /// <inheritdoc />
        public VolumeChromaticAberration Value { get; set; }
    }
}
#endif
