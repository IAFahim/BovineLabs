// <copyright file="CMFollowTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends Cinemachine follow clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMFollowClip))]
    [TrackClipType(typeof(CMFollowInitialClip))]
    [TrackBindingType(typeof(CinemachineFollow))]
    [TrackColor(0.25f, 0.7f, 0.6f)]
    [DisplayName("DOTS/Cinemachine/Follow Track")]
    public class CMFollowTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMFollowInitial>(context.TrackEntity);
        }
    }
}
#endif
