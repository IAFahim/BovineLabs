// <copyright file="AudioSourceVolumeSweepClip.cs" company="BovineLabs">
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
    /// Timeline clip that sweeps audio source volume using a curve.
    /// </summary>
    [Serializable]
    public class AudioSourceVolumeSweepClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Curve sampled over the clip to drive the volume sweep.")]
        private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("Minimum volume for remapping the curve output.")]
        [Min(0f)]
        private float minVolume = 1f;

        [SerializeField]
        [Tooltip("Maximum volume for remapping the curve output.")]
        [Min(0f)]
        private float maxVolume = 1f;

        [SerializeField]
        [Tooltip("Treat the remapped value as an offset from the initial volume.")]
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

            context.Baker.AddComponent<AudioSourceDataAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioSourceDataClipBlob>();
            root.Type = AudioSourceDataClipType.VolumeSweep;

            AudioCurveSweepAuthoringUtility.BakeSweepData(
                clipEntity,
                context,
                this.curve,
                this.remapCurveToClipLength,
                Mathf.Max(0f, this.minVolume),
                Mathf.Max(0f, this.maxVolume),
                this.relative,
                ref builder,
                ref root.Sweep);

            var blob = builder.CreateBlobAssetReference<AudioSourceDataClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioSourceDataClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.minVolume = Mathf.Max(0f, this.minVolume);
            this.maxVolume = Mathf.Max(0f, this.maxVolume);
        }
    }
}
#endif
