// <copyright file="CMThirdPersonFollowClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
#if UNITY_PHYSICS
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Differentiates Cinemachine third person follow clip behaviour.
    /// </summary>
    public enum CMThirdPersonFollowClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each Cinemachine third person follow clip.
    /// </summary>
    public struct CMThirdPersonFollowClipData : IComponentData
    {
        public CMThirdPersonFollowClipType Type;
        public float3 Damping;
        public float3 ShoulderOffset;
        public float VerticalArmLength;
        public float CameraSide;
        public float CameraDistance;
        public CinemachineThirdPersonFollowDots.ObstacleSettings AvoidObstacles;
    }
}
#endif
#endif
