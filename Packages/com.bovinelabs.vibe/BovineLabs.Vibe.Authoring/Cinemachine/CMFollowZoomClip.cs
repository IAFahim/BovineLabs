// <copyright file="CMFollowZoomClip.cs" company="BovineLabs">
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
    /// Timeline clip that drives Cinemachine follow zoom settings.
    /// </summary>
    [Serializable]
    public class CMFollowZoomClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("The shot width to maintain, in world units, at target distance.")]
        private float width = 2f;

        [SerializeField]
        [Tooltip("Increase this value to soften the aggressiveness of the follow zoom.")]
        private float damping = 1f;

        [SerializeField]
        [Tooltip("Range for the FOV that this behavior will generate.")]
        private Vector2 fovRange = new(3f, 60f);

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.Sanitize();
            context.Baker.AddComponent<CMFollowZoomAnimated>(clipEntity);
            context.Baker.AddComponent(
                clipEntity,
                new CMFollowZoomClipData
                {
                    Type = CMFollowZoomClipType.Animated,
                    Width = this.width,
                    Damping = this.damping,
                    FovRange = math.float2(this.fovRange),
                });
        }

        private void OnValidate()
        {
            this.Sanitize();
        }

        private void Sanitize()
        {
            this.width = Mathf.Max(0f, this.width);
            this.damping = Mathf.Max(0f, this.damping);
            this.fovRange.y = Mathf.Clamp(this.fovRange.y, 1f, 179f);
            this.fovRange.x = Mathf.Clamp(this.fovRange.x, 1f, this.fovRange.y);
        }
    }
}
#endif
