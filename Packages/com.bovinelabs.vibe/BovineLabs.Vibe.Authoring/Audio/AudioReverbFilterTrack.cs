// <copyright file="AudioReverbFilterTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends audio reverb filter clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(AudioReverbFilterClip))]
    [TrackClipType(typeof(AudioReverbFilterSweepClip))]
    [TrackClipType(typeof(AudioReverbFilterInitialClip))]
    [TrackBindingType(typeof(AudioReverbFilter))]
    [TrackColor(0.55f, 0.4f, 0.7f)]
    [DisplayName("DOTS/Audio/Reverb Filter Track")]
    public class AudioReverbFilterTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<AudioReverbFilterInitial>(context.TrackEntity);
        }
    }
}
#endif
