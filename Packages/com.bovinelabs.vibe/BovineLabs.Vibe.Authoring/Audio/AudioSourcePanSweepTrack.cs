// <copyright file="AudioSourcePanSweepTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends audio source pan sweep clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(AudioSourcePanSweepClip))]
    [TrackBindingType(typeof(AudioSource))]
    [TrackColor(0.2f, 0.55f, 0.7f)]
    [DisplayName("DOTS/Audio/Audio Source Pan Sweep Track")]
    public class AudioSourcePanSweepTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<AudioSourcePanInitial>(context.TrackEntity);
        }
    }
}
#endif
