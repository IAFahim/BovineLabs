// <copyright file="ScaleInitialClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;

    /// <summary>
    /// Clip that restores the bound transform scale to the track baseline.
    /// </summary>
    [Serializable]
    public class ScaleInitialClip : ScaleClipBase
    {
        /// <inheritdoc/>
        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref ScaleClipBlob blob)
        {
            blob.Type = ScaleType.Initial;
        }
    }
}
