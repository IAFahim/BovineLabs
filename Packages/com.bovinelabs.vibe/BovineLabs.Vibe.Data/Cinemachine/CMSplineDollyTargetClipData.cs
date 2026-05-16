// <copyright file="CMSplineDollyTargetClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Entities;

    /// <summary>
    /// Serialized configuration baked for each spline dolly target clip.
    /// </summary>
    public struct CMSplineDollyTargetClipData : IComponentData
    {
        public Entity Spline;
    }
}
#endif
