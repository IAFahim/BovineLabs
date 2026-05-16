// <copyright file="AudioLowPassFilterTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Audio
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Audio;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that blends audio low-pass filter clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(AudioLowPassFilterClip))]
    [TrackClipType(typeof(AudioLowPassFilterSweepClip))]
    [TrackClipType(typeof(AudioLowPassFilterInitialClip))]
    [TrackBindingType(typeof(AudioLowPassFilter))]
    [TrackColor(0.18f, 0.7f, 0.5f)]
    [DisplayName("DOTS/Audio/Low Pass Filter Track")]
    public class AudioLowPassFilterTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<AudioLowPassFilterInitial>(context.TrackEntity);
        }
    }
}
#endif
