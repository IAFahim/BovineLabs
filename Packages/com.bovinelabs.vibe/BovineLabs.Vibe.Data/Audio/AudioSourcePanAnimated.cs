// <copyright file="AudioSourcePanAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using BovineLabs.Timeline.Data;
    using Unity.Entities;
    using Unity.Properties;

    /// <summary>
    /// Runtime state stored per clip for audio source pan blending.
    /// </summary>
    public struct AudioSourcePanAnimated : IAnimatedComponent<float>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public float Value { get; set; }
    }

    /// <summary>
    /// Captures the initial audio source pan when the track activates.
    /// </summary>
    public struct AudioSourcePanInitial : IComponentData
    {
        public float Value;
    }
}
#endif
