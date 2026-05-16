// <copyright file="CMCameraAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Runtime state used to animate Cinemachine camera field of view clips.
    /// </summary>
    public struct CMCameraAnimated : IAnimatedComponent<float>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public float Value { get; set; }
    }
}
#endif
