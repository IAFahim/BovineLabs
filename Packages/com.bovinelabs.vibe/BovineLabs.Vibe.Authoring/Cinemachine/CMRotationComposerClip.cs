// <copyright file="CMRotationComposerClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that drives a Cinemachine rotation composer.
    /// </summary>
    [Serializable]
    public class CMRotationComposerClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Local space offset from the tracked target.")]
        private Vector3 targetOffset;

        [SerializeField]
        [Tooltip("Degrees per second responsiveness for horizontal (X) and vertical (Y) composition.")]
        private Vector2 damping = new(1f, 1f);

        [SerializeField]
        [Tooltip("Enable procedural lookahead prediction.")]
        private bool lookaheadEnabled = true;

        [SerializeField]
        [Tooltip("Seconds to look ahead when prediction is enabled.")]
        private float lookaheadTime = 0.1f;

        [SerializeField]
        [Tooltip("Smoothing applied to lookahead prediction.")]
        private float lookaheadSmoothing = 3f;

        [SerializeField]
        [Tooltip("Ignore Y movement when computing lookahead.")]
        private bool lookaheadIgnoreY = true;

        [SerializeField]
        [Tooltip("Target screen position (-1.5 to 1.5 range).")]
        private Vector2 screenPosition;

        [SerializeField]
        [Tooltip("Enable the dead zone region.")]
        private bool deadZoneEnabled = true;

        [SerializeField]
        [Tooltip("Width/height of the dead zone when enabled.")]
        private Vector2 deadZoneSize = new(0.2f, 0.2f);

        [SerializeField]
        [Tooltip("Enable screen-space hard limits.")]
        private bool hardLimitsEnabled;

        [SerializeField]
        [Tooltip("Width/height of the hard limit region (must be >= dead zone size).")]
        private Vector2 hardLimitSize = new(0.8f, 0.8f);

        [SerializeField]
        [Tooltip("Offsets the hard limit region relative to the target position.")]
        private Vector2 hardLimitOffset = Vector2.zero;

        [SerializeField]
        [Tooltip("Recenter the rotation target when the clip activates.")]
        private bool centerOnActivate;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.Sanitize();
            context.Baker.AddComponent<CMRotationComposerAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<CMRotationComposerClipBlob>();
            blob.Type = CMRotationComposerClipType.Animated;
            blob.TargetOffset = math.float3(this.targetOffset);
            blob.Damping = math.float2(this.damping);
            blob.LookaheadTime = this.lookaheadTime;
            blob.LookaheadSmoothing = this.lookaheadSmoothing;
            blob.ScreenPosition = math.float2(this.screenPosition);
            blob.DeadZoneSize = math.float2(this.deadZoneSize);
            blob.HardLimitSize = math.float2(this.hardLimitSize);
            blob.HardLimitOffset = math.float2(this.hardLimitOffset);
            blob.LookaheadEnabled = this.lookaheadEnabled;
            blob.LookaheadIgnoreY = this.lookaheadIgnoreY;
            blob.DeadZoneEnabled = this.deadZoneEnabled;
            blob.HardLimitsEnabled = this.hardLimitsEnabled;
            blob.CenterOnActivate = this.centerOnActivate;

            var blobRef = builder.CreateBlobAssetReference<CMRotationComposerClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new CMRotationComposerClipData { Value = blobRef });
        }

        private void OnValidate()
        {
            this.Sanitize();
        }

        private void Sanitize()
        {
            this.damping = new Vector2(
                Mathf.Max(0f, this.damping.x),
                Mathf.Max(0f, this.damping.y));

            this.lookaheadTime = Mathf.Clamp(this.lookaheadTime, 0f, 1f);
            this.lookaheadSmoothing = Mathf.Clamp(this.lookaheadSmoothing, 0f, 30f);
            this.screenPosition = ClampScreenPosition(this.screenPosition);
            this.deadZoneSize = ClampDeadZoneSize(this.deadZoneSize);
            this.hardLimitSize = ClampHardLimitSize(this.hardLimitSize, this.deadZoneSize);
            this.hardLimitOffset = ClampHardLimitOffset(this.hardLimitOffset);
        }

        private static Vector2 ClampScreenPosition(Vector2 value)
        {
            value.x = Mathf.Clamp(value.x, -1.5f, 1.5f);
            value.y = Mathf.Clamp(value.y, -1.5f, 1.5f);
            return value;
        }

        private static Vector2 ClampDeadZoneSize(Vector2 value)
        {
            value.x = Mathf.Clamp(value.x, 0f, 2f);
            value.y = Mathf.Clamp(value.y, 0f, 2f);
            return value;
        }

        private static Vector2 ClampHardLimitSize(Vector2 value, Vector2 min)
        {
            value.x = Mathf.Clamp(value.x, Mathf.Max(min.x, 0f), 3f);
            value.y = Mathf.Clamp(value.y, Mathf.Max(min.y, 0f), 3f);
            return value;
        }

        private static Vector2 ClampHardLimitOffset(Vector2 value)
        {
            value.x = Mathf.Clamp(value.x, -1f, 1f);
            value.y = Mathf.Clamp(value.y, -1f, 1f);
            return value;
        }
    }
}
#endif
