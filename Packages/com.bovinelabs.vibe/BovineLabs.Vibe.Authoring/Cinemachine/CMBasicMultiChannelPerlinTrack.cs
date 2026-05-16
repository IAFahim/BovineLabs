// <copyright file="CMBasicMultiChannelPerlinTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends Cinemachine basic multi channel perlin clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMBasicMultiChannelPerlinClip))]
    [TrackClipType(typeof(CMBasicMultiChannelPerlinInitialClip))]
    [TrackBindingType(typeof(CinemachineBasicMultiChannelPerlin))]
    [TrackColor(0.9f, 0.5f, 0.15f)]
    [DisplayName("DOTS/Cinemachine/Basic Multi Channel Perlin Track")]
    public class CMBasicMultiChannelPerlinTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMBasicMultiChannelPerlinInitial>(context.TrackEntity);
        }
    }
}
#endif
