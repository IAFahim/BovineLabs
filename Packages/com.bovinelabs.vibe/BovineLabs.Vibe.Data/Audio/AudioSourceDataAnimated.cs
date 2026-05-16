// <copyright file="AudioSourceDataAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Blended audio source data values.
    /// </summary>
    public struct AudioSourceDataBlend
    {
        public float Volume;
        public float Pitch;
        public bool EnableVolume;
        public bool EnablePitch;
    }

    /// <summary>
    /// Runtime state stored per clip for audio source data blending.
    /// </summary>
    public struct AudioSourceDataAnimated : IAnimatedComponent<AudioSourceDataBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public AudioSourceDataBlend Value { get; set; }
    }
}
#endif
