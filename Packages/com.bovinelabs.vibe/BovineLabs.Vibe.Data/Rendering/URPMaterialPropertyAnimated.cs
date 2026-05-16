// <copyright file="URPMaterialPropertyAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Vibe.Data.Rendering
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Animated clip value for URP material properties.
    /// </summary>
    public struct URPMaterialPropertyAnimated : IAnimatedComponent<URPMaterialPropertyBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public URPMaterialPropertyBlend Value { get; set; }
    }
}
#endif