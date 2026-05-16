// <copyright file="TimelineCancelTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Timeline
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that stops the current timeline when input is detected during the clip.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(TimelineCancelClip))]
    [TrackColor(0.9f, 0.2f, 0.2f)]
    [DisplayName("DOTS/Timeline/Cancel Track")]
    public class TimelineCancelTrack : DOTSTrack
    {
    }
}
#endif
