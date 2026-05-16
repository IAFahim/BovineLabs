// <copyright file="CMRotateWithFollowTargetTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends rotate-with-follow-target clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMRotateWithFollowTargetClip))]
    [TrackClipType(typeof(CMRotateWithFollowTargetInitialClip))]
    [TrackBindingType(typeof(CinemachineRotateWithFollowTarget))]
    [TrackColor(0.85f, 0.55f, 0.2f)]
    [DisplayName("DOTS/Cinemachine/Rotate With Follow Target Track")]
    public class CMRotateWithFollowTargetTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMRotateWithFollowTargetInitial>(context.TrackEntity);
        }
    }
}
#endif
