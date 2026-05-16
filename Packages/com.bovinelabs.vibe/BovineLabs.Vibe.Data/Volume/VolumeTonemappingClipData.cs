// <copyright file="VolumeTonemappingClipData.cs" company="BovineLabs">
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
    /// Differentiates tonemapping clip behaviour.
    /// </summary>
    public enum VolumeTonemappingClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for tonemapping clips.
    /// </summary>
    public struct VolumeTonemappingClipData : IComponentData
    {
        public BlobAssetReference<VolumeTonemappingClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeTonemappingClipBlob
    {
        [FieldOffset(0)]
        public VolumeTonemappingClipType Type;

        [FieldOffset(4)]
        public VolumeTonemappingConstantData Constant;
    }

    /// <summary>
    /// Constant tonemapping overrides applied while the clip is active.
    /// </summary>
    public struct VolumeTonemappingConstantData
    {
        public TonemappingMode Mode;
        public NeutralRangeReductionMode NeutralHDRRangeReductionMode;
        public HDRACESPreset AcesPreset;
        public float HueShiftAmount;
        public bool DetectPaperWhite;
        public float PaperWhite;
        public bool DetectBrightnessLimits;
        public float MinNits;
        public float MaxNits;
        public bool Active;
        public bool ModeOverride;
        public bool NeutralHDRRangeReductionModeOverride;
        public bool AcesPresetOverride;
        public bool HueShiftAmountOverride;
        public bool DetectPaperWhiteOverride;
        public bool PaperWhiteOverride;
        public bool DetectBrightnessLimitsOverride;
        public bool MinNitsOverride;
        public bool MaxNitsOverride;
    }

    /// <summary>
    /// Blended values for tonemapping animation.
    /// </summary>
    public struct VolumeTonemappingBlend
    {
        public float HueShiftAmount;
        public float PaperWhite;
        public float MinNits;
        public float MaxNits;
        public bool HueShiftAmountOverride;
        public bool PaperWhiteOverride;
        public bool MinNitsOverride;
        public bool MaxNitsOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for tonemapping animation.
    /// </summary>
    public struct VolumeTonemappingAnimated : IAnimatedComponent<VolumeTonemappingBlend>
    {
        /// <inheritdoc />
        public VolumeTonemappingBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial tonemapping values when the track activates.
    /// </summary>
    public struct VolumeTonemappingInitial : IInitial<VolumeTonemapping>
    {
        /// <inheritdoc />
        public VolumeTonemapping Value { get; set; }
    }
}
#endif
