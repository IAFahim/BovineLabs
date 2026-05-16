// <copyright file="CurveSweepUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Vibe.Data;
    using Unity.Mathematics;

    /// <summary>
    /// Shared helpers for evaluating curve-driven sweeps.
    /// </summary>
    internal static class CurveSweepUtility
    {
        public static float Evaluate(ref BlobCurve curve, float min, float max, bool relative, float localTime, ref ClipBlobCurveCache cache, float baseValue)
        {
            if (!curve.IsCreated)
            {
                return baseValue;
            }

            var minValue = math.min(min, max);
            var maxValue = math.max(min, max);
            var normalized = math.saturate(curve.Evaluate(localTime, ref cache.Cache0));
            var value = math.lerp(minValue, maxValue, normalized);
            return relative ? baseValue + value : value;
        }
    }
}
