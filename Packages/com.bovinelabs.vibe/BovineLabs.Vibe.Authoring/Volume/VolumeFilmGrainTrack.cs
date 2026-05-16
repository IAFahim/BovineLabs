// <copyright file="VolumeFilmGrainTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends film grain clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(VolumeFilmGrainClip))]
    [TrackClipType(typeof(VolumeFilmGrainInitialClip))]
    [TrackBindingType(typeof(Volume))]
    [TrackColor(0.4f, 0.6f, 0.7f)]
    [DisplayName("DOTS/Rendering/Volume/Film Grain Track")]
    public class VolumeFilmGrainTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<VolumeFilmGrainInitial>(context.TrackEntity);
        }
    }
}
#endif
#endif
