// <copyright file="CMThirdPersonFollowInitialClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
#if UNITY_PHYSICS
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Clip that restores the Cinemachine third person follow to its captured initial values.
    /// </summary>
    [Serializable]
    public class CMThirdPersonFollowInitialClip : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CMThirdPersonFollowAnimated>(clipEntity);
            context.Baker.AddComponent(clipEntity, new CMThirdPersonFollowClipData { Type = CMThirdPersonFollowClipType.Initial });
        }
    }
}
#endif
#endif
