// <copyright file="RukhankaAnimatorStateTrack.cs" company="BovineLabs">
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
    /// Timeline track that drives Rukhanka animator state changes.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(RukhankaAnimatorCrossfadeClip))]
    [TrackClipType(typeof(RukhankaAnimatorPlayStateClip))]
    [TrackBindingType(typeof(Animator))]
    [TrackColor(0.35f, 0.55f, 0.85f)]
    [DisplayName("DOTS/Rukhanka/Animator State Track")]
    public class RukhankaAnimatorStateTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            var layerUsages = context.Baker.AddBuffer<RukhankaAnimatorStateLayerUsage>(context.TrackEntity);
            context.Baker.AddBuffer<RukhankaAnimatorStateLayerInitial>(context.TrackEntity);

            var animator = RukhankaAuthoringUtility.ResolveBindingAnimator(context);

            foreach (var clipInfo in context.SharedContextValues.ClipEntities)
            {
                switch (clipInfo.Clip.asset)
                {
                    case RukhankaAnimatorCrossfadeClip crossfadeClip:
                        crossfadeClip.AddTrackLayerUsage(animator, layerUsages);
                        break;
                    case RukhankaAnimatorPlayStateClip playStateClip:
                        playStateClip.AddTrackLayerUsage(animator, layerUsages);
                        break;
                }
            }
        }
    }
}

#endif
