// <copyright file="CMThirdPersonFollowInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
#if UNITY_PHYSICS
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;

    /// <summary>
    /// Stores Cinemachine third person follow data captured when the track activates.
    /// </summary>
    public struct CMThirdPersonFollowInitial : IInitial<CMThirdPersonFollow>
    {
        public CMThirdPersonFollow Value { get; set; }
    }
}
#endif
#endif
