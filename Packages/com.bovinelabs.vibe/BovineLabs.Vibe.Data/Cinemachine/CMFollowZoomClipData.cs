// <copyright file="CMFollowZoomClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Differentiates Cinemachine follow zoom clip behaviour.
    /// </summary>
    public enum CMFollowZoomClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each Cinemachine follow zoom clip.
    /// </summary>
    public struct CMFollowZoomClipData : IComponentData
    {
        public CMFollowZoomClipType Type;
        public float Width;
        public float Damping;
        public float2 FovRange;
    }
}
#endif
