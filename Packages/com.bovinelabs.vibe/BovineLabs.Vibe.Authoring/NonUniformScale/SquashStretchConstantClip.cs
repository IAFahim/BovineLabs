// <copyright file="SquashStretchConstantClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.NonUniformScale
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.NonUniformScale;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Applies a constant squash or stretch amount.
    /// </summary>
    [Serializable]
    public class SquashStretchConstantClip : SquashStretchClipBase
    {
        [Tooltip("Absolute squash or stretch amount applied along the selected axis.")]
        [SerializeField]
        private float amount;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref NonUniformScaleClipBlob blob)
        {
            base.Bake(clipEntity, context, ref builder, ref blob);

            blob.Type = NonUniformScaleType.SquashStretchAbsolute;
            blob.SquashStretchAbsolute = new NonUniformScaleClipBlob.SquashStretchAbsoluteData
            {
                Amount = this.amount,
            };
        }
    }
}
