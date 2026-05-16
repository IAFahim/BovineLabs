// <copyright file="CMBasicMultiChannelPerlinAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Aggregated numeric values animated by the Cinemachine basic multi channel perlin track.
    /// </summary>
    public struct CMBasicMultiChannelPerlinBlend
    {
        public float AmplitudeGain;
        public float FrequencyGain;
        public float3 PivotOffset;
    }

    /// <summary>
    /// Runtime state stored per clip for the Cinemachine basic multi channel perlin track.
    /// </summary>
    public struct CMBasicMultiChannelPerlinAnimated : IAnimatedComponent<CMBasicMultiChannelPerlinBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMBasicMultiChannelPerlinBlend Value { get; set; }
    }
}
#endif
