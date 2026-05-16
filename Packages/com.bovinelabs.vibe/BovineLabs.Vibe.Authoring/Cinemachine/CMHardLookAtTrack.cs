// <copyright file="CMHardLookAtTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends hard-look-at clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMHardLookAtClip))]
    [TrackClipType(typeof(CMHardLookAtInitialClip))]
    [TrackBindingType(typeof(CinemachineHardLookAt))]
    [TrackColor(0.6f, 0.45f, 0.85f)]
    [DisplayName("DOTS/Cinemachine/Hard Look At Track")]
    public class CMHardLookAtTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMHardLookAtInitial>(context.TrackEntity);
        }
    }
}
#endif
