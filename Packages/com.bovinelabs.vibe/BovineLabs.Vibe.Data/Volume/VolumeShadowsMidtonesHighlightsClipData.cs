// <copyright file="VolumeShadowsMidtonesHighlightsClipData.cs" company="BovineLabs">
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
    /// Differentiates shadows/midtones/highlights clip behaviour.
    /// </summary>
    public enum VolumeShadowsMidtonesHighlightsClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for shadows/midtones/highlights clips.
    /// </summary>
    public struct VolumeShadowsMidtonesHighlightsClipData : IComponentData
    {
        public BlobAssetReference<VolumeShadowsMidtonesHighlightsClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeShadowsMidtonesHighlightsClipBlob
    {
        [FieldOffset(0)]
        public VolumeShadowsMidtonesHighlightsClipType Type;

        [FieldOffset(4)]
        public VolumeShadowsMidtonesHighlightsConstantData Constant;
    }

    /// <summary>
    /// Constant shadows/midtones/highlights overrides applied while the clip is active.
    /// </summary>
    public struct VolumeShadowsMidtonesHighlightsConstantData
    {
        public float4 Shadows;
        public float4 Midtones;
        public float4 Highlights;
        public float ShadowsStart;
        public float ShadowsEnd;
        public float HighlightsStart;
        public float HighlightsEnd;
        public bool Active;
        public bool ShadowsOverride;
        public bool MidtonesOverride;
        public bool HighlightsOverride;
        public bool ShadowsStartOverride;
        public bool ShadowsEndOverride;
        public bool HighlightsStartOverride;
        public bool HighlightsEndOverride;
    }

    /// <summary>
    /// Blended values for shadows/midtones/highlights animation.
    /// </summary>
    public struct VolumeShadowsMidtonesHighlightsBlend
    {
        public float4 Shadows;
        public float4 Midtones;
        public float4 Highlights;
        public float ShadowsStart;
        public float ShadowsEnd;
        public float HighlightsStart;
        public float HighlightsEnd;
        public bool ShadowsOverride;
        public bool MidtonesOverride;
        public bool HighlightsOverride;
        public bool ShadowsStartOverride;
        public bool ShadowsEndOverride;
        public bool HighlightsStartOverride;
        public bool HighlightsEndOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for shadows/midtones/highlights animation.
    /// </summary>
    public struct VolumeShadowsMidtonesHighlightsAnimated : IAnimatedComponent<VolumeShadowsMidtonesHighlightsBlend>
    {
        /// <inheritdoc />
        public VolumeShadowsMidtonesHighlightsBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial shadows/midtones/highlights values when the track activates.
    /// </summary>
    public struct VolumeShadowsMidtonesHighlightsInitial : IInitial<VolumeShadowsMidtonesHighlights>
    {
        /// <inheritdoc />
        public VolumeShadowsMidtonesHighlights Value { get; set; }
    }
}
#endif
