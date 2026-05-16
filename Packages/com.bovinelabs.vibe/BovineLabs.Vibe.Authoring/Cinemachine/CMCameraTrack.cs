// <copyright file="CMCameraTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends Cinemachine camera lens clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMCameraClip))]
    [TrackClipType(typeof(CMCameraInitialClip))]
    [TrackBindingType(typeof(CinemachineCamera))]
    [TrackColor(0.2f, 0.4f, 0.8f)]
    [DisplayName("DOTS/Cinemachine/Camera Track")]
    public class CMCameraTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMCameraInitial>(context.TrackEntity);
        }
    }
}
#endif
