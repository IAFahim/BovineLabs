// <copyright file="VolumePaniniProjectionTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends panini projection clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(VolumePaniniProjectionClip))]
    [TrackClipType(typeof(VolumePaniniProjectionInitialClip))]
    [TrackBindingType(typeof(Volume))]
    [TrackColor(0.4f, 0.6f, 0.7f)]
    [DisplayName("DOTS/Rendering/Volume/Panini Projection Track")]
    public class VolumePaniniProjectionTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<VolumePaniniProjectionInitial>(context.TrackEntity);
        }
    }
}
#endif
#endif
