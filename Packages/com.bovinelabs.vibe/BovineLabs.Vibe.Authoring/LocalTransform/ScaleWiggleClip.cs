// <copyright file="ScaleWiggleClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Adds low-frequency noise to the uniform scale.
    /// </summary>
    [Serializable]
    public class ScaleWiggleClip : ScaleClipBase
    {
        [Tooltip("Amplitude of the wiggle noise applied to the scale.")]
        [SerializeField]
        private float amplitude = 0.05f;

        [Min(0f)]
        [Tooltip(Strings.WiggleFrequencyTooltip)]
        [SerializeField]
        private float frequency = 4f;

        [Range(0f, 1f)]
        [Tooltip(Strings.WiggleSmoothingTooltip)]
        [SerializeField]
        private float smoothness = 0.6f;

        [Min(0f)]
        [Tooltip(Strings.NoisePerAxisFrequencyMultiplierTooltip)]
        [SerializeField]
        private float frequencyMultiplier = 1f;

        [Tooltip("Enable a curve to scale wiggle amplitude over the clip duration.")]
        [SerializeField]
        private bool useAmplitudeCurve;

        [Tooltip(Strings.NoiseAmplitudeCurveTooltip)]
        [SerializeField]
        private AnimationCurve amplitudeCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Tooltip(Strings.NoiseRemapAmplitudeCurveTooltip)]
        [SerializeField]
        private bool remapAmplitudeCurveToClipLength;

        [Tooltip("Enable a curve to scale wiggle frequency over the clip duration.")]
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
            blob.Type = ScaleType.Wiggle;
            blob.Wiggle = new ScaleClipBlob.WiggleData
            {
                TransformOnClipActivation = this.transformOnClipActivation,
                Amplitude = math.abs(this.amplitude),
                Frequency = math.max(0f, this.frequency),
                Smoothness = math.saturate(this.smoothness),
                Seed = this.seed,
                PerAxisFrequencyMultiplier = math.max(0f, this.frequencyMultiplier),
            };

            var hasCurve = ShakeAttenuationCurveAuthoringUtility.TryConstructAttenuationCurve(
                ref builder,
                ref blob.Wiggle.AmplitudeCurve,
                this.useAmplitudeCurve,
                this.amplitudeCurve,
                this.remapAmplitudeCurveToClipLength,
                context.Clip);

            hasCurve |= ShakeAttenuationCurveAuthoringUtility.TryConstructAttenuationCurve(
                ref builder,
                ref blob.Wiggle.FrequencyCurve,
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
