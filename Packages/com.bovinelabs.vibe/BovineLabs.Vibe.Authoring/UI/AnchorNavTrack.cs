// <copyright file="AnchorNavTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ANCHOR
namespace BovineLabs.Vibe.Authoring.UI
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.UI;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that drives Anchor navigation actions.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(NavigateClip))]
    [TrackClipType(typeof(ClearNavigateClip))]
    [TrackClipType(typeof(ClearBackStackClip))]
    [TrackClipType(typeof(PopBackStackClip))]
    [TrackClipType(typeof(PopBackStackToPanelClip))]
    [TrackClipType(typeof(CloseAllPopupsClip))]
    [TrackClipType(typeof(ClosePopupClip))]
    [TrackColor(0.2f, 0.7f, 0.5f)]
    [DisplayName("DOTS/UI/Anchor Nav Track")]
    public class AnchorNavTrack : DOTSTrack
    {
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<AnchorNavTrackInitial>(context.TrackEntity);
        }
    }
}
#endif
