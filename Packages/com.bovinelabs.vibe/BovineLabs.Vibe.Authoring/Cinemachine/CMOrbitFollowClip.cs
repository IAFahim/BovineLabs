// <copyright file="CMOrbitFollowClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Cinemachine;
    using Unity.Cinemachine.TargetTracking;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that drives Cinemachine orbit follow settings.
    /// </summary>
    [Serializable]
    public class CMOrbitFollowClip : DOTSClip, ITimelineClipAsset
    {
        private const float AxisEpsilon = 0.0001f;

        [SerializeField]
        [Tooltip("Settings to control damping for target tracking.")]
        private TrackerSettings trackerSettings = TrackerSettings.Default;

        [SerializeField]
        [Tooltip("Defines the manner in which the orbit surface is constructed.")]
        private CinemachineOrbitalFollow.OrbitStyles orbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;

        [SerializeField]
        [Tooltip("The camera will be placed at this distance from the Follow target.")]
        private float radius = 5f;

        [SerializeField]
        [Tooltip("Defines a complex surface rig from 3 horizontal rings.")]
        private Cinemachine3OrbitRig.Settings orbits = Cinemachine3OrbitRig.Settings.Default;

        [SerializeField]
        [Tooltip("Axis representing the current horizontal rotation.")]
        private InputAxis horizontalAxis = CreateDefaultHorizontal();

        [SerializeField]
        [Tooltip("Axis representing the current vertical rotation.")]
        private InputAxis verticalAxis = CreateDefaultVertical();

        [SerializeField]
        [Tooltip("Axis controlling the scale of the current distance.")]
        private InputAxis radialAxis = CreateDefaultRadial();

        [SerializeField]
        [Tooltip("Offset from the target object's origin in target-local space.")]
        private Vector3 targetOffset = Vector3.zero;

        [SerializeField]
        [Tooltip("Defines the reference frame for horizontal recentering.")]
        private CinemachineOrbitalFollow.ReferenceFrames recenteringTarget = CinemachineOrbitalFollow.ReferenceFrames.TrackingTarget;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.Sanitize();
            context.Baker.AddComponent<CMOrbitFollowAnimated>(clipEntity);
            context.Baker.AddComponent(
                clipEntity,
                new CMOrbitFollowClipData
                {
                    Type = CMOrbitFollowClipType.Animated,
                    TrackerSettings = this.trackerSettings,
                    OrbitStyle = this.orbitStyle,
                    Radius = this.radius,
                    Orbits = this.orbits,
                    HorizontalAxis = this.horizontalAxis,
                    VerticalAxis = this.verticalAxis,
                    RadialAxis = this.radialAxis,
                    TargetOffset = math.float3(this.targetOffset),
                    RecenteringTarget = this.recenteringTarget,
                });
        }

        private void OnValidate()
        {
            this.Sanitize();
        }

        private void Sanitize()
        {
            this.radius = Mathf.Max(0f, this.radius);
            this.trackerSettings.Validate();
            this.orbits = SanitizeOrbits(this.orbits);

            this.horizontalAxis.Restrictions &=
                ~(InputAxis.RestrictionFlags.NoRecentering | InputAxis.RestrictionFlags.RangeIsDriven);
            this.horizontalAxis.Validate();
            this.verticalAxis.Validate();

            var radialRange = this.radialAxis.Range;
            radialRange.x = Mathf.Max(radialRange.x, AxisEpsilon);
            this.radialAxis.Range = radialRange;
            this.radialAxis.Validate();
        }

        private static Cinemachine3OrbitRig.Settings SanitizeOrbits(Cinemachine3OrbitRig.Settings settings)
        {
            settings.SplineCurvature = Mathf.Clamp01(settings.SplineCurvature);
            settings.Top.Radius = Mathf.Max(0f, settings.Top.Radius);
            settings.Center.Radius = Mathf.Max(0f, settings.Center.Radius);
            settings.Bottom.Radius = Mathf.Max(0f, settings.Bottom.Radius);
            return settings;
        }

        private static InputAxis CreateDefaultHorizontal()
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

        private static InputAxis CreateDefaultVertical()
        {
            return new InputAxis
            {
                Value = 17.5f,
                Range = new Vector2(-10f, 45f),
                Wrap = false,
                Center = 17.5f,
                Recentering = InputAxis.RecenteringSettings.Default,
            };
        }

        private static InputAxis CreateDefaultRadial()
        {
            return new InputAxis
            {
                Value = 1f,
                Range = new Vector2(1f, 1f),
                Wrap = false,
                Center = 1f,
                Recentering = InputAxis.RecenteringSettings.Default,
            };
        }
    }
}
#endif
