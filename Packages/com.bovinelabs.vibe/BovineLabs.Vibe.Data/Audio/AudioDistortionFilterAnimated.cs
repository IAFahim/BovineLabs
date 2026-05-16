// <copyright file="AudioDistortionFilterAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Blended audio distortion filter values.
    /// </summary>
    public struct AudioDistortionFilterBlend
    {
        public float DistortionLevel;
    }

    /// <summary>
    /// Runtime state stored per clip for audio distortion filter blending.
    /// </summary>
    public struct AudioDistortionFilterAnimated : IAnimatedComponent<AudioDistortionFilterBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public AudioDistortionFilterBlend Value { get; set; }
    }
}
#endif
