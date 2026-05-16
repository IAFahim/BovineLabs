// <copyright file="RotationAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.LocalTransform
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Runtime state used by timeline to animate rotation clips.
    /// </summary>
    public struct RotationAnimated : IAnimatedComponent<quaternion>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public quaternion Value { get; set; }
    }
}
