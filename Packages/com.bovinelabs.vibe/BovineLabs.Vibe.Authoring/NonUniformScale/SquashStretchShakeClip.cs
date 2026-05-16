// <copyright file="SquashStretchShakeClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.NonUniformScale
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Authoring;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.NonUniformScale;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Applies procedural shake noise to the squash or stretch amount.
    /// </summary>
    [Serializable]
    public class SquashStretchShakeClip : SquashStretchClipBase
    {
        [Tooltip("Shake amplitude applied to the squash or stretch value.")]
        [SerializeField]
        private float amplitude = 0.25f;

        [Min(0f)]
        [Tooltip(Strings.ShakeFrequencyTooltip)]
        [SerializeField]
        private float frequency = 20f;

        [Min(0f)]
        [Tooltip(Strings.ShakeDampingTooltip)]
        [SerializeField]
        private float damping;

        [Tooltip("Enable an attenuation curve to scale the squash or stretch shake over the clip duration.")]
        [SerializeField]
        private bool useAttenuationCurve;

        [Tooltip("Curve sampled over normalized clip time (0-1) that attenuates the deformation amplitude.")]
        [SerializeField]
        private AnimationCurve attenuationCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Tooltip(Strings.ShakeRemapAttenuationCurveTooltip)]
        [SerializeField]
        private bool remapAttenuationCurveToClipLength;

        [SerializeField]
        [Tooltip("If 0 a random seed will be generated.")]
        private uint seed;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref NonUniformScaleClipBlob blob)
        {
            base.Bake(clipEntity, context, ref builder, ref blob);

            blob.Type = NonUniformScaleType.SquashStretchShake;
            blob.SquashStretchShake = new NonUniformScaleClipBlob.SquashStretchShakeData
            {
                Amplitude = math.abs(this.amplitude),
                Frequency = math.max(0f, this.frequency),
                Damping = math.max(0f, this.damping),
                Seed = this.seed,
            };

            var hasCurve = ShakeAttenuationCurveAuthoringUtility.TryConstructAttenuationCurve(
                ref builder,
                ref blob.SquashStretchShake.AttenuationCurve,
                this.useAttenuationCurve,
                this.attenuationCurve,
                this.remapAttenuationCurveToClipLength,
                context.Clip);

            if (hasCurve)
            {
                context.Baker.AddComponent(clipEntity, ClipBlobCurveCache.Create());
            }
        }
    }
}
