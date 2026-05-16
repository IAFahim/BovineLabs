// <copyright file="CMFollowClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Cinemachine.TargetTracking;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Differentiates follow clip behaviour.
    /// </summary>
    public enum CMFollowClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each Cinemachine follow clip.
    /// </summary>
    public struct CMFollowClipData : IComponentData
    {
        public CMFollowClipType Type;
        public float3 FollowOffset;
        public TrackerSettings TrackerSettings;
    }
}
#endif
