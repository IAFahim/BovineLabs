// <copyright file="VolumeShadowsMidtonesHighlightsTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends shadows/midtones/highlights clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(VolumeShadowsMidtonesHighlightsClip))]
    [TrackClipType(typeof(VolumeShadowsMidtonesHighlightsInitialClip))]
    [TrackBindingType(typeof(Volume))]
    [TrackColor(0.4f, 0.6f, 0.7f)]
    [DisplayName("DOTS/Rendering/Volume/Shadows Midtones Highlights Track")]
    public class VolumeShadowsMidtonesHighlightsTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<VolumeShadowsMidtonesHighlightsInitial>(context.TrackEntity);
        }
    }
}
#endif
#endif
