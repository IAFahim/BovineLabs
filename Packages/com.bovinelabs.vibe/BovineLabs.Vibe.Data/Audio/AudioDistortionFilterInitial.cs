// <copyright file="AudioDistortionFilterInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures the initial audio distortion filter data when the track activates.
    /// </summary>
    public struct AudioDistortionFilterInitial : IInitial<AudioDistortionFilterData>
    {
        /// <inheritdoc/>
        public AudioDistortionFilterData Value { get; set; }
    }
}
#endif
