// <copyright file="VolumeSettingsClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Volume
{
    using BovineLabs.Bridge.Data.Volume;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using Unity.Entities;
    using UnityEngine.Rendering;

    /// <summary>
    /// Differentiates volume settings clip behaviour.
    /// </summary>
    public enum VolumeSettingsClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for volume settings clips.
    /// </summary>
    public struct VolumeSettingsClipData : IComponentData
    {
        public BlobAssetReference<VolumeSettingsClipBlob> Value;
        public UnityObjectRef<VolumeProfile> Profile;
    }

    /// <summary>
    /// Blob data for volume settings clips.
    /// </summary>
    public struct VolumeSettingsClipBlob
    {
        public VolumeSettingsClipType Type;
        public float Weight;
        public float Priority;
        public float BlendDistance;
        public bool IsGlobal;
        public bool OverridePriority;
        public bool OverrideBlendDistance;
        public bool OverrideIsGlobal;
        public bool OverrideProfile;
    }

    /// <summary>
    /// Blended values for volume settings animation.
    /// </summary>
    public struct VolumeSettingsBlend
    {
        public float Weight;
    }

    /// <summary>
    /// Runtime state stored per clip for volume settings animation.
    /// </summary>
    public struct VolumeSettingsAnimated : IAnimatedComponent<VolumeSettingsBlend>
    {
        /// <inheritdoc />
        public VolumeSettingsBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial volume settings when the track activates.
    /// </summary>
    public struct VolumeSettingsInitial : IInitial<VolumeSettings>
    {
        /// <inheritdoc />
        public VolumeSettings Value { get; set; }
    }
}
#endif
