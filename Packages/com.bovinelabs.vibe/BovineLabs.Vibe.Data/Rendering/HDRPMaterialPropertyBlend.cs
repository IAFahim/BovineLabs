// <copyright file="HDRPMaterialPropertyBlend.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_HDRP
namespace BovineLabs.Vibe.Data.Rendering
{
    using Unity.Mathematics;

    /// <summary>
    /// Blend data for HDRP material properties.
    /// </summary>
    public struct HDRPMaterialPropertyBlend
    {
        public float AlphaCutoff;
        public float AORemapMax;
        public float AORemapMin;
        public float DetailAlbedoScale;
        public float DetailNormalScale;
        public float DetailSmoothnessScale;
        public float DiffusionProfileHash;
        public float Metallic;
        public float Smoothness;
        public float SmoothnessRemapMax;
        public float SmoothnessRemapMin;
        public float Thickness;
        public float3 EmissiveColor;
        public float4 BaseColor;
        public float4 SpecularColor;
        public float4 ThicknessRemap;
        public float4 UnlitColor;
        public bool EnableAlphaCutoff;
        public bool EnableAORemapMax;
        public bool EnableAORemapMin;
        public bool EnableDetailAlbedoScale;
        public bool EnableDetailNormalScale;
        public bool EnableDetailSmoothnessScale;
        public bool EnableDiffusionProfileHash;
        public bool EnableMetallic;
        public bool EnableSmoothness;
        public bool EnableSmoothnessRemapMax;
        public bool EnableSmoothnessRemapMin;
        public bool EnableThickness;
        public bool EnableEmissiveColor;
        public bool EnableBaseColor;
        public bool EnableSpecularColor;
        public bool EnableThicknessRemap;
        public bool EnableUnlitColor;
    }
}
#endif