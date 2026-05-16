// <copyright file="CMGroupFramingAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Aggregated numeric values animated by the Cinemachine group framing track.
    /// </summary>
    public struct CMGroupFramingBlend
    {
        public float FramingSize;
        public float2 CenterOffset;
        public float Damping;
        public float2 FovRange;
        public float2 DollyRange;
        public float2 OrthoSizeRange;
    }

    /// <summary>
    /// Runtime blend data stored per clip for the group framing track.
    /// </summary>
    public struct CMGroupFramingAnimated : IAnimatedComponent<CMGroupFramingBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMGroupFramingBlend Value { get; set; }
    }
}
#endif
