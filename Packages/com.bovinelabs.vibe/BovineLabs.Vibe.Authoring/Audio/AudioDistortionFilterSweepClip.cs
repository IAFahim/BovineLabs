// <copyright file="AudioDistortionFilterSweepClip.cs" company="BovineLabs">
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
    /// Timeline clip that sweeps distortion amount using a curve.
    /// </summary>
    [Serializable]
    public class AudioDistortionFilterSweepClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Curve sampled over the clip to drive the distortion sweep.")]
        private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("Minimum distortion amount (0-1).")]
        [Range(0f, 1f)]
        private float minDistortion = 0.5f;

        [SerializeField]
        [Tooltip("Maximum distortion amount (0-1).")]
        [Range(0f, 1f)]
        private float maxDistortion = 0.5f;

        [SerializeField]
        [Tooltip("Treat the remapped value as an offset from the initial distortion.")]
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

            context.Baker.AddComponent<AudioDistortionFilterAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioDistortionFilterClipBlob>();
            root.Type = AudioDistortionFilterClipType.Sweep;

            AudioCurveSweepAuthoringUtility.BakeSweepData(
                clipEntity,
                context,
                this.curve,
                this.remapCurveToClipLength,
                Mathf.Clamp01(this.minDistortion),
                Mathf.Clamp01(this.maxDistortion),
                this.relative,
                ref builder,
                ref root.Sweep);

            var blob = builder.CreateBlobAssetReference<AudioDistortionFilterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioDistortionFilterClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.minDistortion = Mathf.Clamp01(this.minDistortion);
            this.maxDistortion = Mathf.Clamp01(this.maxDistortion);
        }
    }
}
#endif
