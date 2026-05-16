// <copyright file="CameraMatrixShiftInitialClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Camera
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Camera;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Clip that restores the camera matrix shift to its captured initial values.
    /// </summary>
    [Serializable]
    public class CameraMatrixShiftInitialClip : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CameraMatrixShiftAnimated>(clipEntity);
            context.Baker.AddComponent(clipEntity, new CameraMatrixShiftClipData { Type = CameraMatrixShiftClipType.Initial });
        }
    }
}
#endif
