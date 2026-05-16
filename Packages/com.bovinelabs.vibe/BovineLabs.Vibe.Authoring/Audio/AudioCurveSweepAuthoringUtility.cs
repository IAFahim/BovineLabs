// <copyright file="AudioCurveSweepAuthoringUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Audio
{
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Authoring;
    using BovineLabs.Vibe.Data.Audio;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Helper for baking curve-based sweep data into blob assets.
    /// </summary>
    internal static class AudioCurveSweepAuthoringUtility
    {
        public static void BakeSweepData(
            Entity clipEntity, BakingContext context, AnimationCurve curve, bool remapCurveToClipLength, float min, float max, bool relative,
            ref BlobBuilder builder, ref AudioCurveSweepData sweep)
        {
            sweep.Min = min;
            sweep.Max = max;
            sweep.Relative = relative;

            if (curve == null || curve.length == 0)
            {
                return;
            }

            CurveSweepAuthoringUtility.BakeCurve(
                clipEntity,
                context,
                curve,
                remapCurveToClipLength,
                ref builder,
                ref sweep.Curve);
        }
    }
}
#endif
