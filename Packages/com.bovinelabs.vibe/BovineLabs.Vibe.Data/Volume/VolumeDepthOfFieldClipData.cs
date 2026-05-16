// <copyright file="VolumeDepthOfFieldClipData.cs" company="BovineLabs">
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
    /// Differentiates depth of field clip behaviour.
    /// </summary>
    public enum VolumeDepthOfFieldClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for depth of field clips.
    /// </summary>
    public struct VolumeDepthOfFieldClipData : IComponentData
    {
        public BlobAssetReference<VolumeDepthOfFieldClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumeDepthOfFieldClipBlob
    {
        [FieldOffset(0)]
        public VolumeDepthOfFieldClipType Type;

        [FieldOffset(4)]
        public VolumeDepthOfFieldConstantData Constant;
    }

    /// <summary>
    /// Constant depth of field overrides applied while the clip is active.
    /// </summary>
    public struct VolumeDepthOfFieldConstantData
    {
        public DepthOfFieldMode Mode;
        public float GaussianStart;
        public float GaussianEnd;
        public float GaussianMaxRadius;
        public bool HighQualitySampling;
        public float FocusDistance;
        public float Aperture;
        public float FocalLength;
        public int BladeCount;
        public float BladeCurvature;
        public float BladeRotation;
        public bool Active;
        public bool ModeOverride;
        public bool GaussianStartOverride;
        public bool GaussianEndOverride;
        public bool GaussianMaxRadiusOverride;
        public bool HighQualitySamplingOverride;
        public bool FocusDistanceOverride;
        public bool ApertureOverride;
        public bool FocalLengthOverride;
        public bool BladeCountOverride;
        public bool BladeCurvatureOverride;
        public bool BladeRotationOverride;
    }

    /// <summary>
    /// Blended values for depth of field animation.
    /// </summary>
    public struct VolumeDepthOfFieldBlend
    {
        public float GaussianStart;
        public float GaussianEnd;
        public float GaussianMaxRadius;
        public float FocusDistance;
        public float Aperture;
        public float FocalLength;
        public float BladeCurvature;
        public float BladeRotation;
        public bool GaussianStartOverride;
        public bool GaussianEndOverride;
        public bool GaussianMaxRadiusOverride;
        public bool FocusDistanceOverride;
        public bool ApertureOverride;
        public bool FocalLengthOverride;
        public bool BladeCurvatureOverride;
        public bool BladeRotationOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for depth of field animation.
    /// </summary>
    public struct VolumeDepthOfFieldAnimated : IAnimatedComponent<VolumeDepthOfFieldBlend>
    {
        /// <inheritdoc />
        public VolumeDepthOfFieldBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial depth of field values when the track activates.
    /// </summary>
    public struct VolumeDepthOfFieldInitial : IInitial<VolumeDepthOfField>
    {
        /// <inheritdoc />
        public VolumeDepthOfField Value { get; set; }
    }
}
#endif
