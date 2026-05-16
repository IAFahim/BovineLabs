// <copyright file="CMBrainClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;

    /// <summary>
    /// Differentiates Cinemachine brain clip behaviour.
    /// </summary>
    public enum CMBrainClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each Cinemachine brain clip.
    /// </summary>
    public struct CMBrainClipData : IComponentData
    {
        public BlobAssetReference<CMBrainClipBlob> Value;
    }

    /// <summary>
    /// Blob data for Cinemachine brain clips.
    /// </summary>
    public struct CMBrainClipBlob
    {
        public CMBrainClipType Type;
        public bool IgnoreTimeScale;
        public CinemachineBrain.UpdateMethods UpdateMethod;
        public CinemachineBrain.BrainUpdateMethods BlendUpdateMethod;
        public CinemachineBlendDefinition.Styles DefaultBlendStyle;
        public float DefaultBlendTime;
    }
}
#endif
