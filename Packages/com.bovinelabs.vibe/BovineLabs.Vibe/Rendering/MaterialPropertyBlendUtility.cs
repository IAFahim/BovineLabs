// <copyright file="MaterialPropertyBlendUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe
{
    using Unity.Mathematics;

    /// <summary>
    /// Helper methods for blending material property values with enable flags.
    /// </summary>
    internal static class MaterialPropertyBlendUtility
    {
        public static float BlendValue(float a, bool aEnabled, float b, bool bEnabled, float s, out bool enabled)
        {
            if (aEnabled)
            {
                if (bEnabled)
                {
                    enabled = true;
                    return math.lerp(a, b, s);
                }

                enabled = true;
                return a;
            }

            if (bEnabled)
            {
                enabled = true;
                return b;
            }

            enabled = false;
            return 0f;
        }

        public static float3 BlendValue(float3 a, bool aEnabled, float3 b, bool bEnabled, float s, out bool enabled)
        {
            if (aEnabled)
            {
                if (bEnabled)
                {
                    enabled = true;
                    return math.lerp(a, b, s);
                }

                enabled = true;
                return a;
            }

            if (bEnabled)
            {
                enabled = true;
                return b;
            }

            enabled = false;
            return default;
        }

        public static float4 BlendValue(float4 a, bool aEnabled, float4 b, bool bEnabled, float s, out bool enabled)
        {
            if (aEnabled)
            {
                if (bEnabled)
                {
                    enabled = true;
                    return math.lerp(a, b, s);
                }

                enabled = true;
                return a;
            }

            if (bEnabled)
            {
                enabled = true;
                return b;
            }

            enabled = false;
            return default;
        }

        public static float AddValue(float a, bool aEnabled, float b, bool bEnabled, out bool enabled)
        {
            if (aEnabled)
            {
                if (bEnabled)
                {
                    enabled = true;
                    return a + b;
                }

                enabled = true;
                return a;
            }

            if (bEnabled)
            {
                enabled = true;
                return b;
            }

            enabled = false;
            return 0f;
        }

        public static float3 AddValue(float3 a, bool aEnabled, float3 b, bool bEnabled, out bool enabled)
        {
            if (aEnabled)
            {
                if (bEnabled)
                {
                    enabled = true;
                    return a + b;
                }

                enabled = true;
                return a;
            }

            if (bEnabled)
            {
                enabled = true;
                return b;
            }

            enabled = false;
            return default;
        }

        public static float4 AddValue(float4 a, bool aEnabled, float4 b, bool bEnabled, out bool enabled)
        {
            if (aEnabled)
            {
                if (bEnabled)
                {
                    enabled = true;
                    return a + b;
                }

                enabled = true;
                return a;
            }

            if (bEnabled)
            {
                enabled = true;
                return b;
            }

            enabled = false;
            return default;
        }
    }
}
