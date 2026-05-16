// <copyright file="PositionInitialClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;

    /// <summary>
    /// Clip that restores the bound transform position to the track's captured initial value.
    /// </summary>
    [Serializable]
    public class PositionInitialClip : PositionClipBase
    {
        /// <inheritdoc/>
        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PositionClipBlob blob)
        {
            blob.Type = PositionType.Initial;
            blob.TransformOnClipActivation = false;
        }
    }
}
