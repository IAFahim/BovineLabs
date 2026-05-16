// <copyright file="CMFreeLookModifierInitialClip.cs" company="BovineLabs">
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
    /// Clip that restores the Cinemachine free look modifier to its captured initial values.
    /// </summary>
    [Serializable]
    public class CMFreeLookModifierInitialClip : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CMFreeLookModifierAnimated>(clipEntity);
            context.Baker.AddComponent(clipEntity, new CMFreeLookModifierClipData { Type = CMFreeLookModifierClipType.Initial });
        }
    }
}
#endif
