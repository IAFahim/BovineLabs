// <copyright file="AudioEchoFilterTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends audio echo filter clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(AudioEchoFilterClip))]
    [TrackClipType(typeof(AudioEchoFilterSweepClip))]
    [TrackClipType(typeof(AudioEchoFilterInitialClip))]
    [TrackBindingType(typeof(AudioEchoFilter))]
    [TrackColor(0.4f, 0.5f, 0.75f)]
    [DisplayName("DOTS/Audio/Echo Filter Track")]
    public class AudioEchoFilterTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<AudioEchoFilterInitial>(context.TrackEntity);
        }
    }
}
#endif
