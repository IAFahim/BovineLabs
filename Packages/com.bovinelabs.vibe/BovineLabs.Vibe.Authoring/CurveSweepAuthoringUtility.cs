// <copyright file="CurveSweepAuthoringUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Utility;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Shared helper for baking curve sweeps into blob curves.
    /// </summary>
    internal static class CurveSweepAuthoringUtility
    {
        public static void BakeCurve(
            Entity clipEntity, BakingContext context, AnimationCurve curve, bool remapCurveToClipLength, ref BlobBuilder builder, ref BlobCurve targetCurve)
        {
            if (curve == null || curve.length == 0)
            {
                return;
            }

            var curveToBake = curve;
            if (remapCurveToClipLength && context.Clip != null)
            {
                var clipDuration = (float)(context.Clip.duration * context.Clip.timeScale);
                if (CurveRemapUtility.TryRemapToClipLength(curve, (float)context.Clip.clipIn, clipDuration, out var remappedCurve))
                {
                    curveToBake = remappedCurve;
                }
            }

            BlobCurve.Construct(ref builder, ref targetCurve, curveToBake);

            context.Baker.AddComponent(clipEntity, ClipBlobCurveCache.Create());
        }
    }
}
