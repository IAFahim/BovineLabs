// <copyright file="VolumeTrackSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Tests.Runtime.Rendering
{
    using BovineLabs.Bridge.Data.Volume;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Volume;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class VolumeTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void VolumeSettingsTrack_InitialClip_AppliesInitialState()
        {
            var originalProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            var initialProfile = ScriptableObject.CreateInstance<VolumeProfile>();

            try
            {
                var bound = this.Manager.CreateEntity(typeof(VolumeSettings));
                this.Manager.SetComponentData(bound, new VolumeSettings
                {
                    Weight = 0.1f,
                    Priority = 1f,
                    BlendDistance = 2f,
                    IsGlobal = false,
                    Profile = originalProfile,
                });

                var track = this.Manager.CreateEntity(typeof(VolumeSettingsInitial), typeof(TrackBinding));
                this.Manager.SetComponentData(track, new VolumeSettingsInitial
                {
                    Value = new VolumeSettings
                    {
                        Weight = 0.6f,
                        Priority = 12f,
                        BlendDistance = 5f,
                        IsGlobal = true,
                        Profile = initialProfile,
                    },
                });
                this.Manager.SetComponentData(track, new TrackBinding { Value = bound });

                var blob = CreateBlobAssetReference(new VolumeSettingsClipBlob
                {
                    Type = VolumeSettingsClipType.Initial,
                    Weight = 0f,
                });

                try
                {
                    this.CreateClip(
                        track,
                        bound,
                        new VolumeSettingsAnimated { Value = default },
                        new VolumeSettingsClipData { Value = blob, Profile = default });

                    var system = this.World.CreateSystem<VolumeSettingsTrackSystem>();
                    this.RunSystem(system);

                    var result = this.Manager.GetComponentData<VolumeSettings>(bound);
                    Assert.AreEqual(0.6f, result.Weight, 0.0001f);
                    Assert.AreEqual(12f, result.Priority, 0.0001f);
                    Assert.AreEqual(5f, result.BlendDistance, 0.0001f);
                    Assert.IsTrue(result.IsGlobal);
                    Assert.AreEqual(initialProfile, (VolumeProfile)result.Profile);
                }
                finally
                {
                    blob.Dispose();
                }
            }
            finally
            {
                Object.DestroyImmediate(originalProfile);
                Object.DestroyImmediate(initialProfile);
            }
        }

        [Test]
        public void VolumeSettingsTrack_ConstantClip_AppliesConfiguredOverrides()
        {
            var originalProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            var overrideProfile = ScriptableObject.CreateInstance<VolumeProfile>();

            try
            {
                var bound = this.Manager.CreateEntity(typeof(VolumeSettings));
                this.Manager.SetComponentData(bound, new VolumeSettings
                {
                    Weight = 0.1f,
                    Priority = 1f,
                    BlendDistance = 2f,
                    IsGlobal = false,
                    Profile = originalProfile,
                });

                var track = this.Manager.CreateEntity(typeof(VolumeSettingsInitial), typeof(TrackBinding));
                this.Manager.SetComponentData(track, new VolumeSettingsInitial { Value = default });
                this.Manager.SetComponentData(track, new TrackBinding { Value = bound });

                var blob = CreateBlobAssetReference(new VolumeSettingsClipBlob
                {
                    Type = VolumeSettingsClipType.Constant,
                    Weight = 0.9f,
                    Priority = 15f,
                    BlendDistance = 50f,
                    IsGlobal = true,
                    OverridePriority = true,
                    OverrideBlendDistance = false,
                    OverrideIsGlobal = true,
                    OverrideProfile = true,
                });

                try
                {
                    this.CreateClip(
                        track,
                        bound,
                        new VolumeSettingsAnimated { Value = default },
                        new VolumeSettingsClipData { Value = blob, Profile = overrideProfile });

                    var system = this.World.CreateSystem<VolumeSettingsTrackSystem>();
                    this.RunSystem(system);

                    var result = this.Manager.GetComponentData<VolumeSettings>(bound);
                    Assert.AreEqual(0.9f, result.Weight, 0.0001f);
                    Assert.AreEqual(15f, result.Priority, 0.0001f);
                    Assert.AreEqual(2f, result.BlendDistance, 0.0001f);
                    Assert.IsTrue(result.IsGlobal);
                    Assert.AreEqual(overrideProfile, (VolumeProfile)result.Profile);
                }
                finally
                {
                    blob.Dispose();
                }
            }
            finally
            {
                Object.DestroyImmediate(originalProfile);
                Object.DestroyImmediate(overrideProfile);
            }
        }

        [Test]
        public void VolumeBloomTrack_InitialClip_AppliesInitialValuesAndOverrides()
        {
            var bound = this.Manager.CreateEntity(typeof(VolumeBloom));
            this.Manager.SetComponentData(bound, new VolumeBloom
            {
                Threshold = 1f,
                Intensity = 1f,
                Active = true,
                HighQualityFiltering = false,
                ThresholdOverride = false,
                IntensityOverride = false,
                HighQualityFilteringOverride = false,
            });

            var track = this.Manager.CreateEntity(typeof(VolumeBloomInitial), typeof(TrackBinding));
            this.Manager.SetComponentData(track, new VolumeBloomInitial
            {
                Value = new VolumeBloom
                {
                    Threshold = 2f,
                    Intensity = 3f,
                    Scatter = 0.8f,
                    Clamp = 999f,
                    DirtIntensity = 0.7f,
                    Active = false,
                    HighQualityFiltering = true,
                    ThresholdOverride = true,
                    IntensityOverride = true,
                    ScatterOverride = true,
                    ClampOverride = true,
                    DirtIntensityOverride = true,
                    HighQualityFilteringOverride = true,
                },
            });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });

            var blob = CreateBlobAssetReference(new VolumeBloomClipBlob { Type = VolumeBloomClipType.Initial });

            try
            {
                this.CreateClip(
                    track,
                    bound,
                    new VolumeBloomAnimated { Value = default },
                    new VolumeBloomClipData { Value = blob, DirtTexture = default });

                var system = this.World.CreateSystem<VolumeBloomTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<VolumeBloom>(bound);
                Assert.AreEqual(2f, result.Threshold, 0.0001f);
                Assert.AreEqual(3f, result.Intensity, 0.0001f);
                Assert.IsFalse(result.Active);
                Assert.IsTrue(result.HighQualityFiltering);
                Assert.IsTrue(result.ThresholdOverride);
                Assert.IsTrue(result.IntensityOverride);
                Assert.IsTrue(result.HighQualityFilteringOverride);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void VolumeBloomTrack_ConstantClip_AppliesConfiguredOverridesOnly()
        {
            var bound = this.Manager.CreateEntity(typeof(VolumeBloom));
            this.Manager.SetComponentData(bound, new VolumeBloom
            {
                Threshold = 1f,
                Intensity = 1f,
                Active = false,
                HighQualityFiltering = false,
                ThresholdOverride = false,
                IntensityOverride = false,
                HighQualityFilteringOverride = false,
            });

            var track = this.Manager.CreateEntity(typeof(VolumeBloomInitial), typeof(TrackBinding));
            this.Manager.SetComponentData(track, new VolumeBloomInitial { Value = default });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });

            var blob = CreateBlobAssetReference(new VolumeBloomClipBlob
            {
                Type = VolumeBloomClipType.Constant,
                Constant = new VolumeBloomConstantData
                {
                    Threshold = 5f,
                    Intensity = 9f,
                    Active = true,
                    ThresholdOverride = true,
                    IntensityOverride = false,
                    HighQualityFilteringOverride = true,
                    HighQualityFiltering = true,
                },
            });

            try
            {
                this.CreateClip(
                    track,
                    bound,
                    new VolumeBloomAnimated { Value = default },
                    new VolumeBloomClipData { Value = blob, DirtTexture = default });

                var system = this.World.CreateSystem<VolumeBloomTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<VolumeBloom>(bound);
                Assert.AreEqual(5f, result.Threshold, 0.0001f);
                Assert.AreEqual(1f, result.Intensity, 0.0001f);
                Assert.IsTrue(result.Active);
                Assert.IsTrue(result.HighQualityFiltering);
                Assert.IsTrue(result.ThresholdOverride);
                Assert.IsFalse(result.IntensityOverride);
                Assert.IsTrue(result.HighQualityFilteringOverride);
            }
            finally
            {
                blob.Dispose();
            }
        }

        private Entity CreateClip<TAnimated, TClipData>(Entity track, Entity bound, in TAnimated animated, in TClipData clipData)
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
    }
}
#endif
