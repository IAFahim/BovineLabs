// <copyright file="HDRPMaterialPropertyData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_HDRP
namespace BovineLabs.Vibe.Data.Rendering
{
    using System;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Flags describing which HDRP material properties are overridden by a clip.
    /// </summary>
    [Flags]
    public enum HDRPMaterialPropertyFlags : uint
    {
        None = 0,
        AlphaCutoff = 1u << 0,
        AORemapMax = 1u << 1,
        AORemapMin = 1u << 2,
        DetailAlbedoScale = 1u << 3,
        DetailNormalScale = 1u << 4,
        DetailSmoothnessScale = 1u << 5,
        DiffusionProfileHash = 1u << 6,
        Metallic = 1u << 7,
        Smoothness = 1u << 8,
        SmoothnessRemapMax = 1u << 9,
        SmoothnessRemapMin = 1u << 10,
        Thickness = 1u << 11,
        EmissiveColor = 1u << 12,
        BaseColor = 1u << 13,
        SpecularColor = 1u << 14,
        ThicknessRemap = 1u << 15,
        UnlitColor = 1u << 16,
    }

    /// <summary>
    /// Clip data for HDRP material properties.
    /// </summary>
    public struct HDRPMaterialPropertyClipData : IComponentData
    {
        public HDRPMaterialPropertyFlags Flags;
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
    }
}
#endif
