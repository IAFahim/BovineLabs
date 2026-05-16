// <copyright file="CMRecomposerClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;

    /// <summary>
    /// Differentiates Cinemachine recomposer clip behaviour.
    /// </summary>
    public enum CMRecomposerClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized parameters baked per Cinemachine recomposer clip.
    /// </summary>
    public struct CMRecomposerClipData : IComponentData
    {
        public BlobAssetReference<CMRecomposerClipBlob> Value;
    }

    /// <summary>
    /// Blob data for Cinemachine recomposer clips.
    /// </summary>
    public struct CMRecomposerClipBlob
    {
        public CMRecomposerClipType Type;
        public float Tilt;
        public float Pan;
        public float Dutch;
        public float ZoomScale;
        public float FollowAttachment;
        public float LookAtAttachment;
        public CinemachineCore.Stage ApplyAfter;
    }
}
#endif
