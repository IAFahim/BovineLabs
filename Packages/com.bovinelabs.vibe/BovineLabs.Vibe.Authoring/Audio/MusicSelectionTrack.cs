// <copyright file="MusicSelectionTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Audio
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Audio;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that selects music tracks by id.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(MusicSelectionClip))]
    [TrackColor(0.15f, 0.55f, 0.35f)]
    [DisplayName("DOTS/Audio/Music Track")]
    public class MusicSelectionTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<MusicSelectionInitial>(context.TrackEntity);
        }
    }
}
#endif
