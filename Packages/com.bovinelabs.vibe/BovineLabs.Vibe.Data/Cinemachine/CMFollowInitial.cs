// <copyright file="CMFollowInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;

    /// <summary>
    /// Stores Cinemachine follow data captured when the track activates.
    /// </summary>
    public struct CMFollowInitial : IInitial<CMFollow>
    {
        public CMFollow Value { get; set; }
    }
}
#endif
