// <copyright file="CMPanTiltAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Aggregated numeric values animated by the Cinemachine pan/tilt track.
    /// </summary>
    public struct CMPanTiltBlend
    {
        public float PanAxisValue;
        public float TiltAxisValue;
    }

    /// <summary>
    /// Runtime blend data stored per clip for the Cinemachine pan/tilt track.
    /// </summary>
    public struct CMPanTiltAnimated : IAnimatedComponent<CMPanTiltBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMPanTiltBlend Value { get; set; }
    }
}
#endif
