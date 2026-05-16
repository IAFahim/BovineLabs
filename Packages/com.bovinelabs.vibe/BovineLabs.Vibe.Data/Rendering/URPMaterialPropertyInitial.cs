// <copyright file="URPMaterialPropertyInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Vibe.Data.Rendering
{
    using Unity.Entities;

    /// <summary>
    /// Captures initial URP material property values.
    /// </summary>
    public struct URPMaterialPropertyInitial : IComponentData
    {
        public URPMaterialPropertyFlags Flags;
        public URPMaterialPropertyBlend Value;
    }
}
#endif