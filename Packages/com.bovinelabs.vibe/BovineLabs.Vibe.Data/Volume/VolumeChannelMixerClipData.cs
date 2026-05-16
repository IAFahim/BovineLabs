// <copyright file="VolumeChannelMixerClipData.cs" company="BovineLabs">
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
    /// Differentiates volume channel mixer clip behaviour.
    /// </summary>
    public enum VolumeChannelMixerClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for channel mixer clips.
    /// </summary>
    public struct VolumeChannelMixerClipData : IComponentData
    {
        public BlobAssetReference<VolumeChannelMixerClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeChannelMixerClipBlob
    {
        [FieldOffset(0)]
        public VolumeChannelMixerClipType Type;

        [FieldOffset(4)]
        public VolumeChannelMixerConstantData Constant;
    }

    /// <summary>
    /// Constant channel mixer overrides applied while the clip is active.
    /// </summary>
    public struct VolumeChannelMixerConstantData
    {
        public float RedOutRedIn;
        public float RedOutGreenIn;
        public float RedOutBlueIn;
        public float GreenOutRedIn;
        public float GreenOutGreenIn;
        public float GreenOutBlueIn;
        public float BlueOutRedIn;
        public float BlueOutGreenIn;
        public float BlueOutBlueIn;
        public bool Active;
        public bool RedOutRedInOverride;
        public bool RedOutGreenInOverride;
        public bool RedOutBlueInOverride;
        public bool GreenOutRedInOverride;
        public bool GreenOutGreenInOverride;
        public bool GreenOutBlueInOverride;
        public bool BlueOutRedInOverride;
        public bool BlueOutGreenInOverride;
        public bool BlueOutBlueInOverride;
    }

    /// <summary>
    /// Blended values for channel mixer animation.
    /// </summary>
    public struct VolumeChannelMixerBlend
    {
        public float RedOutRedIn;
        public float RedOutGreenIn;
        public float RedOutBlueIn;
        public float GreenOutRedIn;
        public float GreenOutGreenIn;
        public float GreenOutBlueIn;
        public float BlueOutRedIn;
        public float BlueOutGreenIn;
        public float BlueOutBlueIn;
        public bool RedOutRedInOverride;
        public bool RedOutGreenInOverride;
        public bool RedOutBlueInOverride;
        public bool GreenOutRedInOverride;
        public bool GreenOutGreenInOverride;
        public bool GreenOutBlueInOverride;
        public bool BlueOutRedInOverride;
        public bool BlueOutGreenInOverride;
        public bool BlueOutBlueInOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for channel mixer animation.
    /// </summary>
    public struct VolumeChannelMixerAnimated : IAnimatedComponent<VolumeChannelMixerBlend>
    {
        /// <inheritdoc />
        public VolumeChannelMixerBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial channel mixer values when the track activates.
    /// </summary>
    public struct VolumeChannelMixerInitial : IInitial<VolumeChannelMixer>
    {
        /// <inheritdoc />
        public VolumeChannelMixer Value { get; set; }
    }
}
#endif
