// <copyright file="CMHardLockToTargetClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Entities;

    /// <summary>
    /// Differentiates hard-lock-to-target clip behaviour.
    /// </summary>
    public enum CMHardLockToTargetClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each hard-lock-to-target clip.
    /// </summary>
    public struct CMHardLockToTargetClipData : IComponentData
    {
        public CMHardLockToTargetClipType Type;
        public float Damping;
    }
}
#endif
