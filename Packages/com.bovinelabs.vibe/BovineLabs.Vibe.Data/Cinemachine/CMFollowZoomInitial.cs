// <copyright file="CMFollowZoomInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;

    /// <summary>
    /// Stores Cinemachine follow zoom data captured when the track activates.
    /// </summary>
    public struct CMFollowZoomInitial : IInitial<CMFollowZoom>
    {
        public CMFollowZoom Value { get; set; }
    }
}
#endif
