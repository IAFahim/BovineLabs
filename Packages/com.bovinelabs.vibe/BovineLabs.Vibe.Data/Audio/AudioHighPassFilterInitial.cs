// <copyright file="AudioHighPassFilterInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures the initial audio high-pass filter data when the track activates.
    /// </summary>
    public struct AudioHighPassFilterInitial : IInitial<AudioHighPassFilterData>
    {
        /// <inheritdoc/>
        public AudioHighPassFilterData Value { get; set; }
    }
}
#endif
