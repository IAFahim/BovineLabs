// <copyright file="AudioChorusFilterAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Blended audio chorus filter values.
    /// </summary>
    public struct AudioChorusFilterBlend
    {
        public float DryMix;
        public float WetMix1;
        public float WetMix2;
        public float WetMix3;
        public float Delay;
        public float Rate;
        public float Depth;
    }

    /// <summary>
    /// Runtime state stored per clip for audio chorus filter blending.
    /// </summary>
    public struct AudioChorusFilterAnimated : IAnimatedComponent<AudioChorusFilterBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public AudioChorusFilterBlend Value { get; set; }
    }
}
#endif
