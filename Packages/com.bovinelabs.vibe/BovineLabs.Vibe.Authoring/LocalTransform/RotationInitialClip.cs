// <copyright file="RotationInitialClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;

    /// <summary>
    /// Clip that snaps the bound transform rotation back to the captured track baseline.
    /// </summary>
    [Serializable]
    public class RotationInitialClip : RotationClipBase
    {
        /// <inheritdoc/>
        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref RotationClipBlob blob)
        {
            blob.Type = RotationType.Initial;
            blob.TransformOnClipActivation = false;
        }
    }
}
