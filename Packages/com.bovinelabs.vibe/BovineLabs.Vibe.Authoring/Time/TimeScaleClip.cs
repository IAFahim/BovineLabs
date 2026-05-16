// <copyright file="TimeScaleClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.Time
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Authoring;
    using BovineLabs.Vibe.Data.Time;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that drives the global time scale.
    /// </summary>
    [Serializable]
    public class TimeScaleClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Target time scale applied by this clip.")]
        private float targetScale = 1f;

        [SerializeField]
        [Tooltip("Minimum value applied to the evaluated time scale.")]
        private float clampMin;

        [SerializeField]
        [Tooltip("Maximum value applied to the evaluated time scale.")]
        private float clampMax = 100f;

        [SerializeField]
        [Tooltip("Use a curve to ease from the initial time scale to the target scale.")]
        private bool useCurve;

        [SerializeField]
        [Tooltip("Curve sampled over the clip to shape the time scale blend.")]
        private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip(Strings.CurveStretchTooltip)]
        private bool remapCurveToClipLength;

        [SerializeField]
        [Tooltip("Restore the captured initial time scale when the clip ends.")]
        private bool restoreOnDeactivate = true;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<TimeScaleAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<TimeScaleClipBlob>();

            var minClamp = Mathf.Max(0f, Mathf.Min(this.clampMin, this.clampMax));
            var maxClamp = Mathf.Max(minClamp, Mathf.Max(this.clampMin, this.clampMax));

            root.TargetScale = Mathf.Max(0f, this.targetScale);
            root.ClampMin = minClamp;
            root.ClampMax = maxClamp;
            root.RestoreOnDeactivate = this.restoreOnDeactivate;

            var useCurve = this.useCurve && this.curve != null && this.curve.length > 0;
            root.UseCurve = useCurve;

            if (useCurve)
            {
                CurveSweepAuthoringUtility.BakeCurve(
                    clipEntity,
                    context,
                    this.curve,
                    this.remapCurveToClipLength,
                    ref builder,
                    ref root.Curve);
            }

            var blob = builder.CreateBlobAssetReference<TimeScaleClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new TimeScaleClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.targetScale = Mathf.Max(0f, this.targetScale);
            this.clampMin = Mathf.Max(0f, this.clampMin);
            this.clampMax = Mathf.Max(this.clampMin, this.clampMax);
        }
    }
}
