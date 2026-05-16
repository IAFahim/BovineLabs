// <copyright file="AudioEchoFilterInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures the initial audio echo filter data when the track activates.
    /// </summary>
    public struct AudioEchoFilterInitial : IInitial<AudioEchoFilterData>
    {
        /// <inheritdoc/>
        public AudioEchoFilterData Value { get; set; }
    }
}
#endif
