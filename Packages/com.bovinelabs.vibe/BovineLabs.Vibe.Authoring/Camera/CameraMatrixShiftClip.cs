// <copyright file="CameraMatrixShiftClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Camera
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Camera;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that drives CameraViewSpaceOffset and CameraMatrixShiftOverride.
    /// </summary>
    [Serializable]
    public class CameraMatrixShiftClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Offset of the projection center as a fraction of the half-frustum size at the near plane. (1, 0) shifts by one half-width.")]
        private Vector2 projectionCenterOffset;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CameraMatrixShiftAnimated>(clipEntity);
            context.Baker.AddComponent(clipEntity, new CameraMatrixShiftClipData
            {
                Type = CameraMatrixShiftClipType.Animated,
                ProjectionCenterOffset = new float2(this.projectionCenterOffset.x, this.projectionCenterOffset.y),
            });
        }
    }
}
#endif
