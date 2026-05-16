// <copyright file="CMHardLookAtClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that animates hard-look-at offsets.
    /// </summary>
    [Serializable]
    public class CMHardLookAtClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Offset applied in the look-at target's local space.")]
        private Vector3 lookAtOffset = Vector3.zero;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CMHardLookAtAnimated>(clipEntity);
            context.Baker.AddComponent(
                clipEntity,
                new CMHardLookAtClipData
                {
                    Type = CMHardLookAtClipType.Animated,
                    LookAtOffset = math.float3(this.lookAtOffset),
                });
        }
    }
}
#endif
