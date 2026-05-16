// <copyright file="AudioCurveSweepUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.Audio;

    /// <summary>
    /// Shared helpers for evaluating curve-driven audio sweeps.
    /// </summary>
    internal static class AudioCurveSweepUtility
    {
        public static float Evaluate(ref AudioCurveSweepData sweep, float localTime, ref ClipBlobCurveCache cache, float baseValue)
        {
            return CurveSweepUtility.Evaluate(ref sweep.Curve, sweep.Min, sweep.Max, sweep.Relative, localTime, ref cache, baseValue);
        }
    }
}
#endif
