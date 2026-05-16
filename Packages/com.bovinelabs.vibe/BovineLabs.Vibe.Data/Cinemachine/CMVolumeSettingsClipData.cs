// <copyright file="CMVolumeSettingsClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;
    using UnityEngine.Rendering;

    /// <summary>
    /// Differentiates Cinemachine volume settings clip behaviour.
    /// </summary>
    public enum CMVolumeSettingsClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each Cinemachine volume settings clip.
    /// </summary>
    public struct CMVolumeSettingsClipData : IComponentData
    {
        public BlobAssetReference<CMVolumeSettingsClipBlob> Value;
        public UnityObjectRef<VolumeProfile> Profile;
    }

    /// <summary>
    /// Blob data for Cinemachine volume settings clips.
    /// </summary>
    public struct CMVolumeSettingsClipBlob
    {
        public CMVolumeSettingsClipType Type;
        public float Weight;
        public float FocusOffset;
        public CinemachineVolumeSettings.FocusTrackingMode FocusTracking;
        public Entity FocusTarget;
        public bool OverrideFocusTarget;
        public bool OverrideProfile;
    }
}
#endif
