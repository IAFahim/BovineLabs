// <copyright file="VolumeScreenSpaceLensFlareTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends screen space lens flare clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(VolumeScreenSpaceLensFlareClip))]
    [TrackClipType(typeof(VolumeScreenSpaceLensFlareInitialClip))]
    [TrackBindingType(typeof(Volume))]
    [TrackColor(0.4f, 0.6f, 0.7f)]
    [DisplayName("DOTS/Rendering/Volume/Screen Space Lens Flare Track")]
    public class VolumeScreenSpaceLensFlareTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<VolumeScreenSpaceLensFlareInitial>(context.TrackEntity);
        }
    }
}
#endif
#endif
