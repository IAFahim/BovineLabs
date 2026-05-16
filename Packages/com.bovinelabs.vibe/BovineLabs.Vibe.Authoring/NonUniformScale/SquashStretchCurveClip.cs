// <copyright file="SquashStretchCurveClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.NonUniformScale
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Utility;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.NonUniformScale;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Samples an animation curve to drive squash or stretch.
    /// </summary>
    [Serializable]
    public class SquashStretchCurveClip : SquashStretchClipBase
    {
        [Tooltip("Curve evaluated to determine the squash or stretch amount over time.")]
        [SerializeField]
        private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 0);

        [Tooltip(Strings.CurveStretchTooltip)]
        [SerializeField]
        private bool remapCurveToClipLength = true;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref NonUniformScaleClipBlob blob)
        {
            base.Bake(clipEntity, context, ref builder, ref blob);

            blob.Type = NonUniformScaleType.SquashStretchCurve;

            if (this.curve == null || this.curve.length == 0)
            {
                return;
            }

            var curveToBake = this.curve;
            if (this.remapCurveToClipLength && context.Clip != null)
            {
                var clip = context.Clip;
                var clipDuration = (float)(clip.duration * clip.timeScale);
                if (clipDuration > 0f && CurveRemapUtility.TryRemapToClipLength(this.curve, (float)clip.clipIn, clipDuration, out var remappedCurve))
                {
                    curveToBake = remappedCurve;
                }
            }

            BlobCurve.Construct(ref builder, ref blob.SquashStretchCurve.Curve, curveToBake);
            context.Baker.AddComponent(clipEntity, ClipBlobCurveCache.Create());
        }
    }
}
