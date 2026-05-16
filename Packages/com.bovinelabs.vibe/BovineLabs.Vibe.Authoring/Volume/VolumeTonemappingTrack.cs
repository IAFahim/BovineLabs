// <copyright file="VolumeTonemappingTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends tonemapping clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(VolumeTonemappingClip))]
    [TrackClipType(typeof(VolumeTonemappingInitialClip))]
    [TrackBindingType(typeof(Volume))]
    [TrackColor(0.4f, 0.6f, 0.7f)]
    [DisplayName("DOTS/Rendering/Volume/Tonemapping Track")]
    public class VolumeTonemappingTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<VolumeTonemappingInitial>(context.TrackEntity);
        }
    }
}
#endif
#endif
