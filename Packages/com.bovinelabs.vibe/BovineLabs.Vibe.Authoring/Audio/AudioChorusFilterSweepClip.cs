// <copyright file="AudioChorusFilterSweepClip.cs" company="BovineLabs">
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
    /// Timeline clip that sweeps chorus depth using a curve.
    /// </summary>
    [Serializable]
    public class AudioChorusFilterSweepClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Curve sampled over the clip to drive the depth sweep.")]
        private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("Minimum depth value.")]
        [Min(0f)]
        private float minDepth = 0.03f;

        [SerializeField]
        [Tooltip("Maximum depth value.")]
        [Min(0f)]
        private float maxDepth = 0.03f;

        [SerializeField]
        [Tooltip("Treat the remapped value as an offset from the initial depth.")]
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

            context.Baker.AddComponent<AudioChorusFilterAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioChorusFilterClipBlob>();
            root.Type = AudioChorusFilterClipType.Sweep;

            AudioCurveSweepAuthoringUtility.BakeSweepData(
                clipEntity,
                context,
                this.curve,
                this.remapCurveToClipLength,
                Mathf.Max(0f, this.minDepth),
                Mathf.Max(0f, this.maxDepth),
                this.relative,
                ref builder,
                ref root.Sweep);

            var blob = builder.CreateBlobAssetReference<AudioChorusFilterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioChorusFilterClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.minDepth = Mathf.Max(0f, this.minDepth);
            this.maxDepth = Mathf.Max(0f, this.maxDepth);
        }
    }
}
#endif
