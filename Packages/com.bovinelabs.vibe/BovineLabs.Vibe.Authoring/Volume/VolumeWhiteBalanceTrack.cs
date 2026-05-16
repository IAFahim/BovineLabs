// <copyright file="VolumeWhiteBalanceTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends white balance clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(VolumeWhiteBalanceClip))]
    [TrackClipType(typeof(VolumeWhiteBalanceInitialClip))]
    [TrackBindingType(typeof(Volume))]
    [TrackColor(0.4f, 0.6f, 0.7f)]
    [DisplayName("DOTS/Rendering/Volume/White Balance Track")]
    public class VolumeWhiteBalanceTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<VolumeWhiteBalanceInitial>(context.TrackEntity);
        }
    }
}
#endif
#endif
