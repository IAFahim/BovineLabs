// <copyright file="ScaleSpringClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Uses a spring simulation to reach a target uniform scale.
    /// </summary>
    [Serializable]
    public class ScaleSpringClip : ScaleClipBase
    {
        private const float MinimumResidual = 0.0001f;

        [Tooltip("Rest scale the spring converges toward.")]
        [SerializeField]
        private float restScale = 1f;

        [Tooltip("Treats the rest scale as a multiplier against the current value.")]
        [SerializeField]
        private bool restIsMultiplier = true;

        [Tooltip(Strings.SpringInitialVelocityTooltip)]
        [SerializeField]
        private float initialVelocity;

        [Min(0f)]
        [Tooltip(Strings.SpringFrequencyTooltip)]
        [SerializeField]
        private float frequency = 1f;

        [Tooltip(Strings.UseClipActivationTooltip)]
        [SerializeField]
        private bool transformOnClipActivation = true;

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

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref ScaleClipBlob blob)
        {
            blob.Type = ScaleType.Spring;
            blob.Spring = new ScaleClipBlob.SpringData
            {
                TransformOnClipActivation = this.transformOnClipActivation,
                RestScale = this.restScale,
                RestIsMultiplier = this.restIsMultiplier,
                InitialVelocity = this.initialVelocity,
                Frequency = math.max(0f, this.frequency),
                Damping = math.max(0f, this.damping),
            };

            if (this.matchClipDuration && context.Clip != null)
            {
                var clipDuration = (float)(context.Clip.duration * context.Clip.timeScale);
                if (clipDuration > math.FLT_MIN_NORMAL)
                {
                    var amplitude = this.restIsMultiplier ? math.abs(1f - this.restScale) : math.abs(this.restScale - 1f);

                    var velocityMagnitude = math.abs(this.initialVelocity);

                    blob.Spring.Damping = SpringUtility.CalculateDampingForDuration(blob.Spring.Frequency, clipDuration,
                        math.clamp(this.settleTolerance, MinimumResidual, 0.5f), amplitude, velocityMagnitude);
                }
            }
        }
    }
}
