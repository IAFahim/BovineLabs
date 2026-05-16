// <copyright file="ClipCurveUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;

    /// <summary>
    /// Helpers for evaluating curves normalized over clip duration.
    /// </summary>
    internal static class ClipCurveUtility
    {
        public static float EvaluateNormalized(
            ref BlobCurve curve, float localTime, in TimeTransform timeTransform, ref BlobCurveCache cache, float defaultValue = 1f)
        {
            if (!curve.IsCreated)
            {
                return defaultValue;
            }

            var clipDuration = (float)((timeTransform.End - timeTransform.Start) * timeTransform.Scale);
            if (!math.isfinite(clipDuration) || math.abs(clipDuration) <= math.FLT_MIN_NORMAL)
            {
                return curve.Evaluate(0f, ref cache);
            }

            var clipIn = (float)timeTransform.ClipIn;
            var normalizedTime = math.saturate((localTime - clipIn) / clipDuration);
            return curve.Evaluate(normalizedTime, ref cache);
        }
    }
}
