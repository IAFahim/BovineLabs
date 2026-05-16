// <copyright file="PositionOffsetClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Applies an offset relative to the binding's current transform.
    /// </summary>
    [Serializable]
    public class PositionOffsetClip : PositionClipBase
    {
        [Tooltip("Offset sampled in the provided transform space.")]
        [SerializeField]
        private SpaceVector3 offset;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PositionClipBlob blob)
        {
            blob.Type = PositionType.Offset;
            blob.TransformOnClipActivation = this.offset.UseClipActivation;
            blob.Offset = new PositionClipBlob.OffsetData
            {
                Space = this.offset.Space,
                Value = this.offset.Value,
            };
        }
    }
}
