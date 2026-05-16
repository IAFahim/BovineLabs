// <copyright file="LocalTransformInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.LocalTransform
{
    using Unity.Entities;
    using Unity.Transforms;

    /// <summary>
    /// Captures the local transform present when a track activates.
    /// </summary>
    public struct LocalTransformInitial : IComponentData
    {
        /// <summary>
        /// Transform value cached from the bound entity.
        /// </summary>
        public LocalTransform Value;
    }
}
