// <copyright file="CMRecomposerClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Cinemachine;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that drives Cinemachine recomposer offsets.
    /// </summary>
    [Serializable]
    public class CMRecomposerClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Roll adjustment applied to the shot in degrees.")]
        private float tilt;

        [SerializeField]
        [Tooltip("Pan adjustment applied to the shot in degrees.")]
        private float pan;

        [SerializeField]
        [Tooltip("Dutch angle adjustment applied to the shot in degrees.")]
        private float dutch;

        [SerializeField]
        [Tooltip("Multiplier applied to the lens zoom output. 1 = unchanged.")]
        private float zoomScale = 1f;

        [SerializeField]
        [Tooltip("How strongly the camera follows its follow target (0-1).")]
        private float followAttachment = 1f;

        [SerializeField]
        [Tooltip("How strongly the camera follows its look-at target (0-1).")]
        private float lookAtAttachment = 1f;

        [SerializeField]
        [Tooltip("Stage at which the recomposer applies its adjustments.")]
        private CinemachineCore.Stage applyAfter = CinemachineCore.Stage.Aim;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.Sanitize();
            context.Baker.AddComponent<CMRecomposerAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<CMRecomposerClipBlob>();
            blob.Type = CMRecomposerClipType.Animated;
            blob.Tilt = this.tilt;
            blob.Pan = this.pan;
            blob.Dutch = this.dutch;
            blob.ZoomScale = this.zoomScale;
            blob.FollowAttachment = this.followAttachment;
            blob.LookAtAttachment = this.lookAtAttachment;
            blob.ApplyAfter = this.applyAfter;

            var blobRef = builder.CreateBlobAssetReference<CMRecomposerClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new CMRecomposerClipData { Value = blobRef });
        }

        private void OnValidate()
        {
            this.Sanitize();
        }

        private void Sanitize()
        {
            this.zoomScale = Mathf.Max(0f, this.zoomScale);
            this.followAttachment = Mathf.Clamp01(this.followAttachment);
            this.lookAtAttachment = Mathf.Clamp01(this.lookAtAttachment);
        }
    }
}
#endif
