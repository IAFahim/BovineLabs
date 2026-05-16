// <copyright file="CMCameraClip.cs" company="BovineLabs">
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
    /// Timeline clip that drives the Cinemachine camera lens settings.
    /// </summary>
    [Serializable]
    public class CMCameraClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Field of view in degrees applied over the clip duration when the virtual camera is in perspective mode.")]
        [Range(1f, 179f)]
        private float fieldOfView = 60f;

        [SerializeField]
        [Tooltip("Orthographic size applied over the clip duration when the virtual camera is orthographic.")]
        [Min(0.01f)]
        private float orthographicSize = 10f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CMCameraAnimated>(clipEntity);
            context.Baker.AddComponent(
                clipEntity,
                new CMCameraClipData
                {
                    Type = CMCameraClipType.Animated,
                    FieldOfView = math.clamp(this.fieldOfView, 1f, 179f),
                    OrthographicSize = math.max(this.orthographicSize, 0.01f),
                });
        }
    }
}
#endif
