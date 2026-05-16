// <copyright file="URPMaterialPropertyData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Vibe.Data.Rendering
{
    using System;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Flags describing which URP material properties are overridden by a clip.
    /// </summary>
    [Flags]
    public enum URPMaterialPropertyFlags : uint
    {
        None = 0,
        BumpScale = 1u << 0,
        Cutoff = 1u << 1,
        Metallic = 1u << 2,
        OcclusionStrength = 1u << 3,
        Smoothness = 1u << 4,
        BaseColor = 1u << 5,
        EmissionColor = 1u << 6,
        SpecColor = 1u << 7,
    }

    /// <summary>
    /// Clip data for URP material properties.
    /// </summary>
    public struct URPMaterialPropertyClipData : IComponentData
    {
        public URPMaterialPropertyFlags Flags;
        public float BumpScale;
        public float Cutoff;
        public float Metallic;
        public float OcclusionStrength;
        public float Smoothness;
        public float4 BaseColor;
        public float4 EmissionColor;
        public float4 SpecColor;
    }
}
#endif
