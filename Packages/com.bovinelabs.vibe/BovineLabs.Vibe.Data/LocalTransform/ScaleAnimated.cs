// <copyright file="ScaleAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.LocalTransform
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Runtime state used by timeline to animate scale clips.
    /// </summary>
    public struct ScaleAnimated : IAnimatedComponent<float>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public float Value { get; set; }
    }
}
