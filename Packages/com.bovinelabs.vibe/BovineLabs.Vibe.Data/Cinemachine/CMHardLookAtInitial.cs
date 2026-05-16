// <copyright file="CMHardLookAtInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures hard-look-at values when the track activates.
    /// </summary>
    public struct CMHardLookAtInitial : IInitial<CMHardLookAt>
    {
        /// <inheritdoc/>
        public CMHardLookAt Value { get; set; }
    }
}
#endif
