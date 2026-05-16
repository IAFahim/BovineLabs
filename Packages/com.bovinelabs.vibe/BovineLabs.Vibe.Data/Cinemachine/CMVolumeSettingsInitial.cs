// <copyright file="CMVolumeSettingsInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures Cinemachine volume settings values when the track activates.
    /// </summary>
    public struct CMVolumeSettingsInitial : IInitial<CMVolumeSettings>
    {
        /// <inheritdoc/>
        public CMVolumeSettings Value { get; set; }
    }
}
#endif
