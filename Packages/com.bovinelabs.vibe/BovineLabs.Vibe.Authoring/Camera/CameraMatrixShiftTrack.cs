// <copyright file="CameraMatrixShiftTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Camera
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Camera;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that blends camera matrix shift clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CameraMatrixShiftClip))]
    [TrackClipType(typeof(CameraMatrixShiftInitialClip))]
    [TrackBindingType(typeof(Camera))]
    [TrackColor(0.25f, 0.1f, 0.1f)]
    [DisplayName("DOTS/Camera/Matrix Shift Track")]
    public class CameraMatrixShiftTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddTransformUsageFlags(context.Binding!.Target, TransformUsageFlags.Dynamic);
            context.Baker.AddComponent<CameraMatrixShiftInitial>(context.TrackEntity);
        }
    }
}
#endif
