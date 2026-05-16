// <copyright file="AudioSourceClipTrack.cs" company="BovineLabs">
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
    /// Timeline track that assigns clips to audio sources without blending.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(AudioSourceClip))]
    [TrackBindingType(typeof(AudioSource))]
    [TrackColor(0.1f, 0.45f, 0.75f)]
    [DisplayName("DOTS/Audio/Audio Source Clip Track")]
    public class AudioSourceClipTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<AudioSourceClipInitial>(context.TrackEntity);
        }
    }
}
#endif
