// <copyright file="CMPanTiltClip.cs" company="BovineLabs">
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
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that drives Cinemachine pan/tilt axes.
    /// </summary>
    [Serializable]
    public class CMPanTiltClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Defines the reference frame against which pan and tilt rotations are made.")]
        private CinemachinePanTilt.ReferenceFrames referenceFrame = CinemachinePanTilt.ReferenceFrames.ParentObject;

        [SerializeField]
        [Tooltip("Defines the recentering target.")]
        private CinemachinePanTilt.RecenterTargetModes recenterTarget = CinemachinePanTilt.RecenterTargetModes.AxisCenter;

        [SerializeField]
        [Tooltip("Axis representing the current horizontal rotation.")]
        private InputAxis panAxis = CreateDefaultPan();

        [SerializeField]
        [Tooltip("Axis representing the current vertical rotation.")]
        private InputAxis tiltAxis = CreateDefaultTilt();

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.Sanitize();
            context.Baker.AddComponent<CMPanTiltAnimated>(clipEntity);
            context.Baker.AddComponent(
                clipEntity,
                new CMPanTiltClipData
                {
                    Type = CMPanTiltClipType.Animated,
                    ReferenceFrame = this.referenceFrame,
                    RecenterTarget = this.recenterTarget,
                    PanAxis = this.panAxis,
                    TiltAxis = this.tiltAxis,
                });
        }

        private void OnValidate()
        {
            this.Sanitize();
        }

        private void Sanitize()
        {
            this.panAxis.Validate();

            var tiltRange = this.tiltAxis.Range;
            tiltRange.x = Mathf.Clamp(tiltRange.x, -90f, 90f);
            tiltRange.y = Mathf.Clamp(tiltRange.y, -90f, 90f);
            if (tiltRange.y < tiltRange.x)
            {
                tiltRange.y = tiltRange.x;
            }

            this.tiltAxis.Range = tiltRange;
            this.tiltAxis.Validate();
        }

        private static InputAxis CreateDefaultPan()
        {
            return new InputAxis
            {
                Value = 0f,
                Range = new Vector2(-180f, 180f),
                Wrap = true,
                Center = 0f,
                Recentering = InputAxis.RecenteringSettings.Default,
            };
        }

        private static InputAxis CreateDefaultTilt()
        {
            return new InputAxis
            {
                Value = 0f,
                Range = new Vector2(-70f, 70f),
                Wrap = false,
                Center = 0f,
                Recentering = InputAxis.RecenteringSettings.Default,
            };
        }
    }
}
#endif
