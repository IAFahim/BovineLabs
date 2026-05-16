// <copyright file="VolumeDepthOfFieldTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Volume
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Volume;
    using UnityEngine.Rendering;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that blends depth of field clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(VolumeDepthOfFieldClip))]
    [TrackClipType(typeof(VolumeDepthOfFieldInitialClip))]
    [TrackBindingType(typeof(Volume))]
    [TrackColor(0.4f, 0.6f, 0.7f)]
    [DisplayName("DOTS/Rendering/Volume/Depth Of Field Track")]
    public class VolumeDepthOfFieldTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<VolumeDepthOfFieldInitial>(context.TrackEntity);
        }
    }
}
#endif
#endif
