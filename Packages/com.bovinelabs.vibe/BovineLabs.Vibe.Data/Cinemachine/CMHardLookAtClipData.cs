// <copyright file="CMHardLookAtClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Differentiates hard-look-at clip behaviour.
    /// </summary>
    public enum CMHardLookAtClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each hard-look-at clip.
    /// </summary>
    public struct CMHardLookAtClipData : IComponentData
    {
        public CMHardLookAtClipType Type;
        public float3 LookAtOffset;
    }
}
#endif
