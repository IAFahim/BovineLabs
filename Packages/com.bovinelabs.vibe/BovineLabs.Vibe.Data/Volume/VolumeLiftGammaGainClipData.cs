// <copyright file="VolumeLiftGammaGainClipData.cs" company="BovineLabs">
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
    /// Differentiates lift/gamma/gain clip behaviour.
    /// </summary>
    public enum VolumeLiftGammaGainClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for lift/gamma/gain clips.
    /// </summary>
    public struct VolumeLiftGammaGainClipData : IComponentData
    {
        public BlobAssetReference<VolumeLiftGammaGainClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeLiftGammaGainClipBlob
    {
        [FieldOffset(0)]
        public VolumeLiftGammaGainClipType Type;

        [FieldOffset(4)]
        public VolumeLiftGammaGainConstantData Constant;
    }

    /// <summary>
    /// Constant lift/gamma/gain overrides applied while the clip is active.
    /// </summary>
    public struct VolumeLiftGammaGainConstantData
    {
        public float4 Lift;
        public float4 Gamma;
        public float4 Gain;
        public bool Active;
        public bool LiftOverride;
        public bool GammaOverride;
        public bool GainOverride;
    }

    /// <summary>
    /// Blended values for lift/gamma/gain animation.
    /// </summary>
    public struct VolumeLiftGammaGainBlend
    {
        public float4 Lift;
        public float4 Gamma;
        public float4 Gain;
        public bool LiftOverride;
        public bool GammaOverride;
        public bool GainOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for lift/gamma/gain animation.
    /// </summary>
    public struct VolumeLiftGammaGainAnimated : IAnimatedComponent<VolumeLiftGammaGainBlend>
    {
        /// <inheritdoc />
        public VolumeLiftGammaGainBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial lift/gamma/gain values when the track activates.
    /// </summary>
    public struct VolumeLiftGammaGainInitial : IInitial<VolumeLiftGammaGain>
    {
        /// <inheritdoc />
        public VolumeLiftGammaGain Value { get; set; }
    }
}
#endif
