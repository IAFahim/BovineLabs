// <copyright file="CMFreeLookModifierAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Aggregated numeric values animated by the Cinemachine free look modifier track.
    /// </summary>
    public struct CMFreeLookModifierBlend
    {
        public float Easing;
    }

    /// <summary>
    /// Runtime blend data stored per clip for the Cinemachine free look modifier track.
    /// </summary>
    public struct CMFreeLookModifierAnimated : IAnimatedComponent<CMFreeLookModifierBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMFreeLookModifierBlend Value { get; set; }
    }
}
#endif
