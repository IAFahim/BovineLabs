// <copyright file="TimeScaleTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.Time
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Time;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that drives the global time scale.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(TimeScaleClip))]
    [TrackColor(0.5f, 0.35f, 0.1f)]
    [DisplayName("DOTS/Time/Time Scale Track")]
    public class TimeScaleTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<TimeScaleInitial>(context.TrackEntity);
        }
    }
}
