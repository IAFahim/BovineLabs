// <copyright file="ScaleShakeClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Authoring;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Applies procedural shake noise to the uniform scale.
    /// </summary>
    [Serializable]
    public class ScaleShakeClip : ScaleClipBase
    {
        [Tooltip("Shake amplitude applied to the scale value.")]
        [SerializeField]
        private float amplitude = 0.05f;

        [Min(0f)]
        [Tooltip(Strings.ShakeFrequencyTooltip)]
        [SerializeField]
        private float frequency = 20f;

        [Min(0f)]
        [Tooltip(Strings.ShakeDampingTooltip)]
        [SerializeField]
        private float damping;

        [Min(0f)]
        [Tooltip(Strings.NoisePerAxisFrequencyMultiplierTooltip)]
        [SerializeField]
        private float frequencyMultiplier = 1f;

        [Tooltip("Enable an attenuation curve to scale the shake amplitude over the clip duration.")]
        [SerializeField]
        private bool useAttenuationCurve;

        [Tooltip(Strings.ShakeAttenuationCurveTooltip)]
        [SerializeField]
        private AnimationCurve attenuationCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Tooltip(Strings.ShakeRemapAttenuationCurveTooltip)]
        [SerializeField]
        private bool remapAttenuationCurveToClipLength;

        [Tooltip("Enable a curve to scale shake amplitude over the clip duration.")]
        [SerializeField]
        private bool useAmplitudeCurve;

        [Tooltip(Strings.NoiseAmplitudeCurveTooltip)]
        [SerializeField]
        private AnimationCurve amplitudeCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Tooltip(Strings.NoiseRemapAmplitudeCurveTooltip)]
        [SerializeField]
        private bool remapAmplitudeCurveToClipLength;

        [Tooltip("Enable a curve to scale shake frequency over the clip duration.")]
        [SerializeField]
        private bool useFrequencyCurve;

        [Tooltip(Strings.NoiseFrequencyCurveTooltip)]
        [SerializeField]
        private AnimationCurve frequencyCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Tooltip(Strings.NoiseRemapFrequencyCurveTooltip)]
        [SerializeField]
        private bool remapFrequencyCurveToClipLength;

        [Tooltip(Strings.UseClipActivationTooltip)]
        [SerializeField]
        private bool transformOnClipActivation = true;

        [SerializeField]
        [Tooltip(Strings.RandomSeedTooltip)]
        private uint seed;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref ScaleClipBlob blob)
        {
            blob.Type = ScaleType.Shake;
            blob.Shake = new ScaleClipBlob.ShakeData
            {
                TransformOnClipActivation = this.transformOnClipActivation,
                Amplitude = math.abs(this.amplitude),
                Frequency = math.max(0f, this.frequency),
                Damping = math.max(0f, this.damping),
                Seed = this.seed,
                PerAxisFrequencyMultiplier = math.max(0f, this.frequencyMultiplier),
            };

            var hasCurve = ShakeAttenuationCurveAuthoringUtility.TryConstructAttenuationCurve(
                ref builder,
                ref blob.Shake.AttenuationCurve,
                this.useAttenuationCurve,
                this.attenuationCurve,
                this.remapAttenuationCurveToClipLength,
                context.Clip);

            hasCurve |= ShakeAttenuationCurveAuthoringUtility.TryConstructAttenuationCurve(
                ref builder,
                ref blob.Shake.AmplitudeCurve,
                this.useAmplitudeCurve,
                this.amplitudeCurve,
                this.remapAmplitudeCurveToClipLength,
                context.Clip);

            hasCurve |= ShakeAttenuationCurveAuthoringUtility.TryConstructAttenuationCurve(
                ref builder,
                ref blob.Shake.FrequencyCurve,
                this.useFrequencyCurve,
                this.frequencyCurve,
                this.remapFrequencyCurveToClipLength,
                context.Clip);

            if (hasCurve)
            {
                context.Baker.AddComponent(clipEntity, ClipBlobCurveCache.Create());
            }
        }
    }
}
