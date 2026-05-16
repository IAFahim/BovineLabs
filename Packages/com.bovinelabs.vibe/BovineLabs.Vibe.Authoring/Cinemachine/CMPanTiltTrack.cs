// <copyright file="CMPanTiltTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends Cinemachine pan/tilt clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMPanTiltClip))]
    [TrackClipType(typeof(CMPanTiltInitialClip))]
    [TrackBindingType(typeof(CinemachinePanTilt))]
    [TrackColor(0.45f, 0.5f, 0.85f)]
    [DisplayName("DOTS/Cinemachine/Pan Tilt Track")]
    public class CMPanTiltTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMPanTiltInitial>(context.TrackEntity);
        }
    }
}
#endif
