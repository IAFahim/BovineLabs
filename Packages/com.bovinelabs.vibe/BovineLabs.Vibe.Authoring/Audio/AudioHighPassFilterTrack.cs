// <copyright file="AudioHighPassFilterTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends audio high-pass filter clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(AudioHighPassFilterClip))]
    [TrackClipType(typeof(AudioHighPassFilterSweepClip))]
    [TrackClipType(typeof(AudioHighPassFilterInitialClip))]
    [TrackBindingType(typeof(AudioHighPassFilter))]
    [TrackColor(0.15f, 0.55f, 0.6f)]
    [DisplayName("DOTS/Audio/High Pass Filter Track")]
    public class AudioHighPassFilterTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<AudioHighPassFilterInitial>(context.TrackEntity);
        }
    }
}
#endif
