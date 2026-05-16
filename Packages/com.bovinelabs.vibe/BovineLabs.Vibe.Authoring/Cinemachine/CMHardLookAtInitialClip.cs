// <copyright file="CMHardLookAtInitialClip.cs" company="BovineLabs">
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
    /// Clip that restores hard-look-at offsets to their captured initial values.
    /// </summary>
    [Serializable]
    public class CMHardLookAtInitialClip : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CMHardLookAtAnimated>(clipEntity);
            context.Baker.AddComponent(
                clipEntity,
                new CMHardLookAtClipData { Type = CMHardLookAtClipType.Initial });
        }
    }
}
#endif
