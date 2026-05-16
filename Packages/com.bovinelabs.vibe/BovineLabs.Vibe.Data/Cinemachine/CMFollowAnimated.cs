// <copyright file="CMFollowAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Aggregated numeric values animated by the Cinemachine follow track.
    /// </summary>
    public struct CMFollowBlend
    {
        public float3 FollowOffset;
        public float3 PositionDamping;
        public float3 RotationDamping;
        public float QuaternionDamping;
    }

    /// <summary>
    /// Runtime state stored per clip for the Cinemachine follow track.
    /// </summary>
    public struct CMFollowAnimated : IAnimatedComponent<CMFollowBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMFollowBlend Value { get; set; }
    }
}
#endif
