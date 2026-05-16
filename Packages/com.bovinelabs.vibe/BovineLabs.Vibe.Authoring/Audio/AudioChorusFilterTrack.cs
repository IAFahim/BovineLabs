// <copyright file="AudioChorusFilterTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends audio chorus filter clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(AudioChorusFilterClip))]
    [TrackClipType(typeof(AudioChorusFilterSweepClip))]
    [TrackClipType(typeof(AudioChorusFilterInitialClip))]
    [TrackBindingType(typeof(AudioChorusFilter))]
    [TrackColor(0.6f, 0.55f, 0.2f)]
    [DisplayName("DOTS/Audio/Chorus Filter Track")]
    public class AudioChorusFilterTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<AudioChorusFilterInitial>(context.TrackEntity);
        }
    }
}
#endif
