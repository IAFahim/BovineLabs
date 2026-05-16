// <copyright file="NonUniformScaleInitialClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.NonUniformScale
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.NonUniformScale;
    using Unity.Entities;

    /// <summary>
    /// Clip that restores the bound transform's non-uniform scale to the track baseline.
    /// </summary>
    [Serializable]
    public class NonUniformScaleInitialClip : NonUniformScaleClipBase
    {
        /// <inheritdoc/>
        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref NonUniformScaleClipBlob blob)
        {
            blob.Type = NonUniformScaleType.Initial;
            blob.SquashStretch.TransformOnClipActivation = false;
        }
    }
}
