// <copyright file="AudioTrackSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Tests.Runtime.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.Audio;
    using BovineLabs.Vibe.Tests.Fixtures;
    using BovineLabs.Vibe.Tests.TestDoubles;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.IntegerTime;
    using UnityEngine;

    public class AudioTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void AudioSourceTrack_AnimatedDataClip_WritesVolumeAndPitch()
        {
            var bound = this.Manager.CreateEntity(typeof(AudioSourceData));
            this.Manager.SetComponentData(bound, new AudioSourceData { Volume = 1f, Pitch = 1f });

            var track = this.Manager.CreateEntity(typeof(AudioSourceDataInitial), typeof(TrackBinding));
            this.Manager.SetComponentData(track, new AudioSourceDataInitial
            {
                Value = new AudioSourceData { Volume = 0.5f, Pitch = 1.5f },
            });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });

            var blob = CreateBlobAssetReference(new AudioSourceDataClipBlob
            {
                Type = AudioSourceDataClipType.Animated,
                Data = new AnimatedData
                {
                    Volume = 0.25f,
                    Pitch = 1.75f,
                },
            });

            try
            {
                var clip = this.Manager.CreateEntity(
                    typeof(AudioSourceDataAnimated),
                    typeof(AudioSourceDataClipData),
                    typeof(Clip),
                    typeof(TrackBinding),
                    typeof(TimelineActive),
                    typeof(ClipActive),
                    typeof(ClipActivePrevious));

                this.Manager.SetComponentData(clip, new AudioSourceDataAnimated { Value = default });
                this.Manager.SetComponentData(clip, new AudioSourceDataClipData { Value = blob });
                this.Manager.SetComponentData(clip, new Clip { Track = track });
                this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
                this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

                var system = this.World.CreateSystem<AudioSourceTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<AudioSourceData>(bound);
                Assert.AreEqual(0.25f, result.Volume, 0.0001f);
                Assert.AreEqual(1.75f, result.Pitch, 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void AudioSourceTrack_VolumeSweepClip_UsesTrackInitialAsBase()
        {
            var bound = this.Manager.CreateEntity(typeof(AudioSourceData));
            this.Manager.SetComponentData(bound, new AudioSourceData { Volume = 1f, Pitch = 0.9f });

            var track = this.Manager.CreateEntity(typeof(AudioSourceDataInitial), typeof(TrackBinding));
            this.Manager.SetComponentData(track, new AudioSourceDataInitial
            {
                Value = new AudioSourceData { Volume = 0.7f, Pitch = 1.3f },
            });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });

            var curve = BlobCurveTestHelpers.CreateLinear(0f, 1f, 1f, 1f);
            var blob = CreateBlobAssetReference(new AudioSourceDataClipBlob
            {
                Type = AudioSourceDataClipType.VolumeSweep,
                Sweep = new AudioCurveSweepData
                {
                    Curve = curve.Sampler.Curve.Value,
                    Min = 1f,
                    Max = 1f,
                    Relative = true,
                },
            });

            try
            {
                var clip = this.Manager.CreateEntity(
                    typeof(AudioSourceDataAnimated),
                    typeof(AudioSourceDataClipData),
                    typeof(Clip),
                    typeof(TrackBinding),
                    typeof(TimelineActive),
                    typeof(ClipActive),
                    typeof(ClipActivePrevious),
                    typeof(LocalTime),
                    typeof(ClipBlobCurveCache));

                this.Manager.SetComponentData(clip, new AudioSourceDataAnimated { Value = default });
                this.Manager.SetComponentData(clip, new AudioSourceDataClipData { Value = blob });
                this.Manager.SetComponentData(clip, new Clip { Track = track });
                this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
                this.Manager.SetComponentData(clip, new LocalTime { Value = new DiscreteTime(0.5d) });
                this.Manager.SetComponentData(clip, ClipBlobCurveCache.Create());
                this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

                var system = this.World.CreateSystem<AudioSourceTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<AudioSourceData>(bound);
                Assert.AreEqual(1.7f, result.Volume, 0.0001f);
                Assert.AreEqual(0.9f, result.Pitch, 0.0001f);
            }
            finally
            {
                blob.Dispose();
                curve.Dispose();
            }
        }

        [Test]
        public void AudioSourceTrack_ClipActivation_SwapsClip()
        {
            var originalClip = AudioClip.Create("AudioTrackSystemTests_Original", 32, 1, 44100, false);
            var replacementClip = AudioClip.Create("AudioTrackSystemTests_Replacement", 32, 1, 44100, false);

            var bound = this.Manager.CreateEntity(typeof(AudioSourceDataExtended));
            this.Manager.SetComponentData(bound, new AudioSourceDataExtended { Clip = originalClip, PanStereo = 0f });

            var track = this.Manager.CreateEntity(typeof(AudioSourceClipInitial), typeof(TrackBinding));
            this.Manager.SetComponentData(track, new AudioSourceClipInitial { Clip = default });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });

            try
            {
                var clip = this.Manager.CreateEntity(
                    typeof(AudioSourceClipData),
                    typeof(Clip),
                    typeof(TrackBinding),
                    typeof(ClipActive),
                    typeof(ClipActivePrevious));

                this.Manager.SetComponentData(clip, new AudioSourceClipData { Clip = replacementClip });
                this.Manager.SetComponentData(clip, new Clip { Track = track });
                this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
                this.Manager.SetComponentEnabled<ClipActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

                var system = this.World.CreateSystem<AudioSourceTrackSystem>();
                this.RunSystem(system);

                var afterActivate = this.Manager.GetComponentData<AudioSourceDataExtended>(bound);
                Assert.AreEqual(replacementClip, (AudioClip)afterActivate.Clip);
            }
            finally
            {
                Object.DestroyImmediate(originalClip);
                Object.DestroyImmediate(replacementClip);
            }
        }

        [Test]
        public void AudioSourcePanSweepTrack_WritesSweptPanStereo()
        {
            var bound = this.Manager.CreateEntity(typeof(AudioSourceDataExtended));
            this.Manager.SetComponentData(bound, new AudioSourceDataExtended { PanStereo = 0.2f });

            var track = this.Manager.CreateEntity(typeof(AudioSourcePanInitial), typeof(TrackBinding));
            this.Manager.SetComponentData(track, new AudioSourcePanInitial { Value = 0.2f });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });

            var curve = BlobCurveTestHelpers.CreateLinear(0f, 1f, 1f, 1f);
            var blob = CreateBlobAssetReference(new AudioSourcePanSweepClipBlob
            {
                Sweep = new AudioCurveSweepData
                {
                    Curve = curve.Sampler.Curve.Value,
                    Min = 0.5f,
                    Max = 0.5f,
                    Relative = true,
                },
            });

            try
            {
                var clip = this.Manager.CreateEntity(
                    typeof(AudioSourcePanAnimated),
                    typeof(AudioSourcePanSweepClipData),
                    typeof(Clip),
                    typeof(TrackBinding),
                    typeof(TimelineActive),
                    typeof(ClipActive),
                    typeof(ClipActivePrevious),
                    typeof(LocalTime),
                    typeof(ClipBlobCurveCache));

                this.Manager.SetComponentData(clip, new AudioSourcePanAnimated { Value = 0f });
                this.Manager.SetComponentData(clip, new AudioSourcePanSweepClipData { Value = blob });
                this.Manager.SetComponentData(clip, new Clip { Track = track });
                this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
                this.Manager.SetComponentData(clip, new LocalTime { Value = new DiscreteTime(1.0d) });
                this.Manager.SetComponentData(clip, ClipBlobCurveCache.Create());
                this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

                var system = this.World.CreateSystem<AudioSourcePanSweepTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<AudioSourceDataExtended>(bound);
                Assert.AreEqual(0.7f, result.PanStereo, 0.0001f);
            }
            finally
            {
                blob.Dispose();
                curve.Dispose();
            }
        }

        [Test]
        public void AudioSourceTriggerTrack_PlayAction_UpdatesDataClipAndEnabled()
        {
            var selectedClip = AudioClip.Create("AudioTrackSystemTests_Selected", 16, 1, 44100, false);
            var bound = this.Manager.CreateEntity(typeof(AudioSourceData), typeof(AudioSourceDataExtended), typeof(AudioSourceEnabled));
            this.Manager.SetComponentData(bound, new AudioSourceData { Volume = 1f, Pitch = 1f });
            this.Manager.SetComponentData(bound, new AudioSourceDataExtended { Clip = default, PanStereo = 0f });
            this.Manager.SetComponentEnabled<AudioSourceEnabled>(bound, false);

            var blob = CreateBlobAssetReference(new AudioSourceTriggerClipBlob
            {
                Action = AudioSourceTriggerAction.Play,
                MinVolume = 0.65f,
                MaxVolume = 0.65f,
                MinPitch = 1.2f,
                MaxPitch = 1.2f,
                Seed = 42u,
                ForceRestart = false,
            });

            try
            {
                var clip = this.Manager.CreateEntity(
                    typeof(AudioSourceTriggerClipData),
                    typeof(TrackBinding),
                    typeof(ClipActive),
                    typeof(ClipActivePrevious));

                this.Manager.SetComponentData(clip, new AudioSourceTriggerClipData { Value = blob });
                this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
                var entries = this.Manager.AddBuffer<AudioSourceTriggerClipEntry>(clip);
                entries.Add(new AudioSourceTriggerClipEntry { Clip = selectedClip });
                this.Manager.SetComponentEnabled<ClipActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

                var system = this.World.CreateSystem<AudioSourceTriggerTrackSystem>();
                this.RunSystem(system);

                var source = this.Manager.GetComponentData<AudioSourceData>(bound);
                var sourceExtended = this.Manager.GetComponentData<AudioSourceDataExtended>(bound);
                Assert.AreEqual(0.65f, source.Volume, 0.0001f);
                Assert.AreEqual(1.2f, source.Pitch, 0.0001f);
                Assert.AreEqual(selectedClip, (AudioClip)sourceExtended.Clip);
                Assert.IsTrue(this.Manager.IsComponentEnabled<AudioSourceEnabled>(bound));
            }
            finally
            {
                blob.Dispose();
                Object.DestroyImmediate(selectedClip);
            }
        }

        [Test]
        public void AudioSourceTriggerTrack_StopAction_DisablesAudioSource()
        {
            var bound = this.Manager.CreateEntity(typeof(AudioSourceData), typeof(AudioSourceDataExtended), typeof(AudioSourceEnabled));
            this.Manager.SetComponentData(bound, new AudioSourceData { Volume = 1f, Pitch = 1f });
            this.Manager.SetComponentData(bound, new AudioSourceDataExtended());
            this.Manager.SetComponentEnabled<AudioSourceEnabled>(bound, true);

            var blob = CreateBlobAssetReference(new AudioSourceTriggerClipBlob
            {
                Action = AudioSourceTriggerAction.Stop,
                MinVolume = 1f,
                MaxVolume = 1f,
                MinPitch = 1f,
                MaxPitch = 1f,
                Seed = 7u,
                ForceRestart = false,
            });

            try
            {
                var clip = this.Manager.CreateEntity(
                    typeof(AudioSourceTriggerClipData),
                    typeof(TrackBinding),
                    typeof(ClipActive),
                    typeof(ClipActivePrevious));

                this.Manager.SetComponentData(clip, new AudioSourceTriggerClipData { Value = blob });
                this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
                this.Manager.AddBuffer<AudioSourceTriggerClipEntry>(clip);
                this.Manager.SetComponentEnabled<ClipActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

                var system = this.World.CreateSystem<AudioSourceTriggerTrackSystem>();
                this.RunSystem(system);

                Assert.IsFalse(this.Manager.IsComponentEnabled<AudioSourceEnabled>(bound));
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void AudioSourceTrack_MissingBinding_IsNoOp()
        {
            var bound = this.Manager.CreateEntity(typeof(AudioSourceData));
            this.Manager.SetComponentData(bound, new AudioSourceData { Volume = 0.9f, Pitch = 1.1f });

            var track = this.Manager.CreateEntity(typeof(AudioSourceDataInitial), typeof(TrackBinding));
            this.Manager.SetComponentData(track, new AudioSourceDataInitial
            {
                Value = new AudioSourceData { Volume = 0.25f, Pitch = 2f },
            });
            this.Manager.SetComponentData(track, new TrackBinding { Value = Entity.Null });

            var blob = CreateBlobAssetReference(new AudioSourceDataClipBlob
            {
                Type = AudioSourceDataClipType.Animated,
                Data = new AnimatedData
                {
                    Volume = 0.2f,
                    Pitch = 0.2f,
                },
            });

            try
            {
                var clip = this.Manager.CreateEntity(
                    typeof(AudioSourceDataAnimated),
                    typeof(AudioSourceDataClipData),
                    typeof(Clip),
                    typeof(TrackBinding),
                    typeof(TimelineActive),
                    typeof(ClipActive),
                    typeof(ClipActivePrevious));

                this.Manager.SetComponentData(clip, new AudioSourceDataAnimated { Value = default });
                this.Manager.SetComponentData(clip, new AudioSourceDataClipData { Value = blob });
                this.Manager.SetComponentData(clip, new Clip { Track = track });
                this.Manager.SetComponentData(clip, new TrackBinding { Value = Entity.Null });
                this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

                var system = this.World.CreateSystem<AudioSourceTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<AudioSourceData>(bound);
                Assert.AreEqual(0.9f, result.Volume, 0.0001f);
                Assert.AreEqual(1.1f, result.Pitch, 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void AudioSourceTriggerTrack_MissingBinding_IsNoOp()
        {
            var bound = this.Manager.CreateEntity(typeof(AudioSourceData), typeof(AudioSourceDataExtended), typeof(AudioSourceEnabled));
            this.Manager.SetComponentData(bound, new AudioSourceData { Volume = 0.9f, Pitch = 1.1f });
            this.Manager.SetComponentData(bound, new AudioSourceDataExtended());
            this.Manager.SetComponentEnabled<AudioSourceEnabled>(bound, true);

            var blob = CreateBlobAssetReference(new AudioSourceTriggerClipBlob
            {
                Action = AudioSourceTriggerAction.Play,
                MinVolume = 0.3f,
                MaxVolume = 0.3f,
                MinPitch = 0.8f,
                MaxPitch = 0.8f,
                Seed = 123u,
                ForceRestart = false,
            });

            try
            {
                var clip = this.Manager.CreateEntity(
                    typeof(AudioSourceTriggerClipData),
                    typeof(TrackBinding),
                    typeof(ClipActive),
                    typeof(ClipActivePrevious));

                this.Manager.SetComponentData(clip, new AudioSourceTriggerClipData { Value = blob });
                this.Manager.SetComponentData(clip, new TrackBinding { Value = Entity.Null });
                this.Manager.AddBuffer<AudioSourceTriggerClipEntry>(clip);
                this.Manager.SetComponentEnabled<ClipActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

                var system = this.World.CreateSystem<AudioSourceTriggerTrackSystem>();
                this.RunSystem(system);

                var source = this.Manager.GetComponentData<AudioSourceData>(bound);
                Assert.AreEqual(0.9f, source.Volume, 0.0001f);
                Assert.AreEqual(1.1f, source.Pitch, 0.0001f);
                Assert.IsTrue(this.Manager.IsComponentEnabled<AudioSourceEnabled>(bound));
            }
            finally
            {
                blob.Dispose();
            }
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
