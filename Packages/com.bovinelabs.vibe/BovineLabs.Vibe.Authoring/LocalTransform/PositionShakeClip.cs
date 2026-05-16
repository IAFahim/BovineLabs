// <copyright file="PositionShakeClip.cs" company="BovineLabs">
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
    /// Applies a procedural shake offset to the bound transform.
    /// </summary>
    [Serializable]
    public class PositionShakeClip : PositionClipBase
    {
        [Tooltip("Shake amplitude evaluated per-axis in the chosen transform space.")]
        [SerializeField]
        private SpaceVector3 amplitude = SpaceVector3.Local(Vector3.one * 0.1f);

        [Min(0f)]
        [Tooltip(Strings.ShakeFrequencyTooltip)]
        [SerializeField]
        private float frequency = 20f;

        [Min(0f)]
        [Tooltip(Strings.ShakeDampingTooltip)]
        [SerializeField]
        private float damping;

        [Tooltip(Strings.NoisePerAxisFrequencyMultiplierTooltip)]
        [SerializeField]
        private Vector3 perAxisFrequencyMultiplier = Vector3.one;

        [Tooltip("Enable an attenuation curve to scale shake amplitude over the clip duration.")]
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

        [SerializeField]
        [Tooltip(Strings.RandomSeedTooltip)]
        private uint seed;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PositionClipBlob blob)
        {
            blob.Type = PositionType.Shake;
            blob.TransformOnClipActivation = this.amplitude.UseClipActivation;
            blob.Shake = new PositionClipBlob.ShakeData
            {
                Space = this.amplitude.Space,
                Amplitude = math.abs(new float3(this.amplitude.Value)),
                Frequency = math.max(0f, this.frequency),
                Damping = math.max(0f, this.damping),
                Seed = this.seed,
                PerAxisFrequencyMultiplier = math.max(float3.zero, new float3(this.perAxisFrequencyMultiplier)),
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
