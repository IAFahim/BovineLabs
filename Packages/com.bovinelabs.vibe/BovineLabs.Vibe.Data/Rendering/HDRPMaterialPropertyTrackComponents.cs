// <copyright file="HDRPMaterialPropertyTrackComponents.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_HDRP
namespace BovineLabs.Vibe.Data.Rendering
{
    using Unity.Entities;

    /// <summary>
    /// Tracks component add/remove state for HDRP material properties on a track.
    /// </summary>
    public struct HDRPMaterialPropertyTrackComponents : IComponentData
    {
        public bool ManageComponents;
        public HDRPMaterialPropertyFlags AddedFlags;
        public int AlphaCutoffCount;
        public int AORemapMaxCount;
        public int AORemapMinCount;
        public int DetailAlbedoScaleCount;
        public int DetailNormalScaleCount;
        public int DetailSmoothnessScaleCount;
        public int DiffusionProfileHashCount;
        public int MetallicCount;
        public int SmoothnessCount;
        public int SmoothnessRemapMaxCount;
        public int SmoothnessRemapMinCount;
        public int ThicknessCount;
        public int EmissiveColorCount;
        public int BaseColorCount;
        public int SpecularColorCount;
        public int ThicknessRemapCount;
        public int UnlitColorCount;
    }
}
#endif
