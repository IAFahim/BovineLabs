// <copyright file="CinemachineAxisUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using Unity.Cinemachine;
    using Unity.Mathematics;

    internal static class CinemachineAxisUtility
    {
        private const float AxisEpsilon = 0.0001f;

        public static void SanitizeAxis(ref InputAxis axis)
        {
            var range = axis.Range;
            range.y = math.max(range.y, range.x);
            axis.Range = range;

            axis.Center = ClampAxisValue(axis, axis.Center);
            axis.Value = ClampAxisValue(axis, axis.Value);

            var recenter = axis.Recentering;
            recenter.Wait = math.max(0f, recenter.Wait);
            recenter.Time = math.max(0f, recenter.Time);
            axis.Recentering = recenter;
        }

        public static void ClampTiltRange(ref InputAxis axis)
        {
            var range = axis.Range;
            range.x = math.clamp(range.x, -90f, 90f);
            range.y = math.clamp(range.y, -90f, 90f);
            if (range.y < range.x)
            {
                range.y = range.x;
            }

            axis.Range = range;
        }

        public static void ClampRadialRangeMin(ref InputAxis axis)
        {
            var range = axis.Range;
            range.x = math.max(range.x, AxisEpsilon);
            if (range.y < range.x)
            {
                range.y = range.x;
            }

            axis.Range = range;
        }

        private static float ClampAxisValue(in InputAxis axis, float value)
        {
            var min = axis.Range.x;
            var max = axis.Range.y;
            var range = max - min;

            if (!axis.Wrap || range < AxisEpsilon)
            {
                return math.clamp(value, min, max);
            }

            var v = (value - min) % range;
            if (v < 0f)
            {
                v += range;
            }

            return v + min;
        }
    }
}
#endif
