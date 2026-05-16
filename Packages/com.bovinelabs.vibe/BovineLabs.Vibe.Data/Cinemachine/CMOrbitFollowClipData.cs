// <copyright file="CMOrbitFollowClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Cinemachine.TargetTracking;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Differentiates Cinemachine orbit follow clip behaviour.
    /// </summary>
    public enum CMOrbitFollowClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each Cinemachine orbit follow clip.
    /// </summary>
    public struct CMOrbitFollowClipData : IComponentData
    {
        public CMOrbitFollowClipType Type;
        public TrackerSettings TrackerSettings;
        public CinemachineOrbitalFollow.OrbitStyles OrbitStyle;
        public float Radius;
        public Cinemachine3OrbitRig.Settings Orbits;
        public InputAxis HorizontalAxis;
        public InputAxis VerticalAxis;
        public InputAxis RadialAxis;
        public float3 TargetOffset;
        public CinemachineOrbitalFollow.ReferenceFrames RecenteringTarget;
    }
}
#endif
