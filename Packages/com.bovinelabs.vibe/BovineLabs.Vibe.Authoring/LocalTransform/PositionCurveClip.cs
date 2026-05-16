// <copyright file="PositionCurveClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Utility;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Animates a position clip by sampling independent animation curves for each axis.
    /// </summary>
    [Serializable]
    public class PositionCurveClip : PositionClipBase
    {
        [SerializeField]
        [Tooltip("Enables sampling of the X-axis curve.")]
        private bool animateX;

        [SerializeField]
        [Tooltip("Curve evaluated to produce the X-axis offset over time.")]
        private AnimationCurve curveX = AnimationCurve.Linear(0, 0, 1, 0);

        [SerializeField]
        [Tooltip("Enables sampling of the Y-axis curve.")]
        private bool animateY;

        [SerializeField]
        [Tooltip("Curve evaluated to produce the Y-axis offset over time.")]
        private AnimationCurve curveY = AnimationCurve.Linear(0, 0, 1, 0);

        [SerializeField]
        [Tooltip("Enables sampling of the Z-axis curve.")]
        private bool animateZ;

        [SerializeField]
        [Tooltip("Curve evaluated to produce the Z-axis offset over time.")]
        private AnimationCurve curveZ = AnimationCurve.Linear(0, 0, 1, 0);

        [Tooltip(Strings.UseClipActivationTooltip)]
        [SerializeField]
        private bool useClipActivation = true;

        [SerializeField]
        [Tooltip("Stretch the time of enabled curves so they span the playable clip length.")]
        private bool remapCurveToClipLength;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PositionClipBlob blob)
        {
            blob.Type = PositionType.Curve;
            blob.TransformOnClipActivation = this.useClipActivation;

            var hasCurve = CreateCurve(ref blob.Curve.CurveX, this.animateX, this.curveX, ref builder, this.remapCurveToClipLength, context.Clip);
            hasCurve |= CreateCurve(ref blob.Curve.CurveY, this.animateY, this.curveY, ref builder, this.remapCurveToClipLength, context.Clip);
            hasCurve |= CreateCurve(ref blob.Curve.CurveZ, this.animateZ, this.curveZ, ref builder, this.remapCurveToClipLength, context.Clip);

            if (hasCurve)
            {
                context.Baker.AddComponent(clipEntity, ClipBlobCurveCache.Create());
            }
        }

        private static bool CreateCurve(ref BlobCurve blobCurve, bool enabled, AnimationCurve curve, ref BlobBuilder builder, bool remapCurveToClipLength,
            TimelineClip clip)
        {
            if (!enabled || curve == null || curve.length == 0)
            {
                return false;
            }

            var curveToBake = curve;

            if (remapCurveToClipLength && clip != null)
            {
                var clipDuration = (float)(clip.duration * clip.timeScale);
                if (CurveRemapUtility.TryRemapToClipLength(curve, (float)clip.clipIn, clipDuration, out var remappedCurve))
                {
                    curveToBake = remappedCurve;
                }
            }

            BlobCurve.Construct(ref builder, ref blobCurve, curveToBake);
            return true;
        }
    }
}
