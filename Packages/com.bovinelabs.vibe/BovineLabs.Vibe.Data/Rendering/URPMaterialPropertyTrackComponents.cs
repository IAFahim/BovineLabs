// <copyright file="URPMaterialPropertyTrackComponents.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Vibe.Data.Rendering
{
    using Unity.Entities;

    /// <summary>
    /// Tracks component add/remove state for URP material properties on a track.
    /// </summary>
    public struct URPMaterialPropertyTrackComponents : IComponentData
    {
        public bool AddComponents;
        public URPMaterialPropertyFlags AddedFlags;
        public int BumpScaleCount;
        public int CutoffCount;
        public int MetallicCount;
        public int OcclusionStrengthCount;
        public int SmoothnessCount;
        public int BaseColorCount;
        public int EmissionColorCount;
        public int SpecColorCount;
    }
}
#endif
