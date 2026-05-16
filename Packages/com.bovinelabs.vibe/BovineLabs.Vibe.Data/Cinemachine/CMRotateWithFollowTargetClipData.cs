// <copyright file="CMRotateWithFollowTargetClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Entities;

    /// <summary>
    /// Differentiates rotate-with-follow-target clip behaviour.
    /// </summary>
    public enum CMRotateWithFollowTargetClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each rotate-with-follow-target clip.
    /// </summary>
    public struct CMRotateWithFollowTargetClipData : IComponentData
    {
        public CMRotateWithFollowTargetClipType Type;
        public float Damping;
    }
}
#endif
