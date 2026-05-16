// <copyright file="AnchorNavClipBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ANCHOR
namespace BovineLabs.Vibe.Authoring.UI
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.UI;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Base implementation for Anchor navigation timeline clips.
    /// </summary>
    [Serializable]
    public abstract class AnchorNavClipBase : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.None;

        /// <inheritdoc/>
        public sealed override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);
            context.Baker.AddComponent(clipEntity, this.BuildClipData());
        }

        /// <summary>
        /// Builds the clip data baked for this navigation clip.
        /// </summary>
        protected abstract AnchorNavClipData BuildClipData();
    }
}
#endif
