// <copyright file="CMRecomposerTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends Cinemachine recomposer clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMRecomposerClip))]
    [TrackClipType(typeof(CMRecomposerInitialClip))]
    [TrackBindingType(typeof(CinemachineRecomposer))]
    [TrackColor(0.85f, 0.55f, 0.3f)]
    [DisplayName("DOTS/Cinemachine/Recomposer Track")]
    public class CMRecomposerTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMRecomposerInitial>(context.TrackEntity);
        }
    }
}
#endif
