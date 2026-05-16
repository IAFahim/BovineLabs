// <copyright file="HDRPMaterialPropertyAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_HDRP
namespace BovineLabs.Vibe.Data.Rendering
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Animated clip value for HDRP material properties.
    /// </summary>
    public struct HDRPMaterialPropertyAnimated : IAnimatedComponent<HDRPMaterialPropertyBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public HDRPMaterialPropertyBlend Value { get; set; }
    }
}
#endif