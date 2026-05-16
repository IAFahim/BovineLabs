// <copyright file="SpringUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.LocalTransform
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;

    /// <summary>
    /// Helper methods for evaluating damped spring responses.
    /// </summary>
    public static class SpringUtility
    {
        private const float FrequencyThreshold = 1e-4f;
        private const float CriticalEpsilon = 1e-4f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sample(float time, float frequency, float dampingRatio, float amplitude, float velocity)
        {
            if (math.abs(amplitude) <= math.FLT_MIN_NORMAL && math.abs(velocity) <= math.FLT_MIN_NORMAL)
            {
                return 0f;
            }

            var damping = math.max(0f, dampingRatio);

            if (frequency <= FrequencyThreshold)
            {
                var envelope = math.exp(-damping * time);
                return envelope * (amplitude + (velocity * time));
            }

            var omega0 = frequency * (2f * math.PI);

            if (damping < 1f - CriticalEpsilon)
            {
                var omegaD = math.max(omega0 * math.sqrt(1f - (damping * damping)), FrequencyThreshold);
                var expTerm = math.exp(-damping * omega0 * time);
                var sinTerm = math.sin(omegaD * time);
                var cosTerm = math.cos(omegaD * time);
                var c1 = amplitude;
                var c2 = (velocity + (damping * omega0 * amplitude)) / omegaD;
                return expTerm * ((c1 * cosTerm) + (c2 * sinTerm));
            }

            if (damping <= 1f + CriticalEpsilon)
            {
                var expTerm = math.exp(-omega0 * time);
                return expTerm * (amplitude + (velocity + (omega0 * amplitude)) * time);
            }

            var sqrtTerm = math.sqrt((damping * damping) - 1f);
            var r1 = -omega0 * (damping - sqrtTerm);
            var r2 = -omega0 * (damping + sqrtTerm);
            var denom = r2 - r1;

            if (math.abs(denom) <= math.FLT_MIN_NORMAL)
            {
                return amplitude * math.exp(r1 * time);
            }

            var c2Over = (velocity - (r1 * amplitude)) / denom;
            var c1Over = amplitude - c2Over;
            return (c1Over * math.exp(r1 * time)) + (c2Over * math.exp(r2 * time));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Sample(float time, float3 frequency, float3 dampingRatio, float3 amplitude, float3 velocity)
        {
            var amplitudeAbs = math.abs(amplitude);
            var velocityAbs = math.abs(velocity);
            if (math.all(amplitudeAbs <= new float3(math.FLT_MIN_NORMAL)) && math.all(velocityAbs <= new float3(math.FLT_MIN_NORMAL)))
            {
                return float3.zero;
            }

            return new float3(
                Sample(time, frequency.x, dampingRatio.x, amplitude.x, velocity.x),
                Sample(time, frequency.y, dampingRatio.y, amplitude.y, velocity.y),
                Sample(time, frequency.z, dampingRatio.z, amplitude.z, velocity.z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateDampingForDuration(
            float frequency,
            float duration,
            float residualRatio,
            float initialDisplacementMagnitude,
            float initialVelocityMagnitude)
        {
            if (duration <= math.FLT_MIN_NORMAL)
            {
                return 0f;
            }

            var residual = math.clamp(residualRatio, 0.0001f, 0.5f);

            var displacement = math.abs(initialDisplacementMagnitude);
            var velocity = math.abs(initialVelocityMagnitude);

            if (displacement <= math.FLT_MIN_NORMAL && velocity <= math.FLT_MIN_NORMAL)
            {
                return 0f;
            }

            var equivalentDisplacement = displacement;
            if (equivalentDisplacement <= math.FLT_MIN_NORMAL)
            {
                if (frequency <= FrequencyThreshold)
                {
                    equivalentDisplacement = velocity * duration;
                }
                else
                {
                    var omega0 = frequency * (2f * math.PI);
                    equivalentDisplacement = velocity / math.max(omega0, FrequencyThreshold);
                }
            }

            var target = residual * equivalentDisplacement;
            if (target <= math.FLT_MIN_NORMAL)
            {
                return 0f;
            }

            float Evaluate(float damping)
            {
                return math.abs(Sample(duration, frequency, damping, displacement, velocity));
            }

            const int iterations = 24;
            var lower = 0f;
            var upper = 1f;
            var sample = Evaluate(upper);

            for (var i = 0; i < iterations && sample > target && upper < 256f; i++)
            {
                lower = upper;
                upper *= 2f;
                sample = Evaluate(upper);

                if (!math.isfinite(sample))
                {
                    break;
                }
            }

            for (var i = 0; i < iterations; i++)
            {
                var mid = (lower + upper) * 0.5f;
                var value = Evaluate(mid);
                if (!math.isfinite(value))
                {
                    upper = mid;
                    continue;
                }

                if (value > target)
                {
                    lower = mid;
                }
                else
                {
                    upper = mid;
                }
            }

            return math.max(0f, upper);
        }
    }
}
