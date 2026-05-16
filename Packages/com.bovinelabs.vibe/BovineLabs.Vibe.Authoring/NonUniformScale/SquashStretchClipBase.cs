// <copyright file="SquashStretchClipBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.NonUniformScale
{
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.NonUniformScale;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Shared functionality for squash and stretch clip variants.
    /// </summary>
    public abstract class SquashStretchClipBase : NonUniformScaleClipBase
    {
        [Tooltip("Axis along which the geometry is stretched or squashed.")]
        [SerializeField]
        private SquashStretchNonUniformScaleAxis axis = SquashStretchNonUniformScaleAxis.Y;

        [Tooltip("Preserves volume by applying inverse scale on the other axes.")]
        [SerializeField]
        private bool preserveVolume = true;

        [Range(0f, 1f)]
        [Tooltip("Exponent controlling how aggressively volume compensation is applied.")]
        [SerializeField]
        private float compensationExponent = 0.5f;

        [SerializeField]
        [Tooltip(Strings.UseClipActivationTooltip)]
        private bool transformOnClipActivation = true;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref NonUniformScaleClipBlob blob)
        {
            blob.SquashStretch.Axis = this.axis;
            blob.SquashStretch.PreserveVolume = this.preserveVolume;
            blob.SquashStretch.TransformOnClipActivation = this.transformOnClipActivation;
            blob.SquashStretch.VolumeExponent = math.clamp(this.compensationExponent, 0f, 1f);
        }
    }
}
