// <copyright file="RukhankaAnimatorSpeedTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Authoring.Animation
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Animation;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that drives animator speed for Rukhanka animators.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(RukhankaAnimatorSpeedClip))]
    [TrackBindingType(typeof(Animator))]
    [TrackColor(0.8f, 0.5f, 0.2f)]
    [DisplayName("DOTS/Rukhanka/Animator Speed Track")]
    public class RukhankaAnimatorSpeedTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddBuffer<RukhankaAnimatorSpeedInitial>(context.TrackEntity);
        }
    }
}

#endif
