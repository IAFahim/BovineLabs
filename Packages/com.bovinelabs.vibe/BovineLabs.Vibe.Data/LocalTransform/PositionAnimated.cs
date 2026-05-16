// <copyright file="PositionAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.LocalTransform
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Runtime state used by timeline to animate a position clip.
    /// </summary>
    public struct PositionAnimated : IAnimatedComponent<float3>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public float3 Value { get; set; }
    }
}
