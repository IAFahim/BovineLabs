// <copyright file="CMRotateWithFollowTargetInitialClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Clip that restores rotate-with-follow-target settings to their captured initial values.
    /// </summary>
    [Serializable]
    public class CMRotateWithFollowTargetInitialClip : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CMRotateWithFollowTargetAnimated>(clipEntity);
            context.Baker.AddComponent(
                clipEntity,
                new CMRotateWithFollowTargetClipData { Type = CMRotateWithFollowTargetClipType.Initial });
        }
    }
}
#endif
