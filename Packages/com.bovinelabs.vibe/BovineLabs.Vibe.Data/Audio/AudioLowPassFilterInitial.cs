// <copyright file="AudioLowPassFilterInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures the initial audio low-pass filter data when the track activates.
    /// </summary>
    public struct AudioLowPassFilterInitial : IInitial<AudioLowPassFilterData>
    {
        /// <inheritdoc/>
        public AudioLowPassFilterData Value { get; set; }
    }
}
#endif
