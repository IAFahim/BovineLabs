// <copyright file="URPMaterialPropertyBlend.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Vibe.Data.Rendering
{
    using Unity.Mathematics;

    /// <summary>
    /// Blend data for URP material properties.
    /// </summary>
    public struct URPMaterialPropertyBlend
    {
        public float BumpScale;
        public float Cutoff;
        public float Metallic;
        public float OcclusionStrength;
        public float Smoothness;
        public float4 BaseColor;
        public float4 EmissionColor;
        public float4 SpecColor;
        public bool EnableBumpScale;
        public bool EnableCutoff;
        public bool EnableMetallic;
        public bool EnableOcclusionStrength;
        public bool EnableSmoothness;
        public bool EnableBaseColor;
        public bool EnableEmissionColor;
        public bool EnableSpecColor;
    }
}
#endif