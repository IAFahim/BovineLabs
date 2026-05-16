// <copyright file="RotationWiggleClip.cs" company="BovineLabs">
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
    /// Adds low-frequency noise to the rotation of the binding.
    /// </summary>
    [Serializable]
    public class RotationWiggleClip : RotationClipBase
    {
        [Tooltip("Amplitude of the wiggle noise in Euler degrees.")]
        [SerializeField]
        private SpaceVector3 amplitude = SpaceVector3.Local(Vector3.one * 3f);

        [Min(0f)]
        [Tooltip(Strings.WiggleFrequencyTooltip)]
        [SerializeField]
        private float frequency = 4f;

        [Range(0f, 1f)]
        [Tooltip(Strings.WiggleSmoothingTooltip)]
        [SerializeField]
        private float smoothness = 0.6f;

        [Tooltip(Strings.NoisePerAxisFrequencyMultiplierTooltip)]
        [SerializeField]
        private Vector3 perAxisFrequencyMultiplier = Vector3.one;

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

        [SerializeField]
        [Tooltip(Strings.RandomSeedTooltip)]
        private uint seed;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref RotationClipBlob blob)
        {
            blob.Type = RotationType.Wiggle;
            blob.TransformOnClipActivation = this.amplitude.UseClipActivation;
            blob.Wiggle = new RotationClipBlob.WiggleData
            {
                Space = this.amplitude.Space,
                Amplitude = math.abs(new float3(this.amplitude.Value)),
                Frequency = math.max(0f, this.frequency),
                Smoothness = math.saturate(this.smoothness),
                Seed = this.seed,
                PerAxisFrequencyMultiplier = math.max(float3.zero, new float3(this.perAxisFrequencyMultiplier)),
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
