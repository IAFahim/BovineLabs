// <copyright file="CMHardLockToTargetTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends hard-lock-to-target clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMHardLockToTargetClip))]
    [TrackClipType(typeof(CMHardLockToTargetInitialClip))]
    [TrackBindingType(typeof(CinemachineHardLockToTarget))]
    [TrackColor(0.8f, 0.35f, 0.3f)]
    [DisplayName("DOTS/Cinemachine/Hard Lock To Target Track")]
    public class CMHardLockToTargetTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMHardLockToTargetInitial>(context.TrackEntity);
        }
    }
}
#endif
