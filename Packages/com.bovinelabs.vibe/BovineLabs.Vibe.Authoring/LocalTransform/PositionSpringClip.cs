// <copyright file="PositionSpringClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
#if BL_REACTION
    using BovineLabs.Reaction.Data.Core;
#endif
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Uses a critically damped spring to animate the transform towards a rest point.
    /// </summary>
    [Serializable]
    public class PositionSpringClip : PositionClipBase
    {
        private const float MinimumResidual = 0.0001f;

        [Tooltip("Controls whether the spring moves to an absolute position, an additive offset, or applies an impulse.")]
        [SerializeField]
        private PositionClipBlob.SpringData.PositionSpringMode mode = PositionClipBlob.SpringData.PositionSpringMode.Bump;

        [Tooltip("Determines if the rest point and initial velocity are defined in local or world space.")]
        [SerializeField]
        private TransformSpace space = TransformSpace.Local;

#if BL_REACTION
        [Tooltip("Optional director target used as the moving anchor when resolving the spring.")]
        [SerializeField]
        private Target target = Target.None;
#endif

        [Tooltip("Rest position the spring converges toward.")]
        [SerializeField]
        private Vector3 restPoint = Vector3.zero;

        [Tooltip(Strings.UseClipActivationTooltip)]
        [SerializeField]
        private bool useClipActivation = true;

        [Tooltip(Strings.SpringInitialVelocityTooltip)]
        [SerializeField]
        private Vector3 initialVelocity = Vector3.zero;

        [Tooltip("Oscillation frequency, in hertz, for each axis of the spring motion.")]
        [SerializeField]
        private Vector3 frequency = Vector3.one;

        [Tooltip(Strings.SpringAutoDampingTooltip)]
        [SerializeField]
        private bool matchClipDuration;

        [Tooltip(Strings.SpringDampingPerAxisTooltip)]
        [SerializeField]
        private Vector3 damping = new(0.1f, 0.1f, 0.1f);

        [Range(MinimumResidual, 0.5f)]
        [Tooltip(Strings.SpringResidualAmplitudeTooltip)]
        [SerializeField]
        private float settleTolerance = 0.01f;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PositionClipBlob blob)
        {
            var restPointValue = math.float3(this.restPoint);
            var initialVelocityValue = math.float3(this.initialVelocity);

            blob.Type = PositionType.Spring;
            blob.TransformOnClipActivation = this.useClipActivation;
            blob.Spring = new PositionClipBlob.SpringData
            {
                Mode = this.mode,
                Space = this.space,
#if BL_REACTION
                Target = this.target,
#endif
                RestPoint = restPointValue,
                InitialVelocity = initialVelocityValue,
                Frequency = math.max(float3.zero, this.frequency),
                Damping = math.max(float3.zero, this.damping),
            };

            if (this.matchClipDuration && context.Clip != null)
            {
                var clipDuration = (float)(context.Clip.duration * context.Clip.timeScale);
                if (clipDuration > math.FLT_MIN_NORMAL)
                {
                    var tolerance = math.clamp(this.settleTolerance, MinimumResidual, 0.5f);
                    var amplitudeAbs = math.abs(restPointValue);
                    var velocityAbs = math.abs(initialVelocityValue);

                    blob.Spring.Damping = new float3(
                        SpringUtility.CalculateDampingForDuration(blob.Spring.Frequency.x, clipDuration, tolerance, amplitudeAbs.x, velocityAbs.x),
                        SpringUtility.CalculateDampingForDuration(blob.Spring.Frequency.y, clipDuration, tolerance, amplitudeAbs.y, velocityAbs.y),
                        SpringUtility.CalculateDampingForDuration(blob.Spring.Frequency.z, clipDuration, tolerance, amplitudeAbs.z, velocityAbs.z));
                }
            }
        }
    }
}
