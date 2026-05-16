// <copyright file="CMCameraOffsetTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends Cinemachine camera offset clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMCameraOffsetClip))]
    [TrackClipType(typeof(CMCameraOffsetInitialClip))]
    [TrackBindingType(typeof(CinemachineCameraOffset))]
    [TrackColor(0.2f, 0.6f, 0.75f)]
    [DisplayName("DOTS/Cinemachine/Camera Offset Track")]
    public class CMCameraOffsetTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMCameraOffsetInitial>(context.TrackEntity);
        }
    }
}
#endif
