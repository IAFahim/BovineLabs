// <copyright file="LightExtendedAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Light
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Aggregated values animated by the light track for extended properties.
    /// </summary>
    public struct LightExtendedBlend
    {
        public float Range;
        public float SpotAngle;
        public float InnerSpotAngle;
        public float ShadowStrength;
        public float ShadowBias;
        public float ShadowNormalBias;
        public float ShadowNearPlane;
        public float2 CookieSize;
        public bool OverrideRange;
        public bool OverrideSpotAngle;
        public bool OverrideInnerSpotAngle;
        public bool OverrideShadowStrength;
        public bool OverrideShadowBias;
        public bool OverrideShadowNormalBias;
        public bool OverrideShadowNearPlane;
        public bool OverrideCookieSize;
    }

    /// <summary>
    /// Runtime state stored per clip for extended light animation.
    /// </summary>
    public struct LightExtendedAnimated : IAnimatedComponent<LightExtendedBlend>
    {
        public float ReferenceRange;
        public float ReferenceSpotAngle;
        public float ReferenceInnerSpotAngle;
        public float ReferenceShadowStrength;
        public float ReferenceShadowBias;
        public float ReferenceShadowNormalBias;
        public float ReferenceShadowNearPlane;
        public float2 ReferenceCookieSize;

        /// <inheritdoc/>
        [CreateProperty]
        public LightExtendedBlend Value { get; set; }
    }
}
#endif
