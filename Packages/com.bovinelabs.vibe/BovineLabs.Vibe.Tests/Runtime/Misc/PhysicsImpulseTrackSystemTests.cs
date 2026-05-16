// <copyright file="PhysicsImpulseTrackSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS

namespace BovineLabs.Vibe.Tests.Runtime.Misc
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Physics;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Physics;
    using Unity.Transforms;

    public class PhysicsImpulseTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void WorldImpulse_AppliesLinearVelocityWithoutTransform()
        {
            var bound = this.CreateBoundBody(withTransform: false);
            var clipBlob = CreateBlobAssetReference(
                new PhysicsImpulseClipBlob
                {
                    Type = PhysicsImpulseClipType.World,
                    World = new PhysicsImpulseClipBlob.WorldImpulseData
                    {
                        Impulse = new float3(2f, -1f, 0.5f),
                    },
                });

            try
            {
                this.CreateClip(bound, clipBlob);

                var system = this.World.CreateSystem<PhysicsImpulseTrackSystem>();
                this.RunSystem(system);

                var velocity = this.Manager.GetComponentData<PhysicsVelocity>(bound);
                AssertFloat3(velocity.Linear, new float3(2f, -1f, 0.5f));
                AssertFloat3(velocity.Angular, float3.zero);
            }
            finally
            {
                clipBlob.Dispose();
            }
        }

        [Test]
        public void LocalAxisImpulse_UsesLocalTransformDirection()
        {
            var rotation = quaternion.EulerXYZ(0f, math.radians(90f), 0f);
            var bound = this.CreateBoundBody(
                withTransform: true,
                localTransform: LocalTransform.FromPositionRotationScale(float3.zero, rotation, 1f));

            var clipBlob = CreateBlobAssetReference(
                new PhysicsImpulseClipBlob
                {
                    Type = PhysicsImpulseClipType.LocalAxis,
                    LocalAxis = new PhysicsImpulseClipBlob.LocalAxisImpulseData
                    {
                        Axis = PhysicsImpulseAxis.Forward,
                        Magnitude = 3f,
                    },
                });

            try
            {
                this.CreateClip(bound, clipBlob);

                var system = this.World.CreateSystem<PhysicsImpulseTrackSystem>();
                this.RunSystem(system);

                var velocity = this.Manager.GetComponentData<PhysicsVelocity>(bound);
                Assert.AreEqual(3f, velocity.Linear.x, 0.0001f);
                Assert.AreEqual(0f, velocity.Linear.y, 0.0001f);
                Assert.AreEqual(0f, velocity.Linear.z, 0.0001f);
            }
            finally
            {
                clipBlob.Dispose();
            }
        }

        [TestCase(PhysicsImpulseClipType.WorldTorque)]
        [TestCase(PhysicsImpulseClipType.LocalTorque)]
        [TestCase(PhysicsImpulseClipType.ImpulseAtPoint)]
        public void ImpulseVariants_ModifyVelocity(PhysicsImpulseClipType type)
        {
            var bound = this.CreateBoundBody(withTransform: true);
            var clipBlob = CreateImpulseVariantBlob(type);

            try
            {
                this.CreateClip(bound, clipBlob);

                var system = this.World.CreateSystem<PhysicsImpulseTrackSystem>();
                this.RunSystem(system);

                var velocity = this.Manager.GetComponentData<PhysicsVelocity>(bound);

                if (type == PhysicsImpulseClipType.ImpulseAtPoint)
                {
                    Assert.Greater(math.lengthsq(velocity.Linear), 0f);
                    Assert.Greater(math.lengthsq(velocity.Angular), 0f);
                }
                else
                {
                    Assert.Greater(math.lengthsq(velocity.Angular), 0f);
                }
            }
            finally
            {
                clipBlob.Dispose();
            }
        }

        [Test]
        public void RadialImpulse_OutsideRadius_NoVelocityChange()
        {
            var bound = this.CreateBoundBody(
                withTransform: true,
                localTransform: LocalTransform.FromPositionRotationScale(new float3(5f, 0f, 0f), quaternion.identity, 1f));

            var clipBlob = CreateBlobAssetReference(
                new PhysicsImpulseClipBlob
                {
                    Type = PhysicsImpulseClipType.Radial,
                    Radial = new PhysicsImpulseClipBlob.RadialImpulseData
                    {
                        Origin = float3.zero,
                        Radius = 1f,
                        Strength = 8f,
                        UpAxis = float3.zero,
                        FalloffCurve = default,
                    },
                });

            try
            {
                this.CreateClip(bound, clipBlob);

                var system = this.World.CreateSystem<PhysicsImpulseTrackSystem>();
                this.RunSystem(system);

                var velocity = this.Manager.GetComponentData<PhysicsVelocity>(bound);
                AssertFloat3(velocity.Linear, float3.zero);
                AssertFloat3(velocity.Angular, float3.zero);
            }
            finally
            {
                clipBlob.Dispose();
            }
        }

        [Test]
        public void RadialImpulse_WithUpAxis_ProjectsAndAppliesPlanarImpulse()
        {
            var bound = this.CreateBoundBody(
                withTransform: true,
                localTransform: LocalTransform.FromPositionRotationScale(new float3(2f, 3f, 0f), quaternion.identity, 1f));

            var clipBlob = CreateBlobAssetReference(
                new PhysicsImpulseClipBlob
                {
                    Type = PhysicsImpulseClipType.Radial,
                    Radial = new PhysicsImpulseClipBlob.RadialImpulseData
                    {
                        Origin = float3.zero,
                        Radius = 4f,
                        Strength = 5f,
                        UpAxis = new float3(0f, 1f, 0f),
                        FalloffCurve = default,
                    },
                });

            try
            {
                this.CreateClip(bound, clipBlob);

                var system = this.World.CreateSystem<PhysicsImpulseTrackSystem>();
                this.RunSystem(system);

                var velocity = this.Manager.GetComponentData<PhysicsVelocity>(bound);
                Assert.Greater(velocity.Linear.x, 0f);
                Assert.AreEqual(0f, velocity.Linear.y, 0.0001f);
                Assert.AreEqual(0f, velocity.Linear.z, 0.0001f);
            }
            finally
            {
                clipBlob.Dispose();
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

        private static BlobAssetReference<PhysicsImpulseClipBlob> CreateImpulseVariantBlob(PhysicsImpulseClipType type)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<PhysicsImpulseClipBlob>();
            root.Type = type;

            switch (type)
            {
                case PhysicsImpulseClipType.WorldTorque:
                    root.WorldTorque = new PhysicsImpulseClipBlob.WorldTorqueImpulseData { Torque = new float3(0f, 2f, 0f) };
                    break;
                case PhysicsImpulseClipType.LocalTorque:
                    root.LocalTorque = new PhysicsImpulseClipBlob.LocalTorqueImpulseData { Torque = new float3(0f, 0f, 2f) };
                    break;
                case PhysicsImpulseClipType.ImpulseAtPoint:
                    root.Point = new PhysicsImpulseClipBlob.PointImpulseData
                    {
                        Impulse = new float3(0f, 1f, 0f),
                        Point = new float3(1f, 0f, 0f),
                    };
                    break;
            }

            var blob = builder.CreateBlobAssetReference<PhysicsImpulseClipBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static void AssertFloat3(float3 actual, float3 expected, float tolerance = 0.0001f)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance);
            Assert.AreEqual(expected.y, actual.y, tolerance);
            Assert.AreEqual(expected.z, actual.z, tolerance);
        }

        private Entity CreateBoundBody(bool withTransform)
        {
            return this.CreateBoundBody(withTransform, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
        }

        private Entity CreateBoundBody(bool withTransform, in LocalTransform localTransform)
        {
            var entity = withTransform
                ? this.Manager.CreateEntity(typeof(PhysicsVelocity), typeof(PhysicsMass), typeof(LocalTransform))
                : this.Manager.CreateEntity(typeof(PhysicsVelocity), typeof(PhysicsMass));

            this.Manager.SetComponentData(entity, PhysicsVelocity.Zero);
            this.Manager.SetComponentData(entity, PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 1f));

            if (withTransform)
            {
                this.Manager.SetComponentData(entity, localTransform);
            }

            return entity;
        }

        private Entity CreateClip(Entity bound, BlobAssetReference<PhysicsImpulseClipBlob> clipBlob)
        {
            var clip = this.Manager.CreateEntity(
                typeof(PhysicsImpulseClipData),
                typeof(TrackBinding),
                typeof(ClipActive),
                typeof(ClipActivePrevious));

            this.Manager.SetComponentData(clip, new PhysicsImpulseClipData { Value = clipBlob });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);
            return clip;
        }
    }
}

#endif
