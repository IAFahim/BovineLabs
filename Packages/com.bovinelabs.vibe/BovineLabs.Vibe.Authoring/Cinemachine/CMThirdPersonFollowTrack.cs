// <copyright file="CMThirdPersonFollowTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
#if UNITY_PHYSICS
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that blends Cinemachine third person follow clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMThirdPersonFollowClip))]
    [TrackClipType(typeof(CMThirdPersonFollowInitialClip))]
    [TrackBindingType(typeof(CinemachineThirdPersonFollowDots))]
    [TrackColor(0.75f, 0.45f, 0.25f)]
    [DisplayName("DOTS/Cinemachine/Third Person Follow Track")]
    public class CMThirdPersonFollowTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMThirdPersonFollowInitial>(context.TrackEntity);
        }
    }
}
#endif
#endif
