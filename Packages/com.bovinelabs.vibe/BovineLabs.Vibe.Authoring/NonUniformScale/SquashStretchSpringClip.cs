// <copyright file="SquashStretchSpringClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.NonUniformScale
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using BovineLabs.Vibe.Data.NonUniformScale;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Uses a spring simulation to reach a squash or stretch rest value.
    /// </summary>
    [Serializable]
    public class SquashStretchSpringClip : SquashStretchClipBase
    {
        private const float MinimumResidual = 0.0001f;

        [Tooltip("Rest value the spring converges toward.")]
        [SerializeField]
        private float restValue;

        [Tooltip("Treats the rest value as a multiplier instead of an absolute amount.")]
        [SerializeField]
        private bool restIsMultiplier = true;

        [Tooltip(Strings.SpringInitialVelocityTooltip)]
        [SerializeField]
        private float initialVelocity;

        [Min(0f)]
        [Tooltip(Strings.SpringFrequencyTooltip)]
        [SerializeField]
        private float frequency = 1f;

        [Tooltip(Strings.SpringAutoDampingTooltip)]
        [SerializeField]
        private bool matchClipDuration;

        [Min(0f)]
        [Tooltip(Strings.SpringDampingTooltip)]
        [SerializeField]
        private float damping = 0.1f;

        [Range(MinimumResidual, 0.5f)]
        [Tooltip(Strings.SpringResidualAmplitudeTooltip)]
        [SerializeField]
        private float settleTolerance = 0.01f;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref NonUniformScaleClipBlob blob)
        {
            base.Bake(clipEntity, context, ref builder, ref blob);

            blob.Type = NonUniformScaleType.SquashStretchSpring;
            blob.SquashStretchSpring = new NonUniformScaleClipBlob.SquashStretchSpringData
            {
                RestValue = this.restValue,
                RestIsMultiplier = this.restIsMultiplier,
                InitialVelocity = this.initialVelocity,
                Frequency = math.max(0f, this.frequency),
                Damping = math.max(0f, this.damping),
            };

            if (this.matchClipDuration && context.Clip != null)
            {
                var clip = context.Clip;
                var clipDuration = (float)(clip.duration * clip.timeScale);
                if (clipDuration > math.FLT_MIN_NORMAL)
                {
                    var amplitude = math.abs(this.restIsMultiplier ? 1f - this.restValue : this.restValue);
                    var velocityMagnitude = math.abs(this.initialVelocity);

                    blob.SquashStretchSpring.Damping = SpringUtility.CalculateDampingForDuration(blob.SquashStretchSpring.Frequency, clipDuration,
                        math.clamp(this.settleTolerance, MinimumResidual, 0.5f), amplitude, velocityMagnitude);
                }
            }
        }
    }
}
