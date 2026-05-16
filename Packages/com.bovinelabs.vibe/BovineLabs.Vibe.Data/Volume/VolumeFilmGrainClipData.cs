// <copyright file="VolumeFilmGrainClipData.cs" company="BovineLabs">
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
    using UnityEngine.Rendering.Universal;

    /// <summary>
    /// Differentiates film grain clip behaviour.
    /// </summary>
    public enum VolumeFilmGrainClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for film grain clips.
    /// </summary>
    public struct VolumeFilmGrainClipData : IComponentData
    {
        public BlobAssetReference<VolumeFilmGrainClipBlob> Value;
        public UnityObjectRef<Texture> Texture;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeFilmGrainClipBlob
    {
        [FieldOffset(0)]
        public VolumeFilmGrainClipType Type;

        [FieldOffset(4)]
        public VolumeFilmGrainConstantData Constant;
    }

    /// <summary>
    /// Constant film grain overrides applied while the clip is active.
    /// </summary>
    public struct VolumeFilmGrainConstantData
    {
        public FilmGrainLookup Type;
        public float Intensity;
        public float Response;
        public bool Active;
        public bool TypeOverride;
        public bool IntensityOverride;
        public bool ResponseOverride;
        public bool TextureOverride;
    }

    /// <summary>
    /// Blended values for film grain animation.
    /// </summary>
    public struct VolumeFilmGrainBlend
    {
        public float Intensity;
        public float Response;
        public bool IntensityOverride;
        public bool ResponseOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for film grain animation.
    /// </summary>
    public struct VolumeFilmGrainAnimated : IAnimatedComponent<VolumeFilmGrainBlend>
    {
        /// <inheritdoc />
        public VolumeFilmGrainBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial film grain values when the track activates.
    /// </summary>
    public struct VolumeFilmGrainInitial : IInitial<VolumeFilmGrain>
    {
        /// <inheritdoc />
        public VolumeFilmGrain Value { get; set; }
    }
}
#endif
