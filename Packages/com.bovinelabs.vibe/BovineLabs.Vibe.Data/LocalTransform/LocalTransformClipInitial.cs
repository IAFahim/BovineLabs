// <copyright file="LocalTransformClipInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.LocalTransform
{
    using Unity.Entities;
    using Unity.Transforms;

    /// <summary>
    /// Stores the local transform captured when a clip activates.
    /// </summary>
    public struct LocalTransformClipInitial : IComponentData
    {
        /// <summary>
        /// Transform value recorded from the binding entity.
        /// </summary>
        public LocalTransform Value;
    }
}
