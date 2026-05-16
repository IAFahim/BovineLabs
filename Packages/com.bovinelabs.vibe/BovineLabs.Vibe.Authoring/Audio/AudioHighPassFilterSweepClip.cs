// <copyright file="AudioHighPassFilterSweepClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Audio
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Audio;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that sweeps high-pass filter cutoff using a curve.
    /// </summary>
    [Serializable]
    public class AudioHighPassFilterSweepClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Curve sampled over the clip to drive the cutoff sweep.")]
        private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("Minimum cutoff frequency in hertz.")]
        [Min(0f)]
        private float minCutoffFrequency = 500f;

        [SerializeField]
        [Tooltip("Maximum cutoff frequency in hertz.")]
        [Min(0f)]
        private float maxCutoffFrequency = 500f;

        [SerializeField]
        [Tooltip("Treat the remapped value as an offset from the initial cutoff frequency.")]
        private bool relative;

        [SerializeField]
        [Tooltip(Strings.CurveStretchTooltip)]
        private bool remapCurveToClipLength;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<AudioHighPassFilterAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioHighPassFilterClipBlob>();
            root.Type = AudioHighPassFilterClipType.Sweep;

            AudioCurveSweepAuthoringUtility.BakeSweepData(
                clipEntity,
                context,
                this.curve,
                this.remapCurveToClipLength,
                Mathf.Max(0f, this.minCutoffFrequency),
                Mathf.Max(0f, this.maxCutoffFrequency),
                this.relative,
                ref builder,
                ref root.Sweep);

            var blob = builder.CreateBlobAssetReference<AudioHighPassFilterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioHighPassFilterClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.minCutoffFrequency = Mathf.Max(0f, this.minCutoffFrequency);
            this.maxCutoffFrequency = Mathf.Max(0f, this.maxCutoffFrequency);
        }
    }
}
#endif
