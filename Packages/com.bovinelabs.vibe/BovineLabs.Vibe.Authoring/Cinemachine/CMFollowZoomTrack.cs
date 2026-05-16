// <copyright file="CMFollowZoomTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends Cinemachine follow zoom clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMFollowZoomClip))]
    [TrackClipType(typeof(CMFollowZoomInitialClip))]
    [TrackBindingType(typeof(CinemachineFollowZoom))]
    [TrackColor(0.55f, 0.65f, 0.3f)]
    [DisplayName("DOTS/Cinemachine/Follow Zoom Track")]
    public class CMFollowZoomTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMFollowZoomInitial>(context.TrackEntity);
        }
    }
}
#endif
