// <copyright file="CMPositionComposerTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Cinemachine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that blends Cinemachine position composer clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMPositionComposerClip))]
    [TrackClipType(typeof(CMPositionComposerInitialClip))]
    [TrackBindingType(typeof(CinemachinePositionComposer))]
    [TrackColor(0.25f, 0.55f, 0.85f)]
    [DisplayName("DOTS/Cinemachine/Position Composer Track")]
    public class CMPositionComposerTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMPositionComposerInitial>(context.TrackEntity);
        }
    }
}
#endif
