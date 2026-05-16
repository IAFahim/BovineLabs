// <copyright file="VolumeLensDistortionTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends lens distortion clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(VolumeLensDistortionClip))]
    [TrackClipType(typeof(VolumeLensDistortionInitialClip))]
    [TrackBindingType(typeof(Volume))]
    [TrackColor(0.4f, 0.6f, 0.7f)]
    [DisplayName("DOTS/Rendering/Volume/Lens Distortion Track")]
    public class VolumeLensDistortionTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<VolumeLensDistortionInitial>(context.TrackEntity);
        }
    }
}
#endif
#endif
