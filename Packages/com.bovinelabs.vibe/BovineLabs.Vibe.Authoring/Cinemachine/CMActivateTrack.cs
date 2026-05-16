// <copyright file="CMActivateTrack.cs" company="BovineLabs">
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
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that activates a Cinemachine camera without blending.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMActivateClip))]
    [TrackBindingType(typeof(CinemachineCamera))]
    [TrackColor(0.25f, 0.65f, 0.4f)]
    [DisplayName("DOTS/Cinemachine/Activate Track")]
    public class CMActivateTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMCameraActivateInitial>(context.TrackEntity);
        }
    }
}
#endif
