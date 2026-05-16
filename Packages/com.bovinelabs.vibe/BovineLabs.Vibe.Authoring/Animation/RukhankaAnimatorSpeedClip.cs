// <copyright file="RukhankaAnimatorSpeedClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Authoring.Animation
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Authoring;
    using BovineLabs.Vibe.Data.Animation;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that drives animator speed values.
    /// </summary>
    [Serializable]
    public class RukhankaAnimatorSpeedClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("How to drive the animator speed for this clip.")]
        private RukhankaAnimatorSpeedMode mode = RukhankaAnimatorSpeedMode.Constant;

        [SerializeField]
        [Tooltip("Speed value for Constant mode, or minimum for Random/Curve modes.")]
        private float minSpeed = 1f;

        [SerializeField]
        [Tooltip("Maximum speed for Random/Curve modes.")]
        private float maxSpeed = 1f;

        [SerializeField]
        [Tooltip("Treat the speed value as an offset from the initial speed.")]
        private bool relative;

        [SerializeField]
        [Tooltip("Curve sampled over the clip to drive the speed.")]
        private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip(Strings.CurveStretchTooltip)]
        private bool remapCurveToClipLength;

        [SerializeField]
        [Tooltip("Seed used for deterministic randomization. If 0, a stable hash is used.")]
        private uint seed;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<RukhankaAnimatorSpeedAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<RukhankaAnimatorSpeedClipBlob>();
            root.Mode = this.mode;
            root.MinSpeed = this.minSpeed;
            root.MaxSpeed = this.maxSpeed;
            root.Relative = this.relative;
            root.Seed = this.seed;

            if (this.mode == RukhankaAnimatorSpeedMode.Curve)
            {
                CurveSweepAuthoringUtility.BakeCurve(
                    clipEntity,
                    context,
                    this.curve,
                    this.remapCurveToClipLength,
                    ref builder,
                    ref root.Curve);
            }

            var blobRef = builder.CreateBlobAssetReference<RukhankaAnimatorSpeedClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(
                clipEntity,
                new RukhankaAnimatorSpeedClipData
                {
                    Value = blobRef,
                });
        }
    }
}

#endif
