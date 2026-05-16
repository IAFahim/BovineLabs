// <copyright file="RotationLookAtStartClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;

    /// <summary>
    /// Restores the rotation captured when the clip began playing.
    /// </summary>
    [Serializable]
    public class RotationLookAtStartClip : RotationClipBase
    {
        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref RotationClipBlob blob)
        {
            blob.Type = RotationType.LookAtStart;
            blob.TransformOnClipActivation = false;
        }
    }
}
