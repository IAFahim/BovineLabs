// <copyright file="LightTrackSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Tests.Runtime.Rendering
{
    using BovineLabs.Bridge.Data.Lighting;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.Light;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.IntegerTime;
    using Unity.Mathematics;
    using UnityEngine;

    public class LightTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void LightTrack_InitialClip_UsesTrackInitialValues()
        {
            var bound = this.Manager.CreateEntity(typeof(LightData));
            this.Manager.SetComponentData(bound, new LightData
            {
                Color = new Color(0f, 0f, 1f, 1f),
                Intensity = 2f,
                ColorTemperature = 3000f,
            });

            var track = this.Manager.CreateEntity(typeof(LightInitial), typeof(TrackBinding));
            this.Manager.SetComponentData(track, new LightInitial
            {
                Value = new LightData
                {
                    Color = new Color(1f, 0f, 0f, 1f),
                    Intensity = 5f,
                    ColorTemperature = 4500f,
                },
            });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });

            var blob = CreateBlobAssetReference(new LightClipBlobData { Type = LightClipType.Initial });

            try
            {
                this.CreateCoreClip(track, bound, new LightClipData { Value = blob, Cookie = default }, 0.2d);
                var system = this.World.CreateSystem<LightTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<LightData>(bound);
                AssertFloat3(new float3(result.Color.r, result.Color.g, result.Color.b), new float3(1f, 0f, 0f));
                Assert.AreEqual(5f, result.Intensity, 0.0001f);
                Assert.AreEqual(4500f, result.ColorTemperature, 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void LightTrack_ConstantClip_RespectsOverrideFlagsAndClampsIntensity()
        {
            var bound = this.Manager.CreateEntity(typeof(LightData));
            this.Manager.SetComponentData(bound, new LightData
            {
                Color = new Color(0.5f, 0.5f, 0.5f, 1f),
                Intensity = 2f,
                ColorTemperature = 3000f,
            });

            var track = this.Manager.CreateEntity(typeof(LightInitial), typeof(TrackBinding));
            this.Manager.SetComponentData(track, new LightInitial { Value = default });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });

            var blob = CreateBlobAssetReference(new LightClipBlobData
            {
                Type = LightClipType.Constant,
                Constant = new LightConstantData
                {
                    OverrideColor = false,
                    OverrideIntensity = true,
                    Intensity = -3f,
                    OverrideColorTemperature = true,
                    ColorTemperature = 5100f,
                },
            });

            try
            {
                this.CreateCoreClip(track, bound, new LightClipData { Value = blob, Cookie = default }, 0.1d);
                var system = this.World.CreateSystem<LightTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<LightData>(bound);
                AssertFloat3(new float3(result.Color.r, result.Color.g, result.Color.b), new float3(0.5f, 0.5f, 0.5f));
                Assert.AreEqual(0f, result.Intensity, 0.0001f);
                Assert.AreEqual(5100f, result.ColorTemperature, 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void LightTrack_FlickerClip_AppliesReferenceMultiplierAndOverrides()
        {
            var bound = this.Manager.CreateEntity(typeof(LightData));
            this.Manager.SetComponentData(bound, new LightData
            {
                Color = new Color(1f, 1f, 0f, 1f),
                Intensity = 10f,
                ColorTemperature = 3000f,
            });

            var track = this.Manager.CreateEntity(typeof(LightInitial), typeof(TrackBinding));
            this.Manager.SetComponentData(track, new LightInitial { Value = default });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });

            var blob = CreateBlobAssetReference(new LightClipBlobData
            {
                Type = LightClipType.Flicker,
                Flicker = new LightFlickerData
                {
                    Preset = LightFlickerPreset.Strobe,
                    Speed = 1f,
                    MinIntensityMultiplier = 0.5f,
                    MaxIntensityMultiplier = 0.5f,
                    DutyCycle = 1f,
                    Seed = 123u,
                    UseCustomCurve = false,
                    OverrideColor = true,
                    OverrideColorTemperature = true,
                    ColorA = new float3(0.2f, 0.4f, 0.6f),
                    ColorB = new float3(0.2f, 0.4f, 0.6f),
                    TemperatureA = 4200f,
                    TemperatureB = 4200f,
                },
            });

            try
            {
                this.CreateCoreClip(track, bound, new LightClipData { Value = blob, Cookie = default }, 0.5d);
                var system = this.World.CreateSystem<LightTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<LightData>(bound);
                AssertFloat3(new float3(result.Color.r, result.Color.g, result.Color.b), new float3(0.2f, 0.4f, 0.6f));
                Assert.AreEqual(5f, result.Intensity, 0.0001f);
                Assert.AreEqual(4200f, result.ColorTemperature, 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void LightTrack_ExtendedConstantClip_AppliesOverridesAndClampsValues()
        {
            var bound = this.Manager.CreateEntity(typeof(LightDataExtended));
            this.Manager.SetComponentData(bound, new LightDataExtended
            {
                Type = LightType.Spot,
                Range = 10f,
                SpotAngle = 30f,
                InnerSpotAngle = 10f,
                CookieSize = new float2(2f, 3f),
                Shadows = LightShadows.Soft,
                ShadowStrength = 0.3f,
                ShadowBias = 0.2f,
                ShadowNormalBias = 0.3f,
                ShadowNearPlane = 0.4f,
                RenderMode = LightRenderMode.Auto,
                CullingMask = 7,
                RenderingLayerMask = 8,
            });

            var track = this.Manager.CreateEntity(typeof(LightExtendedInitial), typeof(TrackBinding));
            this.Manager.SetComponentData(track, new LightExtendedInitial { Value = default });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });

            var blob = CreateBlobAssetReference(new LightClipBlobData
            {
                Type = LightClipType.ExtendedConstant,
                ExtendedConstant = new LightExtendedData
                {
                    OverrideRange = true,
                    Range = -5f,
                    OverrideSpotAngle = true,
                    SpotAngle = 250f,
                    OverrideInnerSpotAngle = true,
                    InnerSpotAngle = 300f,
                    OverrideShadowStrength = true,
                    ShadowStrength = 2f,
                    OverrideShadowBias = true,
                    ShadowBias = -1f,
                    OverrideShadowNormalBias = true,
                    ShadowNormalBias = -2f,
                    OverrideShadowNearPlane = true,
                    ShadowNearPlane = -3f,
                    OverrideCookieSize = true,
                    CookieSize = new float2(-4f, -5f),
                    OverrideType = true,
                    Type = LightType.Point,
                    OverrideRenderMode = true,
                    RenderMode = LightRenderMode.ForceVertex,
                    OverrideShadows = true,
                    Shadows = LightShadows.Hard,
                    OverrideCullingMask = true,
                    CullingMask = 123,
                    OverrideRenderingLayerMask = true,
                    RenderingLayerMask = 456,
                },
            });

            try
            {
                this.CreateExtendedClip(track, bound, new LightClipData { Value = blob, Cookie = default }, 0.2d);
                var system = this.World.CreateSystem<LightTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<LightDataExtended>(bound);
                Assert.AreEqual(0f, result.Range, 0.0001f);
                Assert.AreEqual(179f, result.SpotAngle, 0.0001f);
                Assert.AreEqual(179f, result.InnerSpotAngle, 0.0001f);
                Assert.AreEqual(1f, result.ShadowStrength, 0.0001f);
                Assert.AreEqual(0f, result.ShadowBias, 0.0001f);
                Assert.AreEqual(0f, result.ShadowNormalBias, 0.0001f);
                Assert.AreEqual(0f, result.ShadowNearPlane, 0.0001f);
                AssertFloat2(result.CookieSize, float2.zero);
                Assert.AreEqual(LightType.Point, result.Type);
                Assert.AreEqual(LightRenderMode.ForceVertex, result.RenderMode);
                Assert.AreEqual(LightShadows.Hard, result.Shadows);
                Assert.AreEqual(123, result.CullingMask);
                Assert.AreEqual(456, result.RenderingLayerMask);
            }
            finally
            {
                blob.Dispose();
            }
        }

        private Entity CreateCoreClip(Entity track, Entity bound, in LightClipData clipData, double localTime)
        {
            var clip = this.Manager.CreateEntity(
                typeof(LightAnimated),
                typeof(LightClipData),
                typeof(Clip),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(ClipActive),
                typeof(ClipActivePrevious),
                typeof(LocalTime),
                typeof(ClipBlobCurveCache));

            this.Manager.SetComponentData(clip, new LightAnimated { Value = default });
            this.Manager.SetComponentData(clip, clipData);
            this.Manager.SetComponentData(clip, new Clip { Track = track });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
            this.Manager.SetComponentData(clip, new LocalTime { Value = new DiscreteTime(localTime) });
            this.Manager.SetComponentData(clip, ClipBlobCurveCache.Create());
            this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);
            return clip;
        }

        private Entity CreateExtendedClip(Entity track, Entity bound, in LightClipData clipData, double localTime)
        {
            var clip = this.Manager.CreateEntity(
                typeof(LightExtendedAnimated),
                typeof(LightClipData),
                typeof(Clip),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(ClipActive),
                typeof(ClipActivePrevious),
                typeof(LocalTime),
                typeof(ClipBlobCurveCache));

            this.Manager.SetComponentData(clip, new LightExtendedAnimated { Value = default });
            this.Manager.SetComponentData(clip, clipData);
            this.Manager.SetComponentData(clip, new Clip { Track = track });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
            this.Manager.SetComponentData(clip, new LocalTime { Value = new DiscreteTime(localTime) });
            this.Manager.SetComponentData(clip, ClipBlobCurveCache.Create());
            this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);
            return clip;
        }

        private static void AssertFloat3(float3 actual, float3 expected, float tolerance = 0.0001f)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance);
            Assert.AreEqual(expected.y, actual.y, tolerance);
            Assert.AreEqual(expected.z, actual.z, tolerance);
        }

        private static void AssertFloat2(float2 actual, float2 expected, float tolerance = 0.0001f)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance);
            Assert.AreEqual(expected.y, actual.y, tolerance);
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
    }
}
#endif
