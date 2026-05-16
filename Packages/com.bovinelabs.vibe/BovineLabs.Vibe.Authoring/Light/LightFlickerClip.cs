// <copyright file="LightFlickerClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Light
{
    using System;
    using BovineLabs.Core.Utility;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Authoring;
    using BovineLabs.Vibe.Data.Light;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that drives preset-based light flickering.
    /// </summary>
    [Serializable]
    public class LightFlickerClip : LightClipBase
    {
        [SerializeField]
        [Tooltip("Preset pattern to evaluate when generating the flicker.")]
        private LightFlickerPreset preset = LightFlickerPreset.Organic;

        [SerializeField]
        [Tooltip("Scales the preset frequency. 1.0 maps to one evaluation per second for strobe and base noise speed for the other presets.")]
        [Min(0f)]
        private float speed = 1f;

        [SerializeField]
        [Tooltip("Minimum multiplier applied to the reference intensity.")]
        [Min(0f)]
        private float minMultiplier = 0.35f;

        [SerializeField]
        [Tooltip("Maximum multiplier applied to the reference intensity.")]
        [Min(0f)]
        private float maxMultiplier = 1.05f;

        [SerializeField]
        [Tooltip("Duty cycle used by the strobe preset (fraction of the cycle spent at max).")]
        [Range(0f, 1f)]
        private float dutyCycle = 0.2f;

        [SerializeField]
        [Tooltip("Ensure the pattern loops over the clip duration.")]
        private bool loop;

        [SerializeField]
        [Tooltip(Strings.RandomSeedTooltip)]
        private uint seed;

        [SerializeField]
        [Tooltip("Use a custom curve instead of the preset pattern.")]
        private bool useCustomCurve;

        [SerializeField]
        [Tooltip("Curve sampled to drive the flicker signal.")]
        private AnimationCurve customCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip(Strings.CurveStretchTooltip)]
        private bool remapCurveToClipLength;

        [SerializeField]
        [Tooltip("Override the light color by lerping between Color A and Color B with the flicker signal.")]
        private bool overrideColor;

        [SerializeField]
        [Tooltip("Color used when the flicker signal is at its minimum value.")]
        private Color colorA = Color.white;

        [SerializeField]
        [Tooltip("Color used when the flicker signal is at its maximum value.")]
        private Color colorB = Color.white;

        [SerializeField]
        [Tooltip("Override the color temperature by lerping between Temperature A and Temperature B.")]
        private bool overrideColorTemperature;

        [SerializeField]
        [Tooltip("Color temperature used when the flicker signal is at its minimum value.")]
        [Min(0f)]
        private float temperatureA = 6500f;

        [SerializeField]
        [Tooltip("Color temperature used when the flicker signal is at its maximum value.")]
        [Min(0f)]
        private float temperatureB = 6500f;

        /// <inheritdoc/>
        public override ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        protected override LightClipType Type => LightClipType.Flicker;

        /// <inheritdoc/>
        protected override void Configure(
            Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref LightClipBlobData clipData, ref LightClipData clipComponent)
        {
            ref var data = ref clipData.Flicker;
            data.Preset = this.preset;
            data.Speed = math.max(this.speed, math.FLT_MIN_NORMAL);

            data.UseCustomCurve = this.useCustomCurve && this.customCurve is { length: > 0 };

            if (data.UseCustomCurve)
            {
                CurveSweepAuthoringUtility.BakeCurve(clipEntity, context, this.customCurve, this.remapCurveToClipLength, ref builder, ref data.Curve);
            }
            else
            {
                if (this.preset == LightFlickerPreset.Strobe)
                {
                    if (this.loop)
                    {
                        var clipDuration = (float)(context.Clip!.duration * context.Clip.timeScale);
                        if (clipDuration > math.FLT_MIN_NORMAL && data.Speed > 0f)
                        {
                            var cycles = math.round(data.Speed * clipDuration);
                            cycles = math.max(1f, cycles);
                            data.Speed = cycles / clipDuration;
                        }
                    }
                }
            }

            var min = math.max(0f, this.minMultiplier);
            var max = math.max(min, this.maxMultiplier);
            data.MinIntensityMultiplier = min;
            data.MaxIntensityMultiplier = max;
            data.DutyCycle = math.saturate(this.dutyCycle);
            data.Seed = this.seed == 0 ? GlobalRandom.NextUInt(1, uint.MaxValue) : this.seed;
            data.Curve = default;
            data.OverrideColor = this.overrideColor;

            data.ColorA = new float3(this.colorA.r, this.colorA.g, this.colorA.b);
            data.ColorB = new float3(this.colorB.r, this.colorB.g, this.colorB.b);
            data.OverrideColorTemperature = this.overrideColorTemperature;
            data.TemperatureA = math.max(0f, this.temperatureA);
            data.TemperatureB = math.max(0f, this.temperatureB);
        }

        private void OnValidate()
        {
            this.speed = math.max(0f, this.speed);
            this.minMultiplier = math.max(0f, this.minMultiplier);
            this.maxMultiplier = math.max(this.minMultiplier, this.maxMultiplier);
            this.dutyCycle = math.saturate(this.dutyCycle);
            this.temperatureA = math.max(0f, this.temperatureA);
            this.temperatureB = math.max(0f, this.temperatureB);
        }
    }
}
#endif
