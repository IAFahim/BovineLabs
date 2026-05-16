// <copyright file="AudioEchoFilterSweepClip.cs" company="BovineLabs">
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
    /// Timeline clip that sweeps echo wet mix using a curve.
    /// </summary>
    [Serializable]
    public class AudioEchoFilterSweepClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Curve sampled over the clip to drive the wet mix sweep.")]
        private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("Minimum wet mix level.")]
        [Min(0f)]
        private float minWetMix = 1f;

        [SerializeField]
        [Tooltip("Maximum wet mix level.")]
        [Min(0f)]
        private float maxWetMix = 1f;

        [SerializeField]
        [Tooltip("Treat the remapped value as an offset from the initial wet mix.")]
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

            context.Baker.AddComponent<AudioEchoFilterAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioEchoFilterClipBlob>();
            root.Type = AudioEchoFilterClipType.Sweep;

            AudioCurveSweepAuthoringUtility.BakeSweepData(
                clipEntity,
                context,
                this.curve,
                this.remapCurveToClipLength,
                Mathf.Max(0f, this.minWetMix),
                Mathf.Max(0f, this.maxWetMix),
                this.relative,
                ref builder,
                ref root.Sweep);

            var blob = builder.CreateBlobAssetReference<AudioEchoFilterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioEchoFilterClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.minWetMix = Mathf.Max(0f, this.minWetMix);
            this.maxWetMix = Mathf.Max(0f, this.maxWetMix);
        }
    }
}
#endif
