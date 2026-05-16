// <copyright file="CinemachineTrackSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Tests.Runtime.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Cinemachine;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Unity.Cinemachine;
    using Unity.Cinemachine.TargetTracking;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    public class CinemachineTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void PanTiltTrack_AnimatedClip_SanitizesAxesAndAppliesModes()
        {
            var bound = this.Manager.CreateEntity(typeof(CMPanTilt));
            this.Manager.SetComponentData(bound, new CMPanTilt
            {
                ReferenceFrame = CinemachinePanTilt.ReferenceFrames.ParentObject,
                RecenterTarget = CinemachinePanTilt.RecenterTargetModes.AxisCenter,
                PanAxis = CreateAxis(-180f, 180f, 0f, 0f, true),
                TiltAxis = CreateAxis(-45f, 45f, 0f, 0f),
            });

            var track = this.Manager.CreateEntity();
            this.CreateClip(
                track,
                bound,
                new CMPanTiltAnimated { Value = default },
                new CMPanTiltClipData
                {
                    Type = CMPanTiltClipType.Animated,
                    ReferenceFrame = CinemachinePanTilt.ReferenceFrames.World,
                    RecenterTarget = CinemachinePanTilt.RecenterTargetModes.TrackingTargetForward,
                    PanAxis = CreateAxis(10f, 0f, -10f, 25f, true, InputAxis.RestrictionFlags.None, recenterWait: -2f, recenterTime: -3f),
                    TiltAxis = CreateAxis(120f, -120f, -200f, 300f, false, InputAxis.RestrictionFlags.None, recenterWait: -4f, recenterTime: -5f),
                });

            var system = this.World.CreateSystem<CMPanTiltTrackSystem>();
            this.RunSystem(system);

            var result = this.Manager.GetComponentData<CMPanTilt>(bound);
            Assert.AreEqual(CinemachinePanTilt.ReferenceFrames.World, result.ReferenceFrame);
            Assert.AreEqual(CinemachinePanTilt.RecenterTargetModes.TrackingTargetForward, result.RecenterTarget);
            Assert.AreEqual(10f, result.PanAxis.Range.x, 0.0001f);
            Assert.AreEqual(10f, result.PanAxis.Range.y, 0.0001f);
            Assert.AreEqual(10f, result.PanAxis.Center, 0.0001f);
            Assert.AreEqual(10f, result.PanAxis.Value, 0.0001f);
            Assert.AreEqual(0f, result.PanAxis.Recentering.Wait, 0.0001f);
            Assert.AreEqual(0f, result.PanAxis.Recentering.Time, 0.0001f);
            Assert.AreEqual(90f, result.TiltAxis.Range.x, 0.0001f);
            Assert.AreEqual(90f, result.TiltAxis.Range.y, 0.0001f);
            Assert.AreEqual(90f, result.TiltAxis.Center, 0.0001f);
            Assert.AreEqual(90f, result.TiltAxis.Value, 0.0001f);
        }

        [Test]
        public void FollowTrack_InitialClip_UsesTrackInitialValue()
        {
            var bound = this.Manager.CreateEntity(typeof(CMFollow));
            this.Manager.SetComponentData(bound, new CMFollow
            {
                FollowOffset = new float3(8f, 8f, 8f),
                TrackerSettings = CreateTrackerSettings((BindingMode)0),
            });

            var track = this.Manager.CreateEntity(typeof(CMFollowInitial));
            this.Manager.SetComponentData(track, new CMFollowInitial
            {
                Value = new CMFollow
                {
                    FollowOffset = new float3(0f, 2f, -4f),
                    TrackerSettings = CreateTrackerSettings((BindingMode)0),
                },
            });

            this.CreateClip(
                track,
                bound,
                new CMFollowAnimated { Value = default },
                new CMFollowClipData
                {
                    Type = CMFollowClipType.Initial,
                    FollowOffset = new float3(99f, 99f, 99f),
                    TrackerSettings = CreateTrackerSettings(BindingMode.LazyFollow),
                });

            var system = this.World.CreateSystem<CMFollowTrackSystem>();
            this.RunSystem(system);

            var result = this.Manager.GetComponentData<CMFollow>(bound);
            AssertFloat3(result.FollowOffset, new float3(0f, 2f, -4f));
        }

        [Test]
        public void FollowTrack_AnimatedClip_LazyFollowConstrainsOffsetAndAppliesBindingMode()
        {
            var bound = this.Manager.CreateEntity(typeof(CMFollow));
            this.Manager.SetComponentData(bound, new CMFollow
            {
                FollowOffset = new float3(1f, 1f, 1f),
                TrackerSettings = CreateTrackerSettings((BindingMode)0),
            });

            var track = this.Manager.CreateEntity();
            this.CreateClip(
                track,
                bound,
                new CMFollowAnimated { Value = default },
                new CMFollowClipData
                {
                    Type = CMFollowClipType.Animated,
                    FollowOffset = new float3(6f, 3f, 2f),
                    TrackerSettings = CreateTrackerSettings(BindingMode.LazyFollow),
                });

            var system = this.World.CreateSystem<CMFollowTrackSystem>();
            this.RunSystem(system);

            var result = this.Manager.GetComponentData<CMFollow>(bound);
            Assert.AreEqual(BindingMode.LazyFollow, result.TrackerSettings.BindingMode);
            AssertFloat3(result.FollowOffset, new float3(0f, 3f, -2f));
        }

        [Test]
        public void FollowZoomTrack_AnimatedClip_SanitizesRangeAndNonNegativeValues()
        {
            var bound = this.Manager.CreateEntity(typeof(CMFollowZoom));
            this.Manager.SetComponentData(bound, new CMFollowZoom
            {
                Width = 3f,
                Damping = 2f,
                FovRange = new float2(20f, 60f),
            });

            var track = this.Manager.CreateEntity();
            this.CreateClip(
                track,
                bound,
                new CMFollowZoomAnimated { Value = default },
                new CMFollowZoomClipData
                {
                    Type = CMFollowZoomClipType.Animated,
                    Width = -1f,
                    Damping = -2f,
                    FovRange = new float2(200f, -5f),
                });

            var system = this.World.CreateSystem<CMFollowZoomTrackSystem>();
            this.RunSystem(system);

            var result = this.Manager.GetComponentData<CMFollowZoom>(bound);
            Assert.AreEqual(0f, result.Width, 0.0001f);
            Assert.AreEqual(0f, result.Damping, 0.0001f);
            AssertFloat2(result.FovRange, new float2(1f, 1f));
        }

        [Test]
        public void FollowZoomTrack_AnimatedClip_ClampsUpperBoundBeforeLowerBound()
        {
            var bound = this.Manager.CreateEntity(typeof(CMFollowZoom));
            this.Manager.SetComponentData(bound, new CMFollowZoom
            {
                Width = 2f,
                Damping = 1f,
                FovRange = new float2(20f, 60f),
            });

            var track = this.Manager.CreateEntity();
            this.CreateClip(
                track,
                bound,
                new CMFollowZoomAnimated { Value = default },
                new CMFollowZoomClipData
                {
                    Type = CMFollowZoomClipType.Animated,
                    Width = 5f,
                    Damping = 4f,
                    FovRange = new float2(200f, 300f),
                });

            var system = this.World.CreateSystem<CMFollowZoomTrackSystem>();
            this.RunSystem(system);

            var result = this.Manager.GetComponentData<CMFollowZoom>(bound);
            Assert.AreEqual(5f, result.Width, 0.0001f);
            Assert.AreEqual(4f, result.Damping, 0.0001f);
            AssertFloat2(result.FovRange, new float2(179f, 179f));
        }

        [Test]
        public void OrbitFollowTrack_AnimatedClip_SanitizesAxesOrbitsAndRadius()
        {
            var bound = this.Manager.CreateEntity(typeof(CMOrbitFollow));
            this.Manager.SetComponentData(bound, new CMOrbitFollow
            {
                TrackerSettings = CreateTrackerSettings((BindingMode)0),
                OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere,
                Radius = 3f,
                Orbits = Cinemachine3OrbitRig.Settings.Default,
                HorizontalAxis = CreateAxis(-180f, 180f, 0f, 0f, true),
                VerticalAxis = CreateAxis(-10f, 45f, 5f, 5f),
                RadialAxis = CreateAxis(1f, 1f, 1f, 1f),
                TargetOffset = float3.zero,
                RecenteringTarget = CinemachineOrbitalFollow.ReferenceFrames.TrackingTarget,
            });

            var track = this.Manager.CreateEntity();
            this.CreateClip(
                track,
                bound,
                new CMOrbitFollowAnimated { Value = default },
                new CMOrbitFollowClipData
                {
                    Type = CMOrbitFollowClipType.Animated,
                    TrackerSettings = CreateTrackerSettings(BindingMode.LazyFollow),
                    OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing,
                    Radius = -5f,
                    Orbits = new Cinemachine3OrbitRig.Settings
                    {
                        Top = new Cinemachine3OrbitRig.Orbit { Radius = -4f, Height = 2f },
                        Center = new Cinemachine3OrbitRig.Orbit { Radius = -3f, Height = 1f },
                        Bottom = new Cinemachine3OrbitRig.Orbit { Radius = -2f, Height = -1f },
                        SplineCurvature = 2f,
                    },
                    HorizontalAxis = CreateAxis(
                        4f,
                        -4f,
                        -10f,
                        8f,
                        true,
                        InputAxis.RestrictionFlags.NoRecentering | InputAxis.RestrictionFlags.RangeIsDriven),
                    VerticalAxis = CreateAxis(2f, -2f, 5f, -5f),
                    RadialAxis = CreateAxis(-5f, -1f, -2f, -10f),
                    TargetOffset = new float3(1f, 2f, 3f),
                    RecenteringTarget = CinemachineOrbitalFollow.ReferenceFrames.ParentObject,
                });

            var system = this.World.CreateSystem<CMOrbitFollowTrackSystem>();
            this.RunSystem(system);

            var result = this.Manager.GetComponentData<CMOrbitFollow>(bound);
            var restrictedFlags = InputAxis.RestrictionFlags.NoRecentering | InputAxis.RestrictionFlags.RangeIsDriven;
            Assert.AreEqual(CinemachineOrbitalFollow.OrbitStyles.ThreeRing, result.OrbitStyle);
            Assert.AreEqual(CinemachineOrbitalFollow.ReferenceFrames.ParentObject, result.RecenteringTarget);
            Assert.AreEqual(BindingMode.LazyFollow, result.TrackerSettings.BindingMode);
            Assert.AreEqual(0f, result.Radius, 0.0001f);
            Assert.AreEqual(1f, result.Orbits.SplineCurvature, 0.0001f);
            Assert.AreEqual(0f, result.Orbits.Top.Radius, 0.0001f);
            Assert.AreEqual(0f, result.Orbits.Center.Radius, 0.0001f);
            Assert.AreEqual(0f, result.Orbits.Bottom.Radius, 0.0001f);
            Assert.IsTrue(result.RadialAxis.Range.x >= 0.0001f);
            Assert.AreEqual(0, (int)(result.HorizontalAxis.Restrictions & restrictedFlags));
        }

        [Test]
        public void GroupFramingTrack_AnimatedClip_SanitizesRangesAndAppliesModes()
        {
            var bound = this.Manager.CreateEntity(typeof(CMGroupFraming));
            this.Manager.SetComponentData(bound, new CMGroupFraming
            {
                FramingMode = CinemachineGroupFraming.FramingModes.HorizontalAndVertical,
                FramingSize = 0.5f,
                CenterOffset = float2.zero,
                Damping = 1f,
                SizeAdjustment = CinemachineGroupFraming.SizeAdjustmentModes.DollyOnly,
                LateralAdjustment = CinemachineGroupFraming.LateralAdjustmentModes.ChangePosition,
                FovRange = new float2(20f, 60f),
                DollyRange = new float2(0f, 10f),
                OrthoSizeRange = new float2(2f, 12f),
            });

            var blob = CreateBlobAssetReference(new CMGroupFramingClipBlob
            {
                Type = CMGroupFramingClipType.Animated,
                FramingMode = CinemachineGroupFraming.FramingModes.Horizontal,
                FramingSize = -1f,
                CenterOffset = new float2(2f, -2f),
                Damping = -3f,
                SizeAdjustment = CinemachineGroupFraming.SizeAdjustmentModes.ZoomOnly,
                LateralAdjustment = CinemachineGroupFraming.LateralAdjustmentModes.ChangeRotation,
                FovRange = new float2(200f, -5f),
                DollyRange = new float2(-5f, -3f),
                OrthoSizeRange = new float2(-2f, 1f),
            });

            try
            {
                var track = this.Manager.CreateEntity();
                this.CreateClip(
                    track,
                    bound,
                    new CMGroupFramingAnimated { Value = default },
                    new CMGroupFramingClipData { Value = blob });

                var system = this.World.CreateSystem<CMGroupFramingTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<CMGroupFraming>(bound);
                Assert.AreEqual(CinemachineGroupFraming.FramingModes.Horizontal, result.FramingMode);
                Assert.AreEqual(CinemachineGroupFraming.SizeAdjustmentModes.ZoomOnly, result.SizeAdjustment);
                Assert.AreEqual(CinemachineGroupFraming.LateralAdjustmentModes.ChangeRotation, result.LateralAdjustment);
                Assert.AreEqual(0f, result.FramingSize, 0.0001f);
                AssertFloat2(result.CenterOffset, new float2(1f, -1f));
                Assert.AreEqual(0f, result.Damping, 0.0001f);
                AssertFloat2(result.FovRange, new float2(1f, 179f));
                AssertFloat2(result.DollyRange, new float2(0f, 0f));
                AssertFloat2(result.OrthoSizeRange, new float2(0f, 1f));
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void PositionComposerTrack_AnimatedClip_SanitizesNumericValuesAndAppliesFlags()
        {
            var bound = this.Manager.CreateEntity(typeof(CMPositionComposer));
            this.Manager.SetComponentData(bound, new CMPositionComposer
            {
                CameraDistance = 5f,
                DeadZoneDepth = 2f,
                Composition = default,
                TargetOffset = new float3(0f, 1f, 0f),
                Damping = new float3(1f, 1f, 1f),
                Lookahead = default,
                CenterOnActivate = false,
            });

            var blob = CreateBlobAssetReference(new CMPositionComposerClipBlob
            {
                Type = CMPositionComposerClipType.Animated,
                CameraDistance = -2f,
                DeadZoneDepth = -3f,
                TargetOffset = new float3(10f, 20f, 30f),
                Damping = new float3(-4f, -5f, -6f),
                LookaheadTime = 2f,
                LookaheadSmoothing = 100f,
                ScreenPosition = new float2(3f, -3f),
                DeadZoneSize = new float2(5f, -5f),
                HardLimitSize = new float2(5f, -5f),
                HardLimitOffset = new float2(2f, -2f),
                LookaheadEnabled = true,
                LookaheadIgnoreY = true,
                DeadZoneEnabled = true,
                HardLimitsEnabled = true,
                CenterOnActivate = true,
            });

            try
            {
                var track = this.Manager.CreateEntity();
                this.CreateClip(
                    track,
                    bound,
                    new CMPositionComposerAnimated { Value = default },
                    new CMPositionComposerClipData { Value = blob });

                var system = this.World.CreateSystem<CMPositionComposerTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<CMPositionComposer>(bound);
                Assert.IsTrue(result.Lookahead.Enabled);
                Assert.IsTrue(result.Lookahead.IgnoreY);
                Assert.IsTrue(result.Composition.DeadZone.Enabled);
                Assert.IsTrue(result.Composition.HardLimits.Enabled);
                Assert.IsTrue(result.CenterOnActivate);
                Assert.AreEqual(0f, result.CameraDistance, 0.0001f);
                Assert.AreEqual(0f, result.DeadZoneDepth, 0.0001f);
                AssertFloat3(result.Damping, float3.zero);
                Assert.AreEqual(1f, result.Lookahead.Time, 0.0001f);
                Assert.AreEqual(30f, result.Lookahead.Smoothing, 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void RecomposerTrack_AnimatedClip_SanitizesValuesAndAppliesStage()
        {
            var bound = this.Manager.CreateEntity(typeof(CMRecomposer));
            this.Manager.SetComponentData(bound, new CMRecomposer
            {
                ApplyAfter = CinemachineCore.Stage.Aim,
                Tilt = 0f,
                Pan = 0f,
                Dutch = 0f,
                ZoomScale = 1f,
                FollowAttachment = 1f,
                LookAtAttachment = 1f,
            });

            var blob = CreateBlobAssetReference(new CMRecomposerClipBlob
            {
                Type = CMRecomposerClipType.Animated,
                Tilt = 5f,
                Pan = 6f,
                Dutch = 7f,
                ZoomScale = -2f,
                FollowAttachment = 2f,
                LookAtAttachment = -1f,
                ApplyAfter = CinemachineCore.Stage.Body,
            });

            try
            {
                var track = this.Manager.CreateEntity();
                this.CreateClip(
                    track,
                    bound,
                    new CMRecomposerAnimated { Value = default },
                    new CMRecomposerClipData { Value = blob });

                var system = this.World.CreateSystem<CMRecomposerTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<CMRecomposer>(bound);
                Assert.AreEqual(CinemachineCore.Stage.Body, result.ApplyAfter);
                Assert.AreEqual(5f, result.Tilt, 0.0001f);
                Assert.AreEqual(6f, result.Pan, 0.0001f);
                Assert.AreEqual(7f, result.Dutch, 0.0001f);
                Assert.AreEqual(0f, result.ZoomScale, 0.0001f);
                Assert.AreEqual(1f, result.FollowAttachment, 0.0001f);
                Assert.AreEqual(0f, result.LookAtAttachment, 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

#if UNITY_PHYSICS
        [Test]
        public void ThirdPersonFollowTrack_AnimatedClip_SanitizesBlendAndObstacleSettings()
        {
            var bound = this.Manager.CreateEntity(typeof(CMThirdPersonFollow));
            this.Manager.SetComponentData(bound, new CMThirdPersonFollow
            {
                Damping = new float3(1f, 1f, 1f),
                ShoulderOffset = new float3(0.5f, -0.4f, 0f),
                VerticalArmLength = 0.4f,
                CameraSide = 1f,
                CameraDistance = 2f,
                AvoidObstacles = default,
            });

            var track = this.Manager.CreateEntity();
            this.CreateClip(
                track,
                bound,
                new CMThirdPersonFollowAnimated { Value = default },
                new CMThirdPersonFollowClipData
                {
                    Type = CMThirdPersonFollowClipType.Animated,
                    Damping = new float3(-3f, -4f, -5f),
                    ShoulderOffset = new float3(1f, 2f, 3f),
                    VerticalArmLength = -6f,
                    CameraSide = 2f,
                    CameraDistance = -7f,
                    AvoidObstacles = new CinemachineThirdPersonFollowDots.ObstacleSettings
                    {
                        Enabled = true,
                        CameraRadius = -1f,
                        DampingIntoCollision = -2f,
                        DampingFromCollision = -3f,
                    },
                });

            var system = this.World.CreateSystem<CMThirdPersonFollowTrackSystem>();
            this.RunSystem(system);

            var result = this.Manager.GetComponentData<CMThirdPersonFollow>(bound);
            AssertFloat3(result.Damping, float3.zero);
            AssertFloat3(result.ShoulderOffset, new float3(1f, 2f, 3f));
            Assert.AreEqual(0f, result.VerticalArmLength, 0.0001f);
            Assert.AreEqual(1f, result.CameraSide, 0.0001f);
            Assert.AreEqual(0f, result.CameraDistance, 0.0001f);
            Assert.IsTrue(result.AvoidObstacles.Enabled);
            Assert.AreEqual(0.001f, result.AvoidObstacles.CameraRadius, 0.0001f);
            Assert.AreEqual(0f, result.AvoidObstacles.DampingIntoCollision, 0.0001f);
            Assert.AreEqual(0f, result.AvoidObstacles.DampingFromCollision, 0.0001f);
        }
#endif
        private static TrackerSettings CreateTrackerSettings(BindingMode bindingMode)
        {
            var settings = TrackerSettings.Default;
            settings.BindingMode = bindingMode;
            return settings;
        }

        private static InputAxis CreateAxis(
            float min,
            float max,
            float center,
            float value,
            bool wrap = false,
            InputAxis.RestrictionFlags restrictions = InputAxis.RestrictionFlags.None,
            float recenterWait = 0f,
            float recenterTime = 0f)
        {
            return new InputAxis
            {
                Wrap = wrap,
                Restrictions = restrictions,
                Range = new Vector2(min, max),
                Center = center,
                Value = value,
                Recentering = new InputAxis.RecenteringSettings
                {
                    Enabled = true,
                    Wait = recenterWait,
                    Time = recenterTime,
                },
            };
        }

        private Entity CreateClip<TAnimated, TClipData>(
            Entity track,
            Entity bound,
            in TAnimated animated,
            in TClipData clipData)
            where TAnimated : unmanaged, IComponentData
            where TClipData : unmanaged, IComponentData
        {
            var clip = this.Manager.CreateEntity(
                typeof(TAnimated),
                typeof(TClipData),
                typeof(Clip),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(ClipActive),
                typeof(ClipActivePrevious));

            this.Manager.SetComponentData(clip, animated);
            this.Manager.SetComponentData(clip, clipData);
            this.Manager.SetComponentData(clip, new Clip { Track = track });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);
            return clip;
        }

        private static BlobAssetReference<T> CreateBlobAssetReference<T>(in T value)
            where T : unmanaged
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<T>();
            root = value;
            var blob = builder.CreateBlobAssetReference<T>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static void AssertFloat2(float2 actual, float2 expected, float tolerance = 0.0001f)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance);
            Assert.AreEqual(expected.y, actual.y, tolerance);
        }

        private static void AssertFloat3(float3 actual, float3 expected, float tolerance = 0.0001f)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance);
            Assert.AreEqual(expected.y, actual.y, tolerance);
            Assert.AreEqual(expected.z, actual.z, tolerance);
        }
    }
}
#endif
