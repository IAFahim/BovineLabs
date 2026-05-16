// <copyright file="CMThirdPersonFollowClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
#if UNITY_PHYSICS
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that drives Cinemachine third person follow settings.
    /// </summary>
    [Serializable]
    public class CMThirdPersonFollowClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("How responsively the camera tracks the target.")]
        private Vector3 damping = new(0.1f, 0.5f, 0.3f);

        [SerializeField]
        [Tooltip("Position of the shoulder pivot relative to the Follow target origin.")]
        private Vector3 shoulderOffset = new(0.5f, -0.4f, 0f);

        [SerializeField]
        [Tooltip("Vertical offset of the hand in relation to the shoulder.")]
        private float verticalArmLength = 0.4f;

        [SerializeField]
        [Tooltip("Specifies which shoulder (left, right, or in-between) the camera is on.")]
        private float cameraSide = 1f;

        [SerializeField]
        [Tooltip("How far behind the hand the camera will be placed.")]
        private float cameraDistance = 2f;

        [SerializeField]
        [Tooltip("Settings for collision resolution.")]
        private CinemachineThirdPersonFollowDots.ObstacleSettings avoidObstacles;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.Sanitize();
            context.Baker.AddComponent<CMThirdPersonFollowAnimated>(clipEntity);
            context.Baker.AddComponent(
                clipEntity,
                new CMThirdPersonFollowClipData
                {
                    Type = CMThirdPersonFollowClipType.Animated,
                    Damping = math.float3(this.damping),
                    ShoulderOffset = math.float3(this.shoulderOffset),
                    VerticalArmLength = this.verticalArmLength,
                    CameraSide = this.cameraSide,
                    CameraDistance = this.cameraDistance,
                    AvoidObstacles = this.avoidObstacles,
                });
        }

        private void OnValidate()
        {
            this.Sanitize();
        }

        private void Sanitize()
        {
            this.damping = new Vector3(
                Mathf.Max(0f, this.damping.x),
                Mathf.Max(0f, this.damping.y),
                Mathf.Max(0f, this.damping.z));

            this.verticalArmLength = Mathf.Max(0f, this.verticalArmLength);
            this.cameraSide = Mathf.Clamp(this.cameraSide, 0f, 1f);
            this.cameraDistance = Mathf.Max(0f, this.cameraDistance);

            var obstacles = this.avoidObstacles;
            obstacles.CameraRadius = Mathf.Max(0.001f, obstacles.CameraRadius);
            obstacles.DampingIntoCollision = Mathf.Max(0f, obstacles.DampingIntoCollision);
            obstacles.DampingFromCollision = Mathf.Max(0f, obstacles.DampingFromCollision);
            this.avoidObstacles = obstacles;
        }
    }
}
#endif
#endif
