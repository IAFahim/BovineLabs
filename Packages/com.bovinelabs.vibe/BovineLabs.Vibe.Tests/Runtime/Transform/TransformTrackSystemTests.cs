// <copyright file="TransformTrackSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.Runtime.Transform
{
#if BL_REACTION
    using BovineLabs.Reaction.Data.Core;
#endif
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.LocalTransform;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.IntegerTime;
    using Unity.Mathematics;
    using Unity.Transforms;

    public class TransformTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void PositionTrack_UsesClipInitialOnActivationWhenEnabled()
        {
            var bound = this.CreateBoundTransform(LocalTransform.FromPositionRotationScale(new float3(0f, 0f, 0f), quaternion.identity, 1f));
            var track = this.CreateTrackLocalTransformInitial(LocalTransform.FromPositionRotationScale(new float3(10f, 0f, 0f), quaternion.identity, 1f));
            var clipInitial = LocalTransform.FromPositionRotationScale(new float3(20f, 0f, 0f), quaternion.identity, 1f);
            var blob = CreateBlobAssetReference(new PositionClipBlob
            {
                Type = PositionType.Offset,
                TransformOnClipActivation = true,
                Offset = new PositionClipBlob.OffsetData
                {
                    Space = TransformSpace.World,
                    Value = new float3(1f, 0f, 0f),
                },
            });

            try
            {
                this.CreatePositionClip(track, bound, clipInitial, blob, 0.25);
                var system = this.World.CreateSystem<PositionTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<LocalTransform>(bound);
                AssertFloat3(result.Position, new float3(21f, 0f, 0f));
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void PositionTrack_UsesTrackInitialOnActivationWhenClipInitialDisabled()
        {
            var bound = this.CreateBoundTransform(LocalTransform.FromPositionRotationScale(new float3(0f, 0f, 0f), quaternion.identity, 1f));
            var track = this.CreateTrackLocalTransformInitial(LocalTransform.FromPositionRotationScale(new float3(10f, 0f, 0f), quaternion.identity, 1f));
            var clipInitial = LocalTransform.FromPositionRotationScale(new float3(20f, 0f, 0f), quaternion.identity, 1f);
            var blob = CreateBlobAssetReference(new PositionClipBlob
            {
                Type = PositionType.Offset,
                TransformOnClipActivation = false,
                Offset = new PositionClipBlob.OffsetData
                {
                    Space = TransformSpace.World,
                    Value = new float3(1f, 0f, 0f),
                },
            });

            try
            {
                this.CreatePositionClip(track, bound, clipInitial, blob, 0.25);
                var system = this.World.CreateSystem<PositionTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<LocalTransform>(bound);
                AssertFloat3(result.Position, new float3(11f, 0f, 0f));
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void RotationTrack_UsesClipInitialOnActivationWhenEnabled()
        {
            var bound = this.CreateBoundTransform(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            var trackRotation = quaternion.EulerXYZ(math.radians(new float3(0f, 90f, 0f)));
            var clipRotation = quaternion.EulerXYZ(math.radians(new float3(0f, 30f, 0f)));
            var track = this.CreateTrackLocalTransformInitial(LocalTransform.FromPositionRotationScale(float3.zero, trackRotation, 1f));
            var clipInitial = LocalTransform.FromPositionRotationScale(float3.zero, clipRotation, 1f);
            var blob = CreateBlobAssetReference(new RotationClipBlob
            {
                Type = RotationType.LookAtRotation,
                TransformOnClipActivation = true,
                LookAtRotation = new RotationClipBlob.LookAtRotationData
                {
                    Space = TransformSpace.Local,
                    Rotation = quaternion.identity,
                },
            });

            try
            {
                this.CreateRotationClip(track, bound, clipInitial, blob, 0.3);
                var system = this.World.CreateSystem<RotationTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<LocalTransform>(bound);
                AssertQuaternion(result.Rotation, clipRotation);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void RotationTrack_UsesTrackInitialOnActivationWhenClipInitialDisabled()
        {
            var bound = this.CreateBoundTransform(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            var trackRotation = quaternion.EulerXYZ(math.radians(new float3(0f, 90f, 0f)));
            var clipRotation = quaternion.EulerXYZ(math.radians(new float3(0f, 30f, 0f)));
            var track = this.CreateTrackLocalTransformInitial(LocalTransform.FromPositionRotationScale(float3.zero, trackRotation, 1f));
            var clipInitial = LocalTransform.FromPositionRotationScale(float3.zero, clipRotation, 1f);
            var blob = CreateBlobAssetReference(new RotationClipBlob
            {
                Type = RotationType.LookAtRotation,
                TransformOnClipActivation = false,
                LookAtRotation = new RotationClipBlob.LookAtRotationData
                {
                    Space = TransformSpace.Local,
                    Rotation = quaternion.identity,
                },
            });

            try
            {
                this.CreateRotationClip(track, bound, clipInitial, blob, 0.3);
                var system = this.World.CreateSystem<RotationTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<LocalTransform>(bound);
                AssertQuaternion(result.Rotation, trackRotation);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void ScaleTrack_UsesClipInitialOnActivationWhenEnabled()
        {
            var bound = this.CreateBoundTransform(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            var track = this.CreateTrackLocalTransformInitial(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 2f));
            var clipInitial = LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 5f);
            var blob = CreateBlobAssetReference(new ScaleClipBlob
            {
                Type = ScaleType.Curve,
                Curve = new ScaleClipBlob.CurveData
                {
                    TransformOnClipActivation = true,
                },
            });

            try
            {
                this.CreateScaleClip(track, bound, clipInitial, blob, 0.4);
                var system = this.World.CreateSystem<ScaleTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<LocalTransform>(bound);
                Assert.AreEqual(5f, result.Scale, 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void ScaleTrack_UsesTrackInitialOnActivationWhenClipInitialDisabled()
        {
            var bound = this.CreateBoundTransform(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            var track = this.CreateTrackLocalTransformInitial(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 2f));
            var clipInitial = LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 5f);
            var blob = CreateBlobAssetReference(new ScaleClipBlob
            {
                Type = ScaleType.Curve,
                Curve = new ScaleClipBlob.CurveData
                {
                    TransformOnClipActivation = false,
                },
            });

            try
            {
                this.CreateScaleClip(track, bound, clipInitial, blob, 0.4);
                var system = this.World.CreateSystem<ScaleTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<LocalTransform>(bound);
                Assert.AreEqual(2f, result.Scale, 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void PositionTrack_ShakeWithFixedSeed_IsDeterministicAcrossUpdates()
        {
            var bound = this.CreateBoundTransform(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            var track = this.CreateTrackLocalTransformInitial(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            var blob = CreateBlobAssetReference(new PositionClipBlob
            {
                Type = PositionType.Shake,
                TransformOnClipActivation = false,
                Shake = new PositionClipBlob.ShakeData
                {
                    Space = TransformSpace.World,
                    Amplitude = new float3(1.2f, 0.8f, 0.4f),
                    Frequency = 2f,
                    Damping = 0.15f,
                    Seed = 12345u,
                    PerAxisFrequencyMultiplier = new float3(1f, 0.7f, 1.3f),
                },
            });

            try
            {
                this.CreatePositionClip(track, bound, LocalTransform.Identity, blob, 0.37);
                var system = this.World.CreateSystem<PositionTrackSystem>();
                this.RunSystem(system);
                var first = this.Manager.GetComponentData<LocalTransform>(bound).Position;

                this.Manager.SetComponentData(bound, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
                this.RunSystem(system);
                var second = this.Manager.GetComponentData<LocalTransform>(bound).Position;

                AssertFloat3(first, second);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void PositionTrack_WiggleWithFixedSeed_IsDeterministicAcrossUpdates()
        {
            var bound = this.CreateBoundTransform(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            var track = this.CreateTrackLocalTransformInitial(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            var blob = CreateBlobAssetReference(new PositionClipBlob
            {
                Type = PositionType.Wiggle,
                TransformOnClipActivation = false,
                Wiggle = new PositionClipBlob.WiggleData
                {
                    Space = TransformSpace.World,
                    Amplitude = new float3(0.9f, 0.5f, 0.3f),
                    Frequency = 1.8f,
                    Smoothness = 0.5f,
                    Seed = 54321u,
                    PerAxisFrequencyMultiplier = new float3(1f, 1.2f, 0.6f),
                },
            });

            try
            {
                this.CreatePositionClip(track, bound, LocalTransform.Identity, blob, 0.42);
                var system = this.World.CreateSystem<PositionTrackSystem>();
                this.RunSystem(system);
                var first = this.Manager.GetComponentData<LocalTransform>(bound).Position;

                this.Manager.SetComponentData(bound, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
                this.RunSystem(system);
                var second = this.Manager.GetComponentData<LocalTransform>(bound).Position;

                AssertFloat3(first, second);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void PositionTrack_SpringBranch_ProducesFiniteOffset()
        {
            var bound = this.CreateBoundTransform(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            var track = this.CreateTrackLocalTransformInitial(LocalTransform.FromPositionRotationScale(new float3(10f, 0f, 0f), quaternion.identity, 1f));
            var blob = CreateBlobAssetReference(new PositionClipBlob
            {
                Type = PositionType.Spring,
                TransformOnClipActivation = false,
                Spring = new PositionClipBlob.SpringData
                {
                    Mode = PositionClipBlob.SpringData.PositionSpringMode.MoveTo,
                    Space = TransformSpace.World,
#if BL_REACTION
                    Target = Target.None,
#endif
                    RestPoint = new float3(4f, 0f, 0f),
                    InitialVelocity = new float3(0.1f, 0f, 0f),
                    Frequency = new float3(2f, 2f, 2f),
                    Damping = new float3(0.2f, 0.2f, 0.2f),
                },
            });

            try
            {
                this.CreatePositionClip(track, bound, LocalTransform.Identity, blob, 0.5);
                var system = this.World.CreateSystem<PositionTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<LocalTransform>(bound).Position;
                Assert.IsTrue(math.all(math.isfinite(result)));
                Assert.Greater(math.distance(result, new float3(10f, 0f, 0f)), 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void PositionTrack_OrbitBranch_UsesConfiguredAxisAndAngle()
        {
            var bound = this.CreateBoundTransform(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            var track = this.CreateTrackLocalTransformInitial(LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            var blob = CreateBlobAssetReference(new PositionClipBlob
            {
                Type = PositionType.Orbit,
                TransformOnClipActivation = false,
                Orbit = new PositionClipBlob.OrbitData
                {
#if BL_REACTION
                    Target = Target.None,
#endif
                    PivotSpace = TransformSpace.World,
                    AxisSpace = TransformSpace.World,
                    InitialOffsetSpace = TransformSpace.World,
                    UseCustomInitialOffset = true,
                    Radius = 0f,
                    AngularSpeed = math.PI,
                    PivotOffset = float3.zero,
                    Axis = new float3(0f, 1f, 0f),
                    InitialOffset = new float3(1f, 0f, 0f),
                },
            });

            try
            {
                this.CreatePositionClip(track, bound, LocalTransform.Identity, blob, 0.5);
                var system = this.World.CreateSystem<PositionTrackSystem>();
                this.RunSystem(system);

                var result = this.Manager.GetComponentData<LocalTransform>(bound).Position;
                AssertFloat3(result, new float3(0f, 0f, -1f), 0.001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        private static void AssertFloat3(float3 actual, float3 expected, float tolerance = 0.0001f)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance);
            Assert.AreEqual(expected.y, actual.y, tolerance);
            Assert.AreEqual(expected.z, actual.z, tolerance);
        }

        private static void AssertQuaternion(quaternion actual, quaternion expected, float tolerance = 0.0001f)
        {
            Assert.AreEqual(expected.value.x, actual.value.x, tolerance);
            Assert.AreEqual(expected.value.y, actual.value.y, tolerance);
            Assert.AreEqual(expected.value.z, actual.value.z, tolerance);
            Assert.AreEqual(expected.value.w, actual.value.w, tolerance);
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

        private Entity CreateBoundTransform(in LocalTransform localTransform)
        {
            var entity = this.Manager.CreateEntity(typeof(LocalTransform));
            this.Manager.SetComponentData(entity, localTransform);
            return entity;
        }

        private Entity CreateTrackLocalTransformInitial(in LocalTransform localTransform)
        {
            var entity = this.Manager.CreateEntity(typeof(LocalTransformInitial));
            this.Manager.SetComponentData(entity, new LocalTransformInitial { Value = localTransform });
            return entity;
        }

        private Entity CreatePositionClip(
            Entity track,
            Entity bound,
            in LocalTransform clipInitial,
            BlobAssetReference<PositionClipBlob> blob,
            double localTimeSeconds)
        {
            var director = this.Manager.CreateEntity();
            var clip = this.Manager.CreateEntity(
                typeof(PositionAnimated),
                typeof(PositionClipData),
                typeof(LocalTransformClipInitial),
                typeof(Clip),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(ClipActive),
                typeof(ClipActivePrevious),
                typeof(DirectorRoot),
                typeof(ClipBlobCurveCache),
                typeof(LocalTime),
                typeof(TimeTransform));

            this.Manager.SetComponentData(clip, new PositionAnimated { Value = float3.zero });
            this.Manager.SetComponentData(clip, new PositionClipData { Value = blob });
            this.Manager.SetComponentData(clip, new LocalTransformClipInitial { Value = clipInitial });
            this.Manager.SetComponentData(clip, new Clip { Track = track });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
            this.Manager.SetComponentData(clip, new DirectorRoot { Director = director });
            this.Manager.SetComponentData(clip, ClipBlobCurveCache.Create());
            this.Manager.SetComponentData(clip, new LocalTime { Value = new DiscreteTime(localTimeSeconds) });
            this.Manager.SetComponentData(clip, new TimeTransform
            {
                Start = new DiscreteTime(0.0),
                End = new DiscreteTime(1.0),
                ClipIn = DiscreteTime.Zero,
                Scale = 1.0,
            });

            this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

            return clip;
        }

        private Entity CreateRotationClip(
            Entity track,
            Entity bound,
            in LocalTransform clipInitial,
            BlobAssetReference<RotationClipBlob> blob,
            double localTimeSeconds)
        {
            var director = this.Manager.CreateEntity();
            var clip = this.Manager.CreateEntity(
                typeof(RotationAnimated),
                typeof(RotationClipData),
                typeof(LocalTransformClipInitial),
                typeof(Clip),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(ClipActive),
                typeof(ClipActivePrevious),
                typeof(DirectorRoot),
                typeof(ClipBlobCurveCache),
                typeof(LocalTime),
                typeof(TimeTransform));

            this.Manager.SetComponentData(clip, new RotationAnimated { Value = quaternion.identity });
            this.Manager.SetComponentData(clip, new RotationClipData { Value = blob });
            this.Manager.SetComponentData(clip, new LocalTransformClipInitial { Value = clipInitial });
            this.Manager.SetComponentData(clip, new Clip { Track = track });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
            this.Manager.SetComponentData(clip, new DirectorRoot { Director = director });
            this.Manager.SetComponentData(clip, ClipBlobCurveCache.Create());
            this.Manager.SetComponentData(clip, new LocalTime { Value = new DiscreteTime(localTimeSeconds) });
            this.Manager.SetComponentData(clip, new TimeTransform
            {
                Start = new DiscreteTime(0.0),
                End = new DiscreteTime(1.0),
                ClipIn = DiscreteTime.Zero,
                Scale = 1.0,
            });

            this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

            return clip;
        }

        private Entity CreateScaleClip(
            Entity track,
            Entity bound,
            in LocalTransform clipInitial,
            BlobAssetReference<ScaleClipBlob> blob,
            double localTimeSeconds)
        {
            var clip = this.Manager.CreateEntity(
                typeof(ScaleAnimated),
                typeof(ScaleClipData),
                typeof(LocalTransformClipInitial),
                typeof(Clip),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(ClipActive),
                typeof(ClipActivePrevious),
                typeof(ClipBlobCurveCache),
                typeof(LocalTime),
                typeof(TimeTransform));

            this.Manager.SetComponentData(clip, new ScaleAnimated { Value = 0f });
            this.Manager.SetComponentData(clip, new ScaleClipData { Value = blob });
            this.Manager.SetComponentData(clip, new LocalTransformClipInitial { Value = clipInitial });
            this.Manager.SetComponentData(clip, new Clip { Track = track });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
            this.Manager.SetComponentData(clip, ClipBlobCurveCache.Create());
            this.Manager.SetComponentData(clip, new LocalTime { Value = new DiscreteTime(localTimeSeconds) });
            this.Manager.SetComponentData(clip, new TimeTransform
            {
                Start = new DiscreteTime(0.0),
                End = new DiscreteTime(1.0),
                ClipIn = DiscreteTime.Zero,
                Scale = 1.0,
            });

            this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

            return clip;
        }
    }
}
