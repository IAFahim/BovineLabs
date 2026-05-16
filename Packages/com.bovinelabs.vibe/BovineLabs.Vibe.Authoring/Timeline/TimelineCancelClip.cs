// <copyright file="TimelineCancelClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Timeline
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Timeline;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that cancels the current timeline when input is detected.
    /// </summary>
    [Serializable]
    public class TimelineCancelClip : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.None;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);
            context.Baker.AddComponent<TimelineCancelClipData>(clipEntity);
        }
    }
}
#endif
