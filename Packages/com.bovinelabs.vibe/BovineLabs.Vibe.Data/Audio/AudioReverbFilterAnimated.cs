// <copyright file="AudioReverbFilterAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Blended audio reverb filter values.
    /// </summary>
    public struct AudioReverbFilterBlend
    {
        public float DryLevel;
        public float Room;
        public float RoomHF;
        public float RoomLF;
        public float DecayTime;
        public float DecayHFRatio;
        public float ReflectionsLevel;
        public float ReflectionsDelay;
        public float ReverbLevel;
        public float ReverbDelay;
        public float HFReference;
        public float LFReference;
        public float Diffusion;
        public float Density;
    }

    /// <summary>
    /// Runtime state stored per clip for audio reverb filter blending.
    /// </summary>
    public struct AudioReverbFilterAnimated : IAnimatedComponent<AudioReverbFilterBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public AudioReverbFilterBlend Value { get; set; }
    }
}
#endif
