// <copyright file="CMOrbitFollowInitialClip.cs" company="BovineLabs">
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
    /// Clip that restores the Cinemachine orbit follow to its captured initial values.
    /// </summary>
    [Serializable]
    public class CMOrbitFollowInitialClip : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CMOrbitFollowAnimated>(clipEntity);
            context.Baker.AddComponent(clipEntity, new CMOrbitFollowClipData { Type = CMOrbitFollowClipType.Initial });
        }
    }
}
#endif
