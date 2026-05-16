// <copyright file="LightAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Light
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Aggregated values animated by the light timeline track.
    /// </summary>
    public struct LightBlend
    {
        public float3 Color;
        public float Intensity;
        public float ColorTemperature;
        public bool OverrideColor;
        public bool OverrideIntensity;
        public bool OverrideColorTemperature;
    }

    /// <summary>
    /// Runtime state stored per clip for light animation.
    /// </summary>
    public struct LightAnimated : IAnimatedComponent<LightBlend>
    {
        /// <summary>
        /// Reference intensity captured when the clip activated so flicker can be applied relatively.
        /// </summary>
        public float ReferenceIntensity;

        /// <summary>
        /// Reference RGB color captured when the clip activated.
        /// </summary>
        public float3 ReferenceColor;

        /// <inheritdoc/>
        [CreateProperty]
        public LightBlend Value { get; set; }
    }
}
#endif
