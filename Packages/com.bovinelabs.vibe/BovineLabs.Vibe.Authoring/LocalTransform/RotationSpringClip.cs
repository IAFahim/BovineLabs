// <copyright file="RotationSpringClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Uses a spring simulation to reach a desired orientation.
    /// </summary>
    [Serializable]
    public class RotationSpringClip : RotationClipBase
    {
        private const float MinimumResidual = 0.0001f;

        [Tooltip("Euler angles describing the rest rotation, in degrees.")]
        [SerializeField]
        private Vector3 restRotation = Vector3.zero;

        [Tooltip(Strings.UseClipActivationTooltip)]
        [SerializeField]
        private bool useClipActivation = true;

        [Tooltip("Initial angular velocity applied when the clip activates.")]
        [SerializeField]
        private Vector3 initialVelocity = Vector3.zero;

        [Tooltip("Oscillation frequency, in hertz, for each Euler axis.")]
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

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref RotationClipBlob blob)
        {
            var restEulerLocal = math.float3(this.restRotation);
            var initialVelocityLocal = math.float3(this.initialVelocity);

            blob.Type = RotationType.Spring;
            blob.TransformOnClipActivation = this.useClipActivation;
            blob.Spring = new RotationClipBlob.SpringData
            {
                RestEuler = restEulerLocal,
                InitialVelocity = initialVelocityLocal,
                Frequency = math.max(float3.zero, this.frequency),
                Damping = math.max(float3.zero, this.damping),
            };

            if (this.matchClipDuration && context.Clip != null)
            {
                var clipDuration = (float)(context.Clip.duration * context.Clip.timeScale);
                if (clipDuration > math.FLT_MIN_NORMAL)
                {
                    var tolerance = math.clamp(this.settleTolerance, MinimumResidual, 0.5f);
                    var amplitudeAbs = math.abs(restEulerLocal);
                    var velocityAbs = math.abs(initialVelocityLocal);

                    blob.Spring.Damping = new float3(
                        SpringUtility.CalculateDampingForDuration(blob.Spring.Frequency.x, clipDuration, tolerance, amplitudeAbs.x, velocityAbs.x),
                        SpringUtility.CalculateDampingForDuration(blob.Spring.Frequency.y, clipDuration, tolerance, amplitudeAbs.y, velocityAbs.y),
                        SpringUtility.CalculateDampingForDuration(blob.Spring.Frequency.z, clipDuration, tolerance, amplitudeAbs.z, velocityAbs.z));
                }
            }
        }
    }
}
