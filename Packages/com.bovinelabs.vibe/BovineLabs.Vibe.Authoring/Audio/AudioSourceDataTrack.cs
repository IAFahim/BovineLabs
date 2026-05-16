// <copyright file="AudioSourceDataTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Audio
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Audio;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that blends audio source data clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(AudioSourceDataClip))]
    [TrackClipType(typeof(AudioSourceDataInitialClip))]
    [TrackClipType(typeof(AudioSourceVolumeSweepClip))]
    [TrackClipType(typeof(AudioSourcePitchSweepClip))]
    [TrackBindingType(typeof(AudioSource))]
    [TrackColor(0.2f, 0.6f, 0.8f)]
    [DisplayName("DOTS/Audio/Audio Source Data Track")]
    public class AudioSourceDataTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<AudioSourceDataInitial>(context.TrackEntity);
        }
    }
}
#endif
