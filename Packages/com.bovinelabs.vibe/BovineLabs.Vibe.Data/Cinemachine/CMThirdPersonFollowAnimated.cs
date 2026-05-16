// <copyright file="CMThirdPersonFollowAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
#if UNITY_PHYSICS
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Aggregated numeric values animated by the Cinemachine third person follow track.
    /// </summary>
    public struct CMThirdPersonFollowBlend
    {
        public float3 Damping;
        public float3 ShoulderOffset;
        public float VerticalArmLength;
        public float CameraSide;
        public float CameraDistance;
    }

    /// <summary>
    /// Runtime blend data stored per clip for the Cinemachine third person follow track.
    /// </summary>
    public struct CMThirdPersonFollowAnimated : IAnimatedComponent<CMThirdPersonFollowBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMThirdPersonFollowBlend Value { get; set; }
    }
}
#endif
#endif
