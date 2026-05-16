// <copyright file="VolumeBloomTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends bloom clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(VolumeBloomClip))]
    [TrackClipType(typeof(VolumeBloomInitialClip))]
    [TrackBindingType(typeof(Volume))]
    [TrackColor(0.4f, 0.6f, 0.7f)]
    [DisplayName("DOTS/Rendering/Volume/Bloom Track")]
    public class VolumeBloomTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<VolumeBloomInitial>(context.TrackEntity);
        }
    }
}
#endif
#endif
