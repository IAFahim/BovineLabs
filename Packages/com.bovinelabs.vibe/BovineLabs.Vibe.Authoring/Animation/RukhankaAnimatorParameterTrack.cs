// <copyright file="RukhankaAnimatorParameterTrack.cs" company="BovineLabs">
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
    /// Timeline track that applies animator parameter changes for Rukhanka animators.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(RukhankaAnimatorParameterClip))]
    [TrackBindingType(typeof(Animator))]
    [TrackColor(0.3f, 0.7f, 0.45f)]
    [DisplayName("DOTS/Rukhanka/Animator Parameter Track")]
    public class RukhankaAnimatorParameterTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            var parameterHashes = context.Baker.AddBuffer<RukhankaAnimatorParameterTrackHash>(context.TrackEntity);
            var layerIndices = context.Baker.AddBuffer<RukhankaAnimatorLayerIndex>(context.TrackEntity);

            context.Baker.AddBuffer<RukhankaAnimatorParameterInitial>(context.TrackEntity);
            context.Baker.AddBuffer<RukhankaAnimatorLayerInitial>(context.TrackEntity);

            var animator = RukhankaAuthoringUtility.ResolveBindingAnimator(context);

            foreach (var clipInfo in context.SharedContextValues.ClipEntities)
            {
                if (clipInfo.Clip.asset is not RukhankaAnimatorParameterClip parameterClip)
                {
                    continue;
                }

                parameterClip.AddTrackHashes(animator, parameterHashes, layerIndices);
            }
        }
    }
}

#endif