// <copyright file="CMOrbitFollowInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;

    /// <summary>
    /// Stores Cinemachine orbit follow data captured when the track activates.
    /// </summary>
    public struct CMOrbitFollowInitial : IInitial<CMOrbitFollow>
    {
        public CMOrbitFollow Value { get; set; }
    }
}
#endif
