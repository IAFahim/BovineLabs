// <copyright file="VolumeSplitToningClipData.cs" company="BovineLabs">
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
    /// Differentiates split toning clip behaviour.
    /// </summary>
    public enum VolumeSplitToningClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for split toning clips.
    /// </summary>
    public struct VolumeSplitToningClipData : IComponentData
    {
        public BlobAssetReference<VolumeSplitToningClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeSplitToningClipBlob
    {
        [FieldOffset(0)]
        public VolumeSplitToningClipType Type;

        [FieldOffset(4)]
        public VolumeSplitToningConstantData Constant;
    }

    /// <summary>
    /// Constant split toning overrides applied while the clip is active.
    /// </summary>
    public struct VolumeSplitToningConstantData
    {
        public float4 Shadows;
        public float4 Highlights;
        public float Balance;
        public bool Active;
        public bool ShadowsOverride;
        public bool HighlightsOverride;
        public bool BalanceOverride;
    }

    /// <summary>
    /// Blended values for split toning animation.
    /// </summary>
    public struct VolumeSplitToningBlend
    {
        public float4 Shadows;
        public float4 Highlights;
        public float Balance;
        public bool ShadowsOverride;
        public bool HighlightsOverride;
        public bool BalanceOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for split toning animation.
    /// </summary>
    public struct VolumeSplitToningAnimated : IAnimatedComponent<VolumeSplitToningBlend>
    {
        /// <inheritdoc />
        public VolumeSplitToningBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial split toning values when the track activates.
    /// </summary>
    public struct VolumeSplitToningInitial : IInitial<VolumeSplitToning>
    {
        /// <inheritdoc />
        public VolumeSplitToning Value { get; set; }
    }
}
#endif
