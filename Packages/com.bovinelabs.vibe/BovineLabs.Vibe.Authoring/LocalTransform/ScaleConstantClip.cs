// <copyright file="ScaleConstantClip.cs" company="BovineLabs">
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
    /// Applies a constant uniform scale to the binding.
    /// </summary>
    [Serializable]
    public class ScaleConstantClip : ScaleClipBase
    {
        [Tooltip("Uniform scale applied for the duration of the clip.")]
        [SerializeField]
        private float scale = 1f;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref ScaleClipBlob blob)
        {
            blob.Type = ScaleType.Absolute;
            blob.Absolute = new ScaleClipBlob.AbsoluteData
            {
                Value = this.scale,
            };
        }
    }
}
