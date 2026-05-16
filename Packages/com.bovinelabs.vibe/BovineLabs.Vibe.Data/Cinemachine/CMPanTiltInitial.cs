// <copyright file="CMPanTiltInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;

    /// <summary>
    /// Stores Cinemachine pan/tilt data captured when the track activates.
    /// </summary>
    public struct CMPanTiltInitial : IInitial<CMPanTilt>
    {
        public CMPanTilt Value { get; set; }
    }
}
#endif
