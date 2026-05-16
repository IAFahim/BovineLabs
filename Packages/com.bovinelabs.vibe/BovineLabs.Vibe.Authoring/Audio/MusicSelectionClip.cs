// <copyright file="MusicSelectionClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Audio
{
    using System;
    using BovineLabs.Bridge.Authoring.Audio;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Audio;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that selects a music track by id.
    /// </summary>
    [Serializable]
    public class MusicSelectionClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        private MusicTrackDefinition track;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.None;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            var trackId = this.track != null ? this.track.Id : 0;
            context.Baker.AddComponent(clipEntity, new MusicSelectionClipData { TrackId = trackId });
        }
    }
}
#endif
