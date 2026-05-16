// <copyright file="TimeScaleAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.Time
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Runtime time scale value used for clip blending.
    /// </summary>
    public struct TimeScaleAnimated : IAnimatedComponent<float>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public float Value { get; set; }
    }
}
