// <copyright file="CMCameraOffsetAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Aggregated numeric values animated by the Cinemachine camera offset track.
    /// </summary>
    public struct CMCameraOffsetBlend
    {
        public float3 Offset;
    }

    /// <summary>
    /// Runtime blend data stored per clip for the Cinemachine camera offset track.
    /// </summary>
    public struct CMCameraOffsetAnimated : IAnimatedComponent<CMCameraOffsetBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMCameraOffsetBlend Value { get; set; }
    }
}
#endif
