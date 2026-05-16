// <copyright file="CMGroupFramingTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends Cinemachine group framing clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMGroupFramingClip))]
    [TrackClipType(typeof(CMGroupFramingInitialClip))]
    [TrackBindingType(typeof(CinemachineGroupFraming))]
    [TrackColor(0.3f, 0.65f, 0.85f)]
    [DisplayName("DOTS/Cinemachine/Group Framing Track")]
    public class CMGroupFramingTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMGroupFramingInitial>(context.TrackEntity);
        }
    }
}
#endif
