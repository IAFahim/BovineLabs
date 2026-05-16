// <copyright file="AudioSourceDataInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures the initial audio source data when the track activates.
    /// </summary>
    public struct AudioSourceDataInitial : IInitial<AudioSourceData>
    {
        /// <inheritdoc/>
        public AudioSourceData Value { get; set; }
    }
}
#endif
