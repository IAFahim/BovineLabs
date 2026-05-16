// <copyright file="AudioSourcePanSweepClip.cs" company="BovineLabs">
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
    /// Timeline clip that sweeps audio source stereo pan using a curve.
    /// </summary>
    [Serializable]
    public class AudioSourcePanSweepClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Curve sampled over the clip to drive the pan sweep.")]
        private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("Minimum pan for remapping the curve output.")]
        [Range(-1f, 1f)]
        private float minPan;

        [SerializeField]
        [Tooltip("Maximum pan for remapping the curve output.")]
        [Range(-1f, 1f)]
        private float maxPan;

        [SerializeField]
        [Tooltip("Treat the remapped value as an offset from the initial pan.")]
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

            context.Baker.AddComponent<AudioSourcePanAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioSourcePanSweepClipBlob>();
            AudioCurveSweepAuthoringUtility.BakeSweepData(
                clipEntity,
                context,
                this.curve,
                this.remapCurveToClipLength,
                Mathf.Clamp(this.minPan, -1f, 1f),
                Mathf.Clamp(this.maxPan, -1f, 1f),
                this.relative,
                ref builder,
                ref root.Sweep);

            var blob = builder.CreateBlobAssetReference<AudioSourcePanSweepClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioSourcePanSweepClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.minPan = Mathf.Clamp(this.minPan, -1f, 1f);
            this.maxPan = Mathf.Clamp(this.maxPan, -1f, 1f);
        }
    }
}
#endif
