// <copyright file="AudioHighPassFilterAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Blended audio high-pass filter values.
    /// </summary>
    public struct AudioHighPassFilterBlend
    {
        public float CutoffFrequency;
        public float HighpassResonanceQ;
    }

    /// <summary>
    /// Runtime state stored per clip for audio high-pass filter blending.
    /// </summary>
    public struct AudioHighPassFilterAnimated : IAnimatedComponent<AudioHighPassFilterBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public AudioHighPassFilterBlend Value { get; set; }
    }
}
#endif
