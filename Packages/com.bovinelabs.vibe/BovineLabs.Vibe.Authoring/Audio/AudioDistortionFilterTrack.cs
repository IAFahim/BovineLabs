// <copyright file="AudioDistortionFilterTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends audio distortion filter clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(AudioDistortionFilterClip))]
    [TrackClipType(typeof(AudioDistortionFilterSweepClip))]
    [TrackClipType(typeof(AudioDistortionFilterInitialClip))]
    [TrackBindingType(typeof(AudioDistortionFilter))]
    [TrackColor(0.65f, 0.35f, 0.3f)]
    [DisplayName("DOTS/Audio/Distortion Filter Track")]
    public class AudioDistortionFilterTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<AudioDistortionFilterInitial>(context.TrackEntity);
        }
    }
}
#endif
