// <copyright file="CMBrainTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends Cinemachine brain clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMBrainClip))]
    [TrackClipType(typeof(CMBrainInitialClip))]
    [TrackBindingType(typeof(CinemachineBrain))]
    [TrackColor(0.4f, 0.7f, 0.95f)]
    [DisplayName("DOTS/Cinemachine/Brain Track")]
    public class CMBrainTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMBrainInitial>(context.TrackEntity);
        }
    }
}
#endif
