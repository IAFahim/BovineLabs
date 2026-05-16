// <copyright file="VolumeColorLookupClipData.cs" company="BovineLabs">
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
    using UnityEngine;

    /// <summary>
    /// Differentiates color lookup clip behaviour.
    /// </summary>
    public enum VolumeColorLookupClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for color lookup clips.
    /// </summary>
    public struct VolumeColorLookupClipData : IComponentData
    {
        public BlobAssetReference<VolumeColorLookupClipBlob> Value;
        public UnityObjectRef<Texture> Texture;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeColorLookupClipBlob
    {
        [FieldOffset(0)]
        public VolumeColorLookupClipType Type;

        [FieldOffset(4)]
        public VolumeColorLookupConstantData Constant;
    }

    /// <summary>
    /// Constant color lookup overrides applied while the clip is active.
    /// </summary>
    public struct VolumeColorLookupConstantData
    {
        public float Contribution;
        public bool Active;
        public bool TextureOverride;
        public bool ContributionOverride;
    }

    /// <summary>
    /// Blended values for color lookup animation.
    /// </summary>
    public struct VolumeColorLookupBlend
    {
        public float Contribution;
        public bool ContributionOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for color lookup animation.
    /// </summary>
    public struct VolumeColorLookupAnimated : IAnimatedComponent<VolumeColorLookupBlend>
    {
        /// <inheritdoc />
        public VolumeColorLookupBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial color lookup values when the track activates.
    /// </summary>
    public struct VolumeColorLookupInitial : IInitial<VolumeColorLookup>
    {
        /// <inheritdoc />
        public VolumeColorLookup Value { get; set; }
    }
}
#endif
