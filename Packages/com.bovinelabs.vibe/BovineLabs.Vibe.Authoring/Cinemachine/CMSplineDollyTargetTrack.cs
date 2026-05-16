// <copyright file="CMSplineDollyTargetTrack.cs" company="BovineLabs">
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
    /// Timeline track that assigns Cinemachine spline dolly targets without blending.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMSplineDollyTargetClip))]
    [TrackBindingType(typeof(CinemachineSplineDolly))]
    [TrackColor(0.25f, 0.6f, 0.7f)]
    [DisplayName("DOTS/Cinemachine/Spline Dolly Target Track")]
    public class CMSplineDollyTargetTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMSplineDollyTargetInitial>(context.TrackEntity);
        }
    }
}
#endif
