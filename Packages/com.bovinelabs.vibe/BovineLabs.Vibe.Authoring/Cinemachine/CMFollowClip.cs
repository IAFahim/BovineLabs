// <copyright file="CMFollowClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Cinemachine.TargetTracking;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that drives Cinemachine follow offsets and damping.
    /// </summary>
    [Serializable]
    public class CMFollowClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Settings to control damping for target tracking.")]
        private TrackerSettings trackerSettings = TrackerSettings.Default;

        [SerializeField]
        [Tooltip("The distance vector that the camera will attempt to maintain from the tracking target")]
        private Vector3 followOffset = new(0f, 0f, -10f);

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CMFollowAnimated>(clipEntity);

            this.trackerSettings.Validate();
            this.followOffset = SanitizeOffset(this.followOffset, this.trackerSettings.BindingMode);

            var clipData = new CMFollowClipData
            {
                Type = CMFollowClipType.Animated,
                FollowOffset = this.followOffset,
                TrackerSettings = this.trackerSettings,
            };

            context.Baker.AddComponent(clipEntity, clipData);
        }

        private static float3 SanitizeOffset(Vector3 offset, BindingMode bindingMode)
        {
            if (bindingMode != BindingMode.LazyFollow)
            {
                return math.float3(offset);
            }

            var sanitized = offset;
            sanitized.x = 0f;
            sanitized.z = -Mathf.Abs(sanitized.z);
            return sanitized;
        }

        private void OnValidate()
        {
            this.trackerSettings.Validate();
            this.followOffset = SanitizeOffset(this.followOffset, this.trackerSettings.BindingMode);
        }
    }
}
#endif
