// <copyright file="HDRPMaterialPropertyInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_HDRP
namespace BovineLabs.Vibe.Data.Rendering
{
    using Unity.Entities;

    /// <summary>
    /// Captures initial HDRP material property values.
    /// </summary>
    public struct HDRPMaterialPropertyInitial : IComponentData
    {
        public HDRPMaterialPropertyFlags Flags;
        public HDRPMaterialPropertyBlend Value;
    }
}
#endif