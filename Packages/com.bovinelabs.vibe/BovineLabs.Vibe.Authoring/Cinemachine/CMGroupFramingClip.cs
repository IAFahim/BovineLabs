// <copyright file="CMGroupFramingClip.cs" company="BovineLabs">
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
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that drives Cinemachine group framing settings.
    /// </summary>
    [Serializable]
    public class CMGroupFramingClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Overall framing strategy to apply while the clip is active.")]
        private CinemachineGroupFraming.FramingModes framingMode = CinemachineGroupFraming.FramingModes.Horizontal;

        [SerializeField]
        [Tooltip("Desired framing size used by the selected framing mode.")]
        private float framingSize = 0.8f;

        [SerializeField]
        [Tooltip("Normalized offset applied to the screen center (-1..1).")]
        private Vector2 centerOffset = Vector2.zero;

        [SerializeField]
        [Tooltip("Smoothing factor applied when adjusting to new framing targets.")]
        private float damping = 1f;

        [SerializeField]
        [Tooltip("How the track adjusts size relative to the bound targets.")]
        private CinemachineGroupFraming.SizeAdjustmentModes sizeAdjustment = CinemachineGroupFraming.SizeAdjustmentModes.DollyThenZoom;

        [SerializeField]
        [Tooltip("How the camera laterally adjusts while framing the group.")]
        private CinemachineGroupFraming.LateralAdjustmentModes lateralAdjustment = CinemachineGroupFraming.LateralAdjustmentModes.ChangePosition;

        [SerializeField]
        [Tooltip("Field of view range (degrees) when using zoom adjustments.")]
        private Vector2 fovRange = new (40f, 60f);

        [SerializeField]
        [Tooltip("Dolly distance range (world units) when using dolly adjustments.")]
        private Vector2 dollyRange = new (0f, 10f);

        [SerializeField]
        [Tooltip("Orthographic size range when the camera uses an ortho projection.")]
        private Vector2 orthoSizeRange = new (5f, 20f);

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.Sanitize();
            context.Baker.AddComponent<CMGroupFramingAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<CMGroupFramingClipBlob>();
            blob.Type = CMGroupFramingClipType.Animated;
            blob.FramingMode = this.framingMode;
            blob.FramingSize = this.framingSize;
            blob.CenterOffset = math.float2(this.centerOffset);
            blob.Damping = this.damping;
            blob.SizeAdjustment = this.sizeAdjustment;
            blob.LateralAdjustment = this.lateralAdjustment;
            blob.FovRange = math.float2(this.fovRange);
            blob.DollyRange = math.float2(this.dollyRange);
            blob.OrthoSizeRange = math.float2(this.orthoSizeRange);

            var blobRef = builder.CreateBlobAssetReference<CMGroupFramingClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new CMGroupFramingClipData { Value = blobRef });
        }

        private void OnValidate()
        {
            this.Sanitize();
        }

        private void Sanitize()
        {
            this.framingSize = Mathf.Max(0f, this.framingSize);
            this.centerOffset.x = Mathf.Clamp(this.centerOffset.x, -1f, 1f);
            this.centerOffset.y = Mathf.Clamp(this.centerOffset.y, -1f, 1f);
            this.damping = Mathf.Max(0f, this.damping);
            this.fovRange = ClampRange(this.fovRange, 1f, 179f);
            this.dollyRange = ClampRange(this.dollyRange, 0f);
            this.orthoSizeRange = ClampRange(this.orthoSizeRange, 0f);
        }

        private static Vector2 ClampRange(in Vector2 range, float min, float max = float.PositiveInfinity)
        {
            var minValue = Mathf.Min(range.x, range.y);
            var maxValue = Mathf.Max(range.x, range.y);

            minValue = Mathf.Max(min, minValue);
            maxValue = Mathf.Max(minValue, maxValue);

            if (!float.IsInfinity(max))
            {
                maxValue = Mathf.Min(max, maxValue);
                minValue = Mathf.Min(minValue, maxValue);
            }

            return new Vector2(minValue, maxValue);
        }
    }
}
#endif
