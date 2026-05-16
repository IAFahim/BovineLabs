// <copyright file="CMOrbitFollowTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends Cinemachine orbit follow clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMOrbitFollowClip))]
    [TrackClipType(typeof(CMOrbitFollowInitialClip))]
    [TrackBindingType(typeof(CinemachineOrbitalFollow))]
    [TrackColor(0.35f, 0.7f, 0.55f)]
    [DisplayName("DOTS/Cinemachine/Orbit Follow Track")]
    public class CMOrbitFollowTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMOrbitFollowInitial>(context.TrackEntity);
        }
    }
}
#endif
