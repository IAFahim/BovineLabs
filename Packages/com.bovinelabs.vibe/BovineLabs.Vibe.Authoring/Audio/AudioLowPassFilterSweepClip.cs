// <copyright file="AudioLowPassFilterSweepClip.cs" company="BovineLabs">
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
    /// Timeline clip that sweeps low-pass filter cutoff using a curve.
    /// </summary>
    [Serializable]
    public class AudioLowPassFilterSweepClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Curve sampled over the clip to drive the cutoff sweep.")]
        private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("Minimum cutoff frequency in hertz.")]
        [Min(0f)]
        private float minCutoffFrequency = 5000f;

        [SerializeField]
        [Tooltip("Maximum cutoff frequency in hertz.")]
        [Min(0f)]
        private float maxCutoffFrequency = 5000f;

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

            context.Baker.AddComponent<AudioLowPassFilterAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioLowPassFilterClipBlob>();
            root.Type = AudioLowPassFilterClipType.Sweep;

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

            var blob = builder.CreateBlobAssetReference<AudioLowPassFilterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioLowPassFilterClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.minCutoffFrequency = Mathf.Max(0f, this.minCutoffFrequency);
            this.maxCutoffFrequency = Mathf.Max(0f, this.maxCutoffFrequency);
        }
    }
}
#endif
