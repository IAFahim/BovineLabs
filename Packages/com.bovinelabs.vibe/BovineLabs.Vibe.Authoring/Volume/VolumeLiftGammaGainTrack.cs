// <copyright file="VolumeLiftGammaGainTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends lift/gamma/gain clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(VolumeLiftGammaGainClip))]
    [TrackClipType(typeof(VolumeLiftGammaGainInitialClip))]
    [TrackBindingType(typeof(Volume))]
    [TrackColor(0.4f, 0.6f, 0.7f)]
    [DisplayName("DOTS/Rendering/Volume/Lift Gamma Gain Track")]
    public class VolumeLiftGammaGainTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<VolumeLiftGammaGainInitial>(context.TrackEntity);
        }
    }
}
#endif
#endif
