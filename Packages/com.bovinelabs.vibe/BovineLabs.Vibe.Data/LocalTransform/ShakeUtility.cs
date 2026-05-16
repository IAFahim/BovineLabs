// <copyright file="ShakeUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.LocalTransform
{
    using Unity.Mathematics;

    /// <summary>
    /// Generates smooth random samples for shake effects.
    /// </summary>
    internal static class ShakeUtility
    {
        public static float Sample(float time, float frequency, float damping, uint seed, int axisOffset = 0)
        {
            if (frequency <= math.FLT_MIN_NORMAL)
            {
                return 0f;
            }

            var progress = time * frequency;
            var sampleIndex = (int)math.floor(progress);
            var fraction = progress - sampleIndex;
            var blend = fraction * fraction * (3f - (2f * fraction));

            var value0 = RandomValue(sampleIndex, seed, axisOffset);
            var value1 = RandomValue(sampleIndex + 1, seed, axisOffset);
            var value = math.lerp(value0, value1, blend);

            if (damping > math.FLT_MIN_NORMAL)
            {
                value *= math.exp(-damping * time);
            }

            return value;
        }

        private static float RandomValue(int sampleIndex, uint seed, int axisOffset)
        {
            var hash = math.hash(new uint4((uint)axisOffset, seed, (uint)sampleIndex, 0));
            var random = new Random(math.max(hash, 1));
            return random.NextFloat(-1f, 1f);
        }
    }
}
