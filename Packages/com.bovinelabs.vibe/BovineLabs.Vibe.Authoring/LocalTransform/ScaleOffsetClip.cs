// <copyright file="ScaleOffsetClip.cs" company="BovineLabs">
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
    /// Applies an additive or multiplicative offset to the binding scale.
    /// </summary>
    [Serializable]
    public class ScaleOffsetClip : ScaleClipBase
    {
        [Tooltip("Offset value applied to the current scale.")]
        [SerializeField]
        private float value = 1f;

        [Tooltip("Treats the value as a multiplier instead of an additive offset.")]
        [SerializeField]
        private bool useMultiplier = true;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref ScaleClipBlob blob)
        {
            blob.Type = ScaleType.Offset;
            blob.Offset = new ScaleClipBlob.OffsetData
            {
                Value = this.value,
                IsMultiplier = this.useMultiplier,
            };
        }
    }
}
