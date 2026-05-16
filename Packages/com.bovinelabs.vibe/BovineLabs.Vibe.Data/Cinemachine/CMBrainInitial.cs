// <copyright file="CMBrainInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures Cinemachine brain values when the track activates.
    /// </summary>
    public struct CMBrainInitial : IInitial<CMBrain>
    {
        /// <inheritdoc/>
        public CMBrain Value { get; set; }
    }
}
#endif
