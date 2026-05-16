// <copyright file="AudioFilterTrackSystemTests.cs" company="BovineLabs">
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

    public class AudioFilterTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void ChorusFilter_AnimatedClip_WritesExpectedMetric()
        {
            this.RunAnimatedClipTest<AudioChorusFilterTrackSystem, AudioChorusFilterData, AudioChorusFilterInitial, AudioChorusFilterAnimated, AudioChorusFilterClipData, AudioChorusFilterClipBlob>(
                new AudioChorusFilterData { Depth = 0.2f },
                new AudioChorusFilterInitial { Value = new AudioChorusFilterData { Depth = 0.2f } },
                new AudioChorusFilterClipBlob { Type = AudioChorusFilterClipType.Animated, Data = new AudioChorusFilterConstantData { Depth = 0.8f } },
                blob => new AudioChorusFilterClipData { Value = blob },
                value => value.Depth,
                0.8f);

        }

        [Test]
        public void DistortionFilter_AnimatedClip_WritesExpectedMetric()
        {
            this.RunAnimatedClipTest<AudioDistortionFilterTrackSystem, AudioDistortionFilterData, AudioDistortionFilterInitial, AudioDistortionFilterAnimated, AudioDistortionFilterClipData, AudioDistortionFilterClipBlob>(
                new AudioDistortionFilterData { DistortionLevel = 0.2f },
                new AudioDistortionFilterInitial { Value = new AudioDistortionFilterData { DistortionLevel = 0.2f } },
                new AudioDistortionFilterClipBlob
                {
                    Type = AudioDistortionFilterClipType.Animated,
                    Data = new AudioDistortionFilterConstantData { DistortionLevel = 0.7f },
                },
                blob => new AudioDistortionFilterClipData { Value = blob },
                value => value.DistortionLevel,
                0.7f);
        }

        [Test]
        public void EchoFilter_AnimatedClip_WritesExpectedMetric()
        {
            this.RunAnimatedClipTest<AudioEchoFilterTrackSystem, AudioEchoFilterData, AudioEchoFilterInitial, AudioEchoFilterAnimated, AudioEchoFilterClipData, AudioEchoFilterClipBlob>(
                new AudioEchoFilterData { WetMix = 0.2f },
                new AudioEchoFilterInitial { Value = new AudioEchoFilterData { WetMix = 0.2f } },
                new AudioEchoFilterClipBlob { Type = AudioEchoFilterClipType.Animated, Data = new AudioEchoFilterConstantData { WetMix = 0.6f } },
                blob => new AudioEchoFilterClipData { Value = blob },
                value => value.WetMix,
                0.6f);
        }

        [Test]
        public void HighPassFilter_AnimatedClip_WritesExpectedMetric()
        {
            this.RunAnimatedClipTest<AudioHighPassFilterTrackSystem, AudioHighPassFilterData, AudioHighPassFilterInitial, AudioHighPassFilterAnimated, AudioHighPassFilterClipData, AudioHighPassFilterClipBlob>(
                new AudioHighPassFilterData { CutoffFrequency = 300f },
                new AudioHighPassFilterInitial { Value = new AudioHighPassFilterData { CutoffFrequency = 300f } },
                new AudioHighPassFilterClipBlob
                {
                    Type = AudioHighPassFilterClipType.Animated,
                    Data = new AudioHighPassFilterConstantData { CutoffFrequency = 1200f },
                },
                blob => new AudioHighPassFilterClipData { Value = blob },
                value => value.CutoffFrequency,
                1200f);
        }

        [Test]
        public void LowPassFilter_AnimatedClip_WritesExpectedMetric()
        {
            this.RunAnimatedClipTest<AudioLowPassFilterTrackSystem, AudioLowPassFilterData, AudioLowPassFilterInitial, AudioLowPassFilterAnimated, AudioLowPassFilterClipData, AudioLowPassFilterClipBlob>(
                new AudioLowPassFilterData { CutoffFrequency = 700f },
                new AudioLowPassFilterInitial { Value = new AudioLowPassFilterData { CutoffFrequency = 700f } },
                new AudioLowPassFilterClipBlob
                {
                    Type = AudioLowPassFilterClipType.Animated,
                    Data = new AudioLowPassFilterConstantData { CutoffFrequency = 200f },
                },
                blob => new AudioLowPassFilterClipData { Value = blob },
                value => value.CutoffFrequency,
                200f);
        }

        [Test]
        public void ReverbFilter_AnimatedClip_WritesExpectedMetric()
        {
            this.RunAnimatedClipTest<AudioReverbFilterTrackSystem, AudioReverbFilterData, AudioReverbFilterInitial, AudioReverbFilterAnimated, AudioReverbFilterClipData, AudioReverbFilterClipBlob>(
                new AudioReverbFilterData { ReverbLevel = -500f },
                new AudioReverbFilterInitial { Value = new AudioReverbFilterData { ReverbLevel = -500f } },
                new AudioReverbFilterClipBlob
                {
                    Type = AudioReverbFilterClipType.Animated,
                    Data = new AudioReverbFilterConstantData { ReverbLevel = -150f, OverrideReverbPreset = true },
                },
                blob => new AudioReverbFilterClipData { Value = blob },
                value => value.ReverbLevel,
                -150f);
        }

        [Test]
        public void ChorusFilter_SweepClip_UsesInitialBaseMetric()
        {
            this.RunSweepClipTest<AudioChorusFilterTrackSystem, AudioChorusFilterData, AudioChorusFilterInitial, AudioChorusFilterAnimated, AudioChorusFilterClipData, AudioChorusFilterClipBlob>(
                new AudioChorusFilterData { Depth = 0.2f },
                new AudioChorusFilterInitial { Value = new AudioChorusFilterData { Depth = 0.2f } },
                curve => new AudioChorusFilterClipBlob
                {
                    Type = AudioChorusFilterClipType.Sweep,
                    Sweep = new AudioCurveSweepData { Curve = curve, Min = 1f, Max = 1f, Relative = true },
                },
                blob => new AudioChorusFilterClipData { Value = blob },
                value => value.Depth,
                1.2f);
        }

        [Test]
        public void DistortionFilter_SweepClip_UsesInitialBaseMetric()
        {
            this.RunSweepClipTest<AudioDistortionFilterTrackSystem, AudioDistortionFilterData, AudioDistortionFilterInitial, AudioDistortionFilterAnimated, AudioDistortionFilterClipData, AudioDistortionFilterClipBlob>(
                new AudioDistortionFilterData { DistortionLevel = 0.3f },
                new AudioDistortionFilterInitial { Value = new AudioDistortionFilterData { DistortionLevel = 0.3f } },
                curve => new AudioDistortionFilterClipBlob
                {
                    Type = AudioDistortionFilterClipType.Sweep,
                    Sweep = new AudioCurveSweepData { Curve = curve, Min = 1f, Max = 1f, Relative = true },
                },
                blob => new AudioDistortionFilterClipData { Value = blob },
                value => value.DistortionLevel,
                1.3f);
        }

        [Test]
        public void EchoFilter_SweepClip_UsesInitialBaseMetric()
        {
            this.RunSweepClipTest<AudioEchoFilterTrackSystem, AudioEchoFilterData, AudioEchoFilterInitial, AudioEchoFilterAnimated, AudioEchoFilterClipData, AudioEchoFilterClipBlob>(
                new AudioEchoFilterData { WetMix = 0.25f },
                new AudioEchoFilterInitial { Value = new AudioEchoFilterData { WetMix = 0.25f } },
                curve => new AudioEchoFilterClipBlob
                {
                    Type = AudioEchoFilterClipType.Sweep,
                    Sweep = new AudioCurveSweepData { Curve = curve, Min = 1f, Max = 1f, Relative = true },
                },
                blob => new AudioEchoFilterClipData { Value = blob },
                value => value.WetMix,
                1.25f);
        }

        [Test]
        public void HighPassFilter_SweepClip_UsesInitialBaseMetric()
        {
            this.RunSweepClipTest<AudioHighPassFilterTrackSystem, AudioHighPassFilterData, AudioHighPassFilterInitial, AudioHighPassFilterAnimated, AudioHighPassFilterClipData, AudioHighPassFilterClipBlob>(
                new AudioHighPassFilterData { CutoffFrequency = 250f },
                new AudioHighPassFilterInitial { Value = new AudioHighPassFilterData { CutoffFrequency = 250f } },
                curve => new AudioHighPassFilterClipBlob
                {
                    Type = AudioHighPassFilterClipType.Sweep,
                    Sweep = new AudioCurveSweepData { Curve = curve, Min = 200f, Max = 200f, Relative = true },
                },
                blob => new AudioHighPassFilterClipData { Value = blob },
                value => value.CutoffFrequency,
                450f);
        }

        [Test]
        public void LowPassFilter_SweepClip_UsesInitialBaseMetric()
        {
            this.RunSweepClipTest<AudioLowPassFilterTrackSystem, AudioLowPassFilterData, AudioLowPassFilterInitial, AudioLowPassFilterAnimated, AudioLowPassFilterClipData, AudioLowPassFilterClipBlob>(
                new AudioLowPassFilterData { CutoffFrequency = 500f },
                new AudioLowPassFilterInitial { Value = new AudioLowPassFilterData { CutoffFrequency = 500f } },
                curve => new AudioLowPassFilterClipBlob
                {
                    Type = AudioLowPassFilterClipType.Sweep,
                    Sweep = new AudioCurveSweepData { Curve = curve, Min = 100f, Max = 100f, Relative = true },
                },
                blob => new AudioLowPassFilterClipData { Value = blob },
                value => value.CutoffFrequency,
                600f);
        }

        [Test]
        public void ReverbFilter_SweepClip_UsesInitialBaseMetric()
        {
            this.RunSweepClipTest<AudioReverbFilterTrackSystem, AudioReverbFilterData, AudioReverbFilterInitial, AudioReverbFilterAnimated, AudioReverbFilterClipData, AudioReverbFilterClipBlob>(
                new AudioReverbFilterData { ReverbLevel = -600f },
                new AudioReverbFilterInitial { Value = new AudioReverbFilterData { ReverbLevel = -600f } },
                curve => new AudioReverbFilterClipBlob
                {
                    Type = AudioReverbFilterClipType.Sweep,
                    Sweep = new AudioCurveSweepData { Curve = curve, Min = 100f, Max = 100f, Relative = true },
                },
                blob => new AudioReverbFilterClipData { Value = blob },
                value => value.ReverbLevel,
                -500f);
        }

        [Test]
        public void ChorusFilter_MissingBinding_IsNoOp()
        {
            this.RunMissingBindingTest<AudioChorusFilterTrackSystem, AudioChorusFilterData, AudioChorusFilterInitial, AudioChorusFilterAnimated, AudioChorusFilterClipData, AudioChorusFilterClipBlob>(
                new AudioChorusFilterData { Depth = 0.2f },
                new AudioChorusFilterInitial { Value = default },
                new AudioChorusFilterClipBlob { Type = AudioChorusFilterClipType.Animated, Data = new AudioChorusFilterConstantData { Depth = 1f } },
                blob => new AudioChorusFilterClipData { Value = blob },
                value => value.Depth,
                0.2f);
        }

        [Test]
        public void DistortionFilter_MissingBinding_IsNoOp()
        {
            this.RunMissingBindingTest<AudioDistortionFilterTrackSystem, AudioDistortionFilterData, AudioDistortionFilterInitial, AudioDistortionFilterAnimated, AudioDistortionFilterClipData, AudioDistortionFilterClipBlob>(
                new AudioDistortionFilterData { DistortionLevel = 0.4f },
                new AudioDistortionFilterInitial { Value = default },
                new AudioDistortionFilterClipBlob
                {
                    Type = AudioDistortionFilterClipType.Animated,
                    Data = new AudioDistortionFilterConstantData { DistortionLevel = 0.9f },
                },
                blob => new AudioDistortionFilterClipData { Value = blob },
                value => value.DistortionLevel,
                0.4f);
        }

        [Test]
        public void EchoFilter_MissingBinding_IsNoOp()
        {
            this.RunMissingBindingTest<AudioEchoFilterTrackSystem, AudioEchoFilterData, AudioEchoFilterInitial, AudioEchoFilterAnimated, AudioEchoFilterClipData, AudioEchoFilterClipBlob>(
                new AudioEchoFilterData { WetMix = 0.6f },
                new AudioEchoFilterInitial { Value = default },
                new AudioEchoFilterClipBlob { Type = AudioEchoFilterClipType.Animated, Data = new AudioEchoFilterConstantData { WetMix = 0.1f } },
                blob => new AudioEchoFilterClipData { Value = blob },
                value => value.WetMix,
                0.6f);
        }

        [Test]
        public void HighPassFilter_MissingBinding_IsNoOp()
        {
            this.RunMissingBindingTest<AudioHighPassFilterTrackSystem, AudioHighPassFilterData, AudioHighPassFilterInitial, AudioHighPassFilterAnimated, AudioHighPassFilterClipData, AudioHighPassFilterClipBlob>(
                new AudioHighPassFilterData { CutoffFrequency = 200f },
                new AudioHighPassFilterInitial { Value = default },
                new AudioHighPassFilterClipBlob
                {
                    Type = AudioHighPassFilterClipType.Animated,
                    Data = new AudioHighPassFilterConstantData { CutoffFrequency = 1200f },
                },
                blob => new AudioHighPassFilterClipData { Value = blob },
                value => value.CutoffFrequency,
                200f);
        }

        [Test]
        public void LowPassFilter_MissingBinding_IsNoOp()
        {
            this.RunMissingBindingTest<AudioLowPassFilterTrackSystem, AudioLowPassFilterData, AudioLowPassFilterInitial, AudioLowPassFilterAnimated, AudioLowPassFilterClipData, AudioLowPassFilterClipBlob>(
                new AudioLowPassFilterData { CutoffFrequency = 350f },
                new AudioLowPassFilterInitial { Value = default },
                new AudioLowPassFilterClipBlob
                {
                    Type = AudioLowPassFilterClipType.Animated,
                    Data = new AudioLowPassFilterConstantData { CutoffFrequency = 50f },
                },
                blob => new AudioLowPassFilterClipData { Value = blob },
                value => value.CutoffFrequency,
                350f);
        }

        [Test]
        public void ReverbFilter_MissingBinding_IsNoOp()
        {
            this.RunMissingBindingTest<AudioReverbFilterTrackSystem, AudioReverbFilterData, AudioReverbFilterInitial, AudioReverbFilterAnimated, AudioReverbFilterClipData, AudioReverbFilterClipBlob>(
                new AudioReverbFilterData { ReverbLevel = -500f },
                new AudioReverbFilterInitial { Value = default },
                new AudioReverbFilterClipBlob
                {
                    Type = AudioReverbFilterClipType.Animated,
                    Data = new AudioReverbFilterConstantData { ReverbLevel = -100f },
                },
                blob => new AudioReverbFilterClipData { Value = blob },
                value => value.ReverbLevel,
                -500f);
        }

        private void RunAnimatedClipTest<TSystem, TFilter, TInitial, TAnimated, TClipData, TClipBlob>(
            in TFilter targetValue,
            in TInitial initialValue,
            in TClipBlob clipBlobValue,
            System.Func<BlobAssetReference<TClipBlob>, TClipData> clipDataFactory,
            System.Func<TFilter, float> readMetric,
            float expectedMetric)
            where TSystem : unmanaged, ISystem
            where TFilter : unmanaged, IComponentData
            where TInitial : unmanaged, IComponentData
            where TAnimated : unmanaged, IComponentData
            where TClipData : unmanaged, IComponentData
            where TClipBlob : unmanaged
        {
            var bound = this.Manager.CreateEntity(typeof(TFilter));
            this.Manager.SetComponentData(bound, targetValue);
            var track = this.CreateTrack(bound, initialValue);
            var blob = CreateBlobAssetReference(clipBlobValue);

            try
            {
                _ = this.CreateClip<TAnimated, TClipData>(track, bound, clipDataFactory(blob), includeSweepState: false);
                var system = this.World.CreateSystem<TSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<TFilter>(bound);
                Assert.AreEqual(expectedMetric, readMetric(result), 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        private void RunSweepClipTest<TSystem, TFilter, TInitial, TAnimated, TClipData, TClipBlob>(
            in TFilter targetValue,
            in TInitial initialValue,
            System.Func<BovineLabs.Core.Collections.BlobCurve, TClipBlob> clipBlobFactory,
            System.Func<BlobAssetReference<TClipBlob>, TClipData> clipDataFactory,
            System.Func<TFilter, float> readMetric,
            float expectedMetric)
            where TSystem : unmanaged, ISystem
            where TFilter : unmanaged, IComponentData
            where TInitial : unmanaged, IComponentData
            where TAnimated : unmanaged, IComponentData
            where TClipData : unmanaged, IComponentData
            where TClipBlob : unmanaged
        {
            var bound = this.Manager.CreateEntity(typeof(TFilter));
            this.Manager.SetComponentData(bound, targetValue);
            var track = this.CreateTrack(bound, initialValue);
            var curve = BlobCurveTestHelpers.CreateLinear(0f, 1f, 1f, 1f);
            var blob = CreateBlobAssetReference(clipBlobFactory(curve.Sampler.Curve.Value));

            try
            {
                _ = this.CreateClip<TAnimated, TClipData>(track, bound, clipDataFactory(blob), includeSweepState: true);
                var system = this.World.CreateSystem<TSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<TFilter>(bound);
                Assert.AreEqual(expectedMetric, readMetric(result), 0.0001f);
            }
            finally
            {
                blob.Dispose();
                curve.Dispose();
            }
        }

        private void RunMissingBindingTest<TSystem, TFilter, TInitial, TAnimated, TClipData, TClipBlob>(
            in TFilter targetValue,
            in TInitial initialValue,
            in TClipBlob clipBlobValue,
            System.Func<BlobAssetReference<TClipBlob>, TClipData> clipDataFactory,
            System.Func<TFilter, float> readMetric,
            float expectedMetric)
            where TSystem : unmanaged, ISystem
            where TFilter : unmanaged, IComponentData
            where TInitial : unmanaged, IComponentData
            where TAnimated : unmanaged, IComponentData
            where TClipData : unmanaged, IComponentData
            where TClipBlob : unmanaged
        {
            var bound = this.Manager.CreateEntity(typeof(TFilter));
            this.Manager.SetComponentData(bound, targetValue);
            var track = this.CreateTrack(Entity.Null, initialValue);
            var blob = CreateBlobAssetReference(clipBlobValue);

            try
            {
                _ = this.CreateClip<TAnimated, TClipData>(track, Entity.Null, clipDataFactory(blob), includeSweepState: false);
                var system = this.World.CreateSystem<TSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<TFilter>(bound);
                Assert.AreEqual(expectedMetric, readMetric(result), 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        private Entity CreateTrack<TInitial>(Entity bound, in TInitial initial)
            where TInitial : unmanaged, IComponentData
        {
            var track = this.Manager.CreateEntity(typeof(TInitial), typeof(TrackBinding));
            this.Manager.SetComponentData(track, initial);
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });
            return track;
        }

        private Entity CreateClip<TAnimated, TClipData>(Entity track, Entity bound, in TClipData clipData, bool includeSweepState)
            where TAnimated : unmanaged, IComponentData
            where TClipData : unmanaged, IComponentData
        {
            Entity clip;
            if (includeSweepState)
            {
                clip = this.Manager.CreateEntity(
                    typeof(TAnimated),
                    typeof(TClipData),
                    typeof(Clip),
                    typeof(TrackBinding),
                    typeof(TimelineActive),
                    typeof(ClipActive),
                    typeof(ClipActivePrevious),
                    typeof(LocalTime),
                    typeof(ClipBlobCurveCache));

                this.Manager.SetComponentData(clip, new LocalTime { Value = new DiscreteTime(0.5d) });
                this.Manager.SetComponentData(clip, ClipBlobCurveCache.Create());
            }
            else
            {
                clip = this.Manager.CreateEntity(
                    typeof(TAnimated),
                    typeof(TClipData),
                    typeof(Clip),
                    typeof(TrackBinding),
                    typeof(TimelineActive),
                    typeof(ClipActive),
                    typeof(ClipActivePrevious));
            }

            this.Manager.SetComponentData(clip, default(TAnimated));
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
