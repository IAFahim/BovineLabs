// <copyright file="CMOrbitFollowAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Cinemachine;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Aggregated numeric values animated by the Cinemachine orbit follow track.
    /// </summary>
    public struct CMOrbitFollowBlend
    {
        public float3 TargetOffset;
        public float Radius;
        public float3 PositionDamping;
        public float3 RotationDamping;
        public float QuaternionDamping;
        public Cinemachine3OrbitRig.Settings Orbits;
        public float HorizontalAxisValue;
        public float VerticalAxisValue;
        public float RadialAxisValue;
    }

    /// <summary>
    /// Runtime blend data stored per clip for the Cinemachine orbit follow track.
    /// </summary>
    public struct CMOrbitFollowAnimated : IAnimatedComponent<CMOrbitFollowBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMOrbitFollowBlend Value { get; set; }
    }
}
#endif
