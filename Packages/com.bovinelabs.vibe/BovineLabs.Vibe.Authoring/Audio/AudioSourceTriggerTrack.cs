// <copyright file="AudioSourceTriggerTrack.cs" company="BovineLabs">
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
    /// Timeline track that triggers audio source playback actions.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(AudioSourceTriggerClip))]
    [TrackBindingType(typeof(AudioSource))]
    [TrackColor(0.15f, 0.6f, 0.9f)]
    [DisplayName("DOTS/Audio/Audio Source Trigger Track")]
    public class AudioSourceTriggerTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<AudioSourceDataInitial>(context.TrackEntity);
            context.Baker.AddComponent<AudioSourceClipInitial>(context.TrackEntity);
        }
    }
}
#endif
