// <copyright file="CMFollowZoomAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Aggregated numeric values animated by the Cinemachine follow zoom track.
    /// </summary>
    public struct CMFollowZoomBlend
    {
        public float Width;
        public float Damping;
        public float2 FovRange;
    }

    /// <summary>
    /// Runtime blend data stored per clip for the Cinemachine follow zoom track.
    /// </summary>
    public struct CMFollowZoomAnimated : IAnimatedComponent<CMFollowZoomBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMFollowZoomBlend Value { get; set; }
    }
}
#endif
