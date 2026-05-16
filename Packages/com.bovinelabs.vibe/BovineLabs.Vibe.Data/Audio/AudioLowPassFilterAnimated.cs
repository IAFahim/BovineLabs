// <copyright file="AudioLowPassFilterAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Blended audio low-pass filter values.
    /// </summary>
    public struct AudioLowPassFilterBlend
    {
        public float CutoffFrequency;
        public float LowpassResonanceQ;
    }

    /// <summary>
    /// Runtime state stored per clip for audio low-pass filter blending.
    /// </summary>
    public struct AudioLowPassFilterAnimated : IAnimatedComponent<AudioLowPassFilterBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public AudioLowPassFilterBlend Value { get; set; }
    }
}
#endif
