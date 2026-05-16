// <copyright file="WiggleUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.LocalTransform
{
    using Unity.Mathematics;

    /// <summary>
    /// Generates smooth pseudo-random noise for wiggle effects.
    /// </summary>
    internal static class WiggleUtility
    {
        public static float Sample(float time, float frequency, float smoothness, uint seed, int axisOffset = 0)
        {
            if (frequency <= math.FLT_MIN_NORMAL)
            {
                return 0f;
            }

            var progress = math.max(0f, time) * frequency;
            var segmentIndex = (int)math.floor(progress);
            var t = progress - segmentIndex;

            var p0 = RandomValue(segmentIndex - 1, seed, axisOffset);
            var p1 = RandomValue(segmentIndex, seed, axisOffset);
            var p2 = RandomValue(segmentIndex + 1, seed, axisOffset);
            var p3 = RandomValue(segmentIndex + 2, seed, axisOffset);

            var catmull = CatmullRom(p0, p1, p2, p3, t);
            var linear = math.lerp(p1, p2, t);

            var blend = math.saturate(smoothness);
            var value = math.lerp(linear, catmull, blend);

            return math.clamp(value, -1f, 1f);
        }

        private static float RandomValue(int sampleIndex, uint seed, int axisOffset)
        {
            var hash = math.hash(new uint4((uint)sampleIndex, seed, (uint)axisOffset, 0));
            var random = new Random(math.max(hash, 1));
            return random.NextFloat(-1f, 1f);
        }

        private static float CatmullRom(float p0, float p1, float p2, float p3, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return 0.5f * ((2f * p1) + ((-p0 + p2) * t) + (((2f * p0) - (5f * p1) + (4f * p2) - p3) * t2) + ((-p0 + (3f * p1) - (3f * p2) + p3) * t3));
        }
    }
}
