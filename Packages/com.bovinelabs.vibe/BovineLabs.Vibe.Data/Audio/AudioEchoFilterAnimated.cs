// <copyright file="AudioEchoFilterAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Blended audio echo filter values.
    /// </summary>
    public struct AudioEchoFilterBlend
    {
        public float Delay;
        public float DecayRatio;
        public float WetMix;
        public float DryMix;
    }

    /// <summary>
    /// Runtime state stored per clip for audio echo filter blending.
    /// </summary>
    public struct AudioEchoFilterAnimated : IAnimatedComponent<AudioEchoFilterBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public AudioEchoFilterBlend Value { get; set; }
    }
}
#endif
