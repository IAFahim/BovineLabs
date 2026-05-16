// <copyright file="LightExtendedCurveClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Light
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Authoring;
    using BovineLabs.Vibe.Data.Light;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that animates extended light properties using curves.
    /// </summary>
    [Serializable]
    public class LightExtendedCurveClip : LightClipBase
    {
        [SerializeField]
        [Tooltip("Animate the light range using a curve.")]
        private bool animateRange = true;

        [SerializeField]
        [Tooltip("Curve sampled to drive the light range.")]
        private AnimationCurve rangeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("Minimum range for remapping the curve output.")]
        [Min(0f)]
        private float rangeMin = 10f;

        [SerializeField]
        [Tooltip("Maximum range for remapping the curve output.")]
        [Min(0f)]
        private float rangeMax = 10f;

        [SerializeField]
        [Tooltip("Treat the remapped range as an offset from the base value.")]
        private bool rangeRelative;

        [SerializeField]
        [Tooltip("Use the initial track value as the base for relative range.")]
        private bool rangeUseInitial = true;

        [SerializeField]
        [Tooltip("Animate the outer spot angle using a curve.")]
        private bool animateSpotAngle;

        [SerializeField]
        [Tooltip("Curve sampled to drive the outer spot angle.")]
        private AnimationCurve spotAngleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("Minimum outer spot angle for remapping the curve output.")]
        [Range(0f, 179f)]
        private float spotAngleMin = 30f;

        [SerializeField]
        [Tooltip("Maximum outer spot angle for remapping the curve output.")]
        [Range(0f, 179f)]
        private float spotAngleMax = 30f;

        [SerializeField]
        [Tooltip("Treat the remapped outer spot angle as an offset from the base value.")]
        private bool spotAngleRelative;

        [SerializeField]
        [Tooltip("Use the initial track value as the base for relative outer spot angle.")]
        private bool spotAngleUseInitial = true;

        [SerializeField]
        [Tooltip("Animate the inner spot angle using a curve.")]
        private bool animateInnerSpotAngle;

        [SerializeField]
        [Tooltip("Curve sampled to drive the inner spot angle.")]
        private AnimationCurve innerSpotAngleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("Minimum inner spot angle for remapping the curve output.")]
        [Range(0f, 179f)]
        private float innerSpotAngleMin;

        [SerializeField]
        [Tooltip("Maximum inner spot angle for remapping the curve output.")]
        [Range(0f, 179f)]
        private float innerSpotAngleMax;

        [SerializeField]
        [Tooltip("Treat the remapped inner spot angle as an offset from the base value.")]
        private bool innerSpotAngleRelative;

        [SerializeField]
        [Tooltip("Use the initial track value as the base for relative inner spot angle.")]
        private bool innerSpotAngleUseInitial = true;

        [SerializeField]
        [Tooltip("Animate the shadow strength using a curve.")]
        private bool animateShadowStrength;

        [SerializeField]
        [Tooltip("Curve sampled to drive the shadow strength.")]
        private AnimationCurve shadowStrengthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("Minimum shadow strength for remapping the curve output.")]
        [Range(0f, 1f)]
        private float shadowStrengthMin = 1f;

        [SerializeField]
        [Tooltip("Maximum shadow strength for remapping the curve output.")]
        [Range(0f, 1f)]
        private float shadowStrengthMax = 1f;

        [SerializeField]
        [Tooltip("Treat the remapped shadow strength as an offset from the base value.")]
        private bool shadowStrengthRelative;

        [SerializeField]
        [Tooltip("Use the initial track value as the base for relative shadow strength.")]
        private bool shadowStrengthUseInitial = true;

        [SerializeField]
        [Tooltip(Strings.CurveStretchTooltip)]
        private bool remapCurveToClipLength;

        /// <inheritdoc/>
        public override ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        protected override LightClipType Type => LightClipType.ExtendedCurve;

        /// <inheritdoc/>
        protected override void Configure(
            Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref LightClipBlobData clipData, ref LightClipData clipComponent)
        {
            ref var data = ref clipData.ExtendedCurve;

            var rangeMinValue = math.max(0f, math.min(this.rangeMin, this.rangeMax));
            var rangeMaxValue = math.max(rangeMinValue, math.max(this.rangeMin, this.rangeMax));
            this.ConfigureCurve(ref data.Range, this.animateRange, this.rangeCurve, rangeMinValue, rangeMaxValue, this.rangeRelative, this.rangeUseInitial,
                clipEntity, context, ref builder);

            var spotMin = math.clamp(math.min(this.spotAngleMin, this.spotAngleMax), 0f, 179f);
            var spotMax = math.clamp(math.max(this.spotAngleMin, this.spotAngleMax), 0f, 179f);
            if (spotMax < spotMin)
            {
                spotMax = spotMin;
            }

            this.ConfigureCurve(ref data.SpotAngle, this.animateSpotAngle, this.spotAngleCurve, spotMin, spotMax, this.spotAngleRelative,
                this.spotAngleUseInitial, clipEntity, context, ref builder);

            var innerMin = math.clamp(math.min(this.innerSpotAngleMin, this.innerSpotAngleMax), 0f, 179f);
            var innerMax = math.clamp(math.max(this.innerSpotAngleMin, this.innerSpotAngleMax), 0f, 179f);
            if (innerMax < innerMin)
            {
                innerMax = innerMin;
            }

            this.ConfigureCurve(ref data.InnerSpotAngle, this.animateInnerSpotAngle, this.innerSpotAngleCurve, innerMin, innerMax, this.innerSpotAngleRelative,
                this.innerSpotAngleUseInitial, clipEntity, context, ref builder);

            var shadowMin = math.clamp(math.min(this.shadowStrengthMin, this.shadowStrengthMax), 0f, 1f);
            var shadowMax = math.clamp(math.max(this.shadowStrengthMin, this.shadowStrengthMax), 0f, 1f);
            if (shadowMax < shadowMin)
            {
                shadowMax = shadowMin;
            }

            this.ConfigureCurve(ref data.ShadowStrength, this.animateShadowStrength, this.shadowStrengthCurve, shadowMin, shadowMax,
                this.shadowStrengthRelative, this.shadowStrengthUseInitial, clipEntity, context, ref builder);
        }

        private void ConfigureCurve(
            ref LightExtendedCurve target, bool enabled, AnimationCurve curve, float min, float max, bool relative, bool useInitial, Entity clipEntity,
            BakingContext context, ref BlobBuilder builder)
        {
            target.OverrideValue = enabled;
            target.Min = min;
            target.Max = max;
            target.Relative = relative;
            target.UseInitial = useInitial;

            if (!enabled)
            {
                return;
            }

            CurveSweepAuthoringUtility.BakeCurve(clipEntity, context, curve, this.remapCurveToClipLength, ref builder, ref target.Curve);
        }

        private void OnValidate()
        {
            this.rangeMin = Mathf.Max(0f, this.rangeMin);
            this.rangeMax = Mathf.Max(this.rangeMin, this.rangeMax);

            this.spotAngleMin = Mathf.Clamp(this.spotAngleMin, 0f, 179f);
            this.spotAngleMax = Mathf.Clamp(this.spotAngleMax, 0f, 179f);
            if (this.spotAngleMax < this.spotAngleMin)
            {
                this.spotAngleMax = this.spotAngleMin;
            }

            this.innerSpotAngleMin = Mathf.Clamp(this.innerSpotAngleMin, 0f, 179f);
            this.innerSpotAngleMax = Mathf.Clamp(this.innerSpotAngleMax, 0f, 179f);
            if (this.innerSpotAngleMax < this.innerSpotAngleMin)
            {
                this.innerSpotAngleMax = this.innerSpotAngleMin;
            }

            this.shadowStrengthMin = Mathf.Clamp01(this.shadowStrengthMin);
            this.shadowStrengthMax = Mathf.Clamp01(this.shadowStrengthMax);
            if (this.shadowStrengthMax < this.shadowStrengthMin)
            {
                this.shadowStrengthMax = this.shadowStrengthMin;
            }
        }
    }
}
#endif
