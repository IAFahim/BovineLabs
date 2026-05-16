// <copyright file="CMCameraOffsetClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that offsets the Cinemachine camera.
    /// </summary>
    [Serializable]
    public class CMCameraOffsetClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Offset the camera's position by this much (camera space).")]
        private Vector3 offset = Vector3.zero;

        [SerializeField]
        [Tooltip("When to apply the offset.")]
        private CinemachineCore.Stage applyAfter = CinemachineCore.Stage.Aim;

        [SerializeField]
        [Tooltip("Re-adjust the aim to preserve the screen position when applying after aim.")]
        private bool preserveComposition;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CMCameraOffsetAnimated>(clipEntity);
            context.Baker.AddComponent(
                clipEntity,
                new CMCameraOffsetClipData
                {
                    Type = CMCameraOffsetClipType.Animated,
                    Offset = math.float3(this.offset),
                    ApplyAfter = this.applyAfter,
                    PreserveComposition = this.preserveComposition,
                });
        }
    }
}
#endif
