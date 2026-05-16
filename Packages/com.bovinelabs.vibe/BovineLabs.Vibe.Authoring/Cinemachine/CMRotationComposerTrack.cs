// <copyright file="CMRotationComposerTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends Cinemachine rotation composer clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMRotationComposerClip))]
    [TrackClipType(typeof(CMRotationComposerInitialClip))]
    [TrackBindingType(typeof(CinemachineRotationComposer))]
    [TrackColor(0.4f, 0.5f, 0.85f)]
    [DisplayName("DOTS/Cinemachine/Rotation Composer Track")]
    public class CMRotationComposerTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMRotationComposerInitial>(context.TrackEntity);
        }
    }
}
#endif
