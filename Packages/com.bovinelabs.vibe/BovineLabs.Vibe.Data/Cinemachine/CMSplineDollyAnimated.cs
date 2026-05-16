// <copyright file="CMSplineDollyAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Runtime state used to animate Cinemachine spline dolly clips.
    /// </summary>
    public struct CMSplineDollyAnimated : IAnimatedComponent<float>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public float Value { get; set; }
    }
}
#endif
