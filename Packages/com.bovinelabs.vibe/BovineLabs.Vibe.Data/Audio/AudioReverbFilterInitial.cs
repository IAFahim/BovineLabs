// <copyright file="AudioReverbFilterInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures the initial audio reverb filter data when the track activates.
    /// </summary>
    public struct AudioReverbFilterInitial : IInitial<AudioReverbFilterData>
    {
        /// <inheritdoc/>
        public AudioReverbFilterData Value { get; set; }
    }
}
#endif
