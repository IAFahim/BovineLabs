// <copyright file="CMSplineDollyClip.cs" company="BovineLabs">
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
    /// Timeline clip that drives Cinemachine spline dolly position.
    /// </summary>
    [Serializable]
    public class CMSplineDollyClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Normalized position along the dolly spline (0=start, 1=end).")]
        [Range(0f, 1f)]
        private float position;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CMSplineDollyAnimated>(clipEntity);
            context.Baker.AddComponent(
                clipEntity,
                new CMSplineDollyClipData
                {
                    Type = CMSplineDollyClipType.Animated,
                    Position = math.clamp(this.position, 0f, 1f),
                });
        }
    }
}
#endif
