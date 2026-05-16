// <copyright file="CMCameraOffsetInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;

    /// <summary>
    /// Stores Cinemachine camera offset data captured when the track activates.
    /// </summary>
    public struct CMCameraOffsetInitial : IInitial<CMCameraOffset>
    {
        public CMCameraOffset Value { get; set; }
    }
}
#endif
