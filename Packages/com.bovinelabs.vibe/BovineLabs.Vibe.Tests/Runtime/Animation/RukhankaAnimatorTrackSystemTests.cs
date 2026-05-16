// <copyright file="RukhankaAnimatorTrackSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Tests.Runtime.Animation
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Animation;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.Animation;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Rukhanka;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.IntegerTime;
    using UnityEngine;
    using BlobCurve = BovineLabs.Core.Collections.BlobCurve;

    public class RukhankaAnimatorTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void StateUtility_GetStateIndex_InvalidInputs_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, RukhankaAnimatorStateUtility.GetStateIndex(default, 0, 123u));

            var controller = CreateControllerBlob(layerCount: 0, stateCountPerLayer: 0);
            try
            {
                Assert.AreEqual(-1, RukhankaAnimatorStateUtility.GetStateIndex(controller, 0, 123u));
                Assert.AreEqual(-1, RukhankaAnimatorStateUtility.GetStateIndex(controller, -1, 123u));
                Assert.AreEqual(-1, RukhankaAnimatorStateUtility.GetStateIndex(controller, 0, 0u));
            }
            finally
            {
                controller.Dispose();
            }
        }

        [Test]
        public void StateUtility_GetStateDurationSeconds_InvalidData_ReturnsDefaultDuration()
        {
            var controller = CreateControllerBlob(layerCount: 1, stateCountPerLayer: 0);
            var animations = CreateControllerAnimationsBlob(animationCount: 0);

            try
            {
                var duration = RukhankaAnimatorStateUtility.GetStateDurationSeconds(layerIndex: 0, stateIndex: 0, controllerBlob: controller,
                    controllerAnimationsBlob: animations, layers: default, parameters: default, blobDatabase: default);

                Assert.AreEqual(1f, duration, 0.0001f);
            }
            finally
            {
                controller.Dispose();
                animations.Dispose();
            }
        }

        [Test]
        public void ParameterTrack_Deactivate_RestoresTrackedValues()
        {
            var bound = this.CreateBoundAnimatorEntity();
            var parameters = this.Manager.AddBuffer<AnimatorControllerParameterComponent>(bound);
            parameters.Add(CreateParameter(101u, ControllerParameterType.Float, new ParameterValue { floatValue = 55f }));
            parameters.Add(CreateParameter(202u, ControllerParameterType.Bool, new ParameterValue { boolValue = true }));
            parameters.Add(CreateParameter(303u, ControllerParameterType.Int, new ParameterValue { intValue = -20 }));

            var layers = this.Manager.AddBuffer<AnimatorControllerLayerComponent>(bound);
            layers.Add(new AnimatorControllerLayerComponent
            {
                weight = 0.2f,
                speed = 1f,
            });

            layers.Add(new AnimatorControllerLayerComponent
            {
                weight = 0.15f,
                speed = 1f,
            });

            var track = this.Manager.CreateEntity(typeof(TrackBinding), typeof(TrackResetOnDeactivate), typeof(TimelineActive), typeof(TimelineActivePrevious));

            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<TimelineActive>(track, false);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, true);

            // Gate the system's RequireForUpdate without activating any clip work.
            this.Manager.CreateEntity(typeof(RukhankaAnimatorParameterClipData));

            var parameterInitials = this.Manager.AddBuffer<RukhankaAnimatorParameterInitial>(track);
            parameterInitials.Add(new RukhankaAnimatorParameterInitial
            {
                Hash = 101u,
                Value = new ParameterValue { floatValue = 1.5f },
            });

            parameterInitials.Add(new RukhankaAnimatorParameterInitial
            {
                Hash = 303u,
                Value = new ParameterValue { intValue = 10 },
            });

            var layerInitials = this.Manager.AddBuffer<RukhankaAnimatorLayerInitial>(track);
            layerInitials.Add(new RukhankaAnimatorLayerInitial
            {
                LayerIndex = 1,
                Weight = 0.8f,
            });

            var system = this.World.CreateSystem<RukhankaAnimatorParameterTrackSystem>();
            this.RunSystem(system);

            parameters = this.Manager.GetBuffer<AnimatorControllerParameterComponent>(bound);
            Assert.AreEqual(1.5f, GetParameter(parameters, 101u).value.floatValue, 0.0001f);
            Assert.AreEqual(10, GetParameter(parameters, 303u).value.intValue);

            layers = this.Manager.GetBuffer<AnimatorControllerLayerComponent>(bound);
            Assert.AreEqual(0.8f, layers[1].weight, 0.0001f);
        }

        [Test]
        public void ParameterTrack_ClipActivate_AppliesConfiguredParameterModesAndLayerWeight()
        {
            var bound = this.CreateBoundAnimatorEntity();
            var parameters = this.Manager.AddBuffer<AnimatorControllerParameterComponent>(bound);
            parameters.Add(CreateParameter(11u, ControllerParameterType.Trigger, new ParameterValue { boolValue = false }));
            parameters.Add(CreateParameter(22u, ControllerParameterType.Bool, new ParameterValue { boolValue = false }));
            parameters.Add(CreateParameter(33u, ControllerParameterType.Int, new ParameterValue { intValue = 10 }));
            parameters.Add(CreateParameter(44u, ControllerParameterType.Float, new ParameterValue { floatValue = 2f }));
            parameters.Add(CreateParameter(55u, ControllerParameterType.Trigger, new ParameterValue { boolValue = false }));

            var layers = this.Manager.AddBuffer<AnimatorControllerLayerComponent>(bound);
            layers.Add(new AnimatorControllerLayerComponent
            {
                weight = 0.5f,
                speed = 1f,
            });

            var clipBlob = CreateBlobAssetReference(new RukhankaAnimatorParameterClipBlob
            {
                TriggerHash = 11u,
                BoolHash = 22u,
                IntHash = 33u,
                FloatHash = 44u,
                TriggerMode = RukhankaAnimatorParameterTriggerMode.Set,
                IntMode = RukhankaAnimatorParameterValueMode.Increment,
                FloatMode = RukhankaAnimatorParameterValueMode.Random,
                UpdateTrigger = true,
                UpdateRandomTrigger = true,
                UpdateBool = true,
                UpdateRandomBool = false,
                SetLayerWeight = true,
                BoolValue = true,
                IntValue = 0,
                IntMin = 0,
                IntMax = 0,
                IntIncrement = 4,
                FloatValue = 0f,
                FloatMin = 6f,
                FloatMax = 6f,
                FloatIncrement = 0f,
                LayerIndex = 0,
                LayerWeight = 0.2f,
                Seed = 5u,
            });

            try
            {
                var clip = this.Manager.CreateEntity(typeof(RukhankaAnimatorParameterClipData), typeof(TrackBinding), typeof(ClipActive),
                    typeof(ClipActivePrevious));

                this.Manager.SetComponentData(clip, new RukhankaAnimatorParameterClipData { Value = clipBlob });
                this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
                this.Manager.SetComponentEnabled<ClipActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

                var randomHashes = this.Manager.AddBuffer<RukhankaAnimatorParameterRandomHash>(clip);
                randomHashes.Add(new RukhankaAnimatorParameterRandomHash { Hash = 55u });

                var system = this.World.CreateSystem<RukhankaAnimatorParameterTrackSystem>();
                this.RunSystem(system);

                parameters = this.Manager.GetBuffer<AnimatorControllerParameterComponent>(bound);
                Assert.IsTrue(GetParameter(parameters, 11u).value.boolValue);
                Assert.IsTrue(GetParameter(parameters, 22u).value.boolValue);
                Assert.AreEqual(14, GetParameter(parameters, 33u).value.intValue);
                Assert.AreEqual(6f, GetParameter(parameters, 44u).value.floatValue, 0.0001f);
                Assert.IsTrue(GetParameter(parameters, 55u).value.boolValue);

                layers = this.Manager.GetBuffer<AnimatorControllerLayerComponent>(bound);
                Assert.AreEqual(0.2f, layers[0].weight, 0.0001f);
            }
            finally
            {
                clipBlob.Dispose();
            }
        }

        [Test]
        public void SpeedTrack_ConstantRelative_WritesBlendAndTrackDeactivateRestoresInitialValues()
        {
            var bound = this.CreateBoundAnimatorEntity();
            var layers = this.Manager.AddBuffer<AnimatorControllerLayerComponent>(bound);
            layers.Add(new AnimatorControllerLayerComponent
            {
                speed = 2f,
                weight = 0.3f,
            });

            layers.Add(new AnimatorControllerLayerComponent
            {
                speed = 4f,
                weight = 0.7f,
            });

            var track = this.Manager.CreateEntity(typeof(TrackBinding), typeof(TimelineActive), typeof(TimelineActivePrevious));

            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<TimelineActive>(track, true);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, false);
            this.Manager.AddBuffer<RukhankaAnimatorSpeedInitial>(track);

            var clipBlob = CreateSpeedClipBlob(RukhankaAnimatorSpeedMode.Constant, minSpeed: 1.5f, maxSpeed: 1.5f, relative: true, seed: 0u, curve: null);

            try
            {
                var clip = this.CreateSpeedClip(track, bound, clipBlob, includeCurveUpdateComponents: false, localTimeSeconds: 0d);

                var system = this.World.CreateSystem<RukhankaAnimatorSpeedTrackSystem>();
                this.RunSystem(system);

                layers = this.Manager.GetBuffer<AnimatorControllerLayerComponent>(bound);
                Assert.AreEqual(3.5f, layers[0].speed, 0.0001f);
                Assert.AreEqual(3.5f, layers[1].speed, 0.0001f);

                this.Manager.AddComponent<TrackResetOnDeactivate>(track);
                this.Manager.SetComponentEnabled<TimelineActive>(track, false);
                this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, true);
                this.Manager.SetComponentEnabled<ClipActive>(clip, false);
                this.Manager.SetComponentEnabled<TimelineActive>(clip, false);

                this.RunSystem(system);

                layers = this.Manager.GetBuffer<AnimatorControllerLayerComponent>(bound);
                Assert.AreEqual(2f, layers[0].speed, 0.0001f);
                Assert.AreEqual(4f, layers[1].speed, 0.0001f);
            }
            finally
            {
                clipBlob.Dispose();
            }
        }

        [Test]
        public void SpeedTrack_CurveMode_EvaluatesCurveValue()
        {
            var bound = this.CreateBoundAnimatorEntity();
            var layers = this.Manager.AddBuffer<AnimatorControllerLayerComponent>(bound);
            layers.Add(new AnimatorControllerLayerComponent
            {
                speed = 2f,
                weight = 1f,
            });

            var track = this.Manager.CreateEntity(typeof(TrackBinding), typeof(TimelineActive), typeof(TimelineActivePrevious));

            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<TimelineActive>(track, true);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, false);
            this.Manager.AddBuffer<RukhankaAnimatorSpeedInitial>(track);

            var clipBlob = CreateSpeedClipBlob(RukhankaAnimatorSpeedMode.Curve, minSpeed: 2f, maxSpeed: 6f, relative: false, seed: 0u,
                curve: AnimationCurve.Linear(0f, 0f, 1f, 1f));

            try
            {
                this.CreateSpeedClip(track, bound, clipBlob, includeCurveUpdateComponents: true, localTimeSeconds: 0.25d);

                var system = this.World.CreateSystem<RukhankaAnimatorSpeedTrackSystem>();
                this.RunSystem(system);

                layers = this.Manager.GetBuffer<AnimatorControllerLayerComponent>(bound);
                Assert.AreEqual(3f, layers[0].speed, 0.0001f);
            }
            finally
            {
                clipBlob.Dispose();
            }
        }

        [Test]
        public void StateTrack_Lifecycle_CapturesMergedUsageAndRestoresLayerWeights()
        {
            // Gate the system's RequireForUpdate without activating clip work.
            this.Manager.CreateEntity(typeof(RukhankaAnimatorStateClipData));

            var bound = this.CreateBoundAnimatorEntity();
            var layers = this.Manager.AddBuffer<AnimatorControllerLayerComponent>(bound);
            layers.Add(new AnimatorControllerLayerComponent
            {
                weight = 0.3f,
                speed = 1f,
            });

            layers.Add(new AnimatorControllerLayerComponent
            {
                weight = 0.7f,
                speed = 1f,
            });

            var track = this.Manager.CreateEntity(typeof(TrackBinding), typeof(TimelineActive), typeof(TimelineActivePrevious));

            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<TimelineActive>(track, true);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, false);

            var layerUsages = this.Manager.AddBuffer<RukhankaAnimatorStateLayerUsage>(track);
            layerUsages.Add(new RukhankaAnimatorStateLayerUsage
            {
                LayerIndex = 0,
                RestoreState = true,
                RestoreWeight = false,
            });

            layerUsages.Add(new RukhankaAnimatorStateLayerUsage
            {
                LayerIndex = 0,
                RestoreState = false,
                RestoreWeight = true,
            });

            layerUsages.Add(new RukhankaAnimatorStateLayerUsage
            {
                LayerIndex = 1,
                RestoreState = false,
                RestoreWeight = true,
            });

            this.Manager.AddBuffer<RukhankaAnimatorStateLayerInitial>(track);

            var system = this.World.CreateSystem<RukhankaAnimatorStateTrackSystem>();
            this.RunSystem(system);

            var initials = this.Manager.GetBuffer<RukhankaAnimatorStateLayerInitial>(track);
            Assert.AreEqual(2, initials.Length);
            Assert.IsTrue(initials[0].RestoreState);
            Assert.IsTrue(initials[0].RestoreWeight);
            Assert.AreEqual(0.3f, initials[0].Weight, 0.0001f);
            Assert.IsFalse(initials[1].RestoreState);
            Assert.IsTrue(initials[1].RestoreWeight);
            Assert.AreEqual(0.7f, initials[1].Weight, 0.0001f);

            layers = this.Manager.GetBuffer<AnimatorControllerLayerComponent>(bound);
            var layer0 = layers[0];
            layer0.weight = 0.11f;
            layers[0] = layer0;
            var layer1 = layers[1];
            layer1.weight = 0.22f;
            layers[1] = layer1;

            this.Manager.AddComponent<TrackResetOnDeactivate>(track);
            this.Manager.SetComponentEnabled<TimelineActive>(track, false);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, true);

            this.RunSystem(system);

            layers = this.Manager.GetBuffer<AnimatorControllerLayerComponent>(bound);
            Assert.AreEqual(0.3f, layers[0].weight, 0.0001f);
            Assert.AreEqual(0.7f, layers[1].weight, 0.0001f);
        }

        [Test]
        public void StateTrack_PlayStateClip_WithZeroHash_DoesNotModifyBoundLayer()
        {
            var bound = this.CreateBoundAnimatorEntity();
            var layers = this.Manager.AddBuffer<AnimatorControllerLayerComponent>(bound);
            layers.Add(new AnimatorControllerLayerComponent
            {
                weight = 0.6f,
                speed = 1f,
            });

            var clip = this.Manager.CreateEntity(typeof(RukhankaAnimatorStateClipData), typeof(TrackBinding), typeof(ClipActive), typeof(ClipActivePrevious));

            this.Manager.SetComponentData(clip, new RukhankaAnimatorStateClipData
            {
                Type = RukhankaAnimatorStateClipType.PlayState,
                PlayState = new RukhankaAnimatorStatePlayStateData
                {
                    StateHash = 0u,
                    LayerIndex = 0,
                    SetLayerWeight = true,
                    WeightLayerIndex = 0,
                    LayerWeight = 0.1f,
                    Mode = RukhankaAnimatorPlayStateMode.NormalizedTime,
                    NormalizedTime = 0.5f,
                    FixedTimeSeconds = 0f,
                },
            });

            this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

            var system = this.World.CreateSystem<RukhankaAnimatorStateTrackSystem>();
            this.RunSystem(system);

            layers = this.Manager.GetBuffer<AnimatorControllerLayerComponent>(bound);
            Assert.AreEqual(0.6f, layers[0].weight, 0.0001f);
        }

        private static AnimatorControllerParameterComponent CreateParameter(uint hash, ControllerParameterType type, in ParameterValue value)
        {
            return new AnimatorControllerParameterComponent
            {
                hash = hash,
                type = type,
                value = value,
            };
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

        private static BlobAssetReference<ControllerBlob> CreateControllerBlob(int layerCount, int stateCountPerLayer)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ControllerBlob>();
            var layers = builder.Allocate(ref root.layers, layerCount);
            builder.Allocate(ref root.parameters, 0);

            for (var i = 0; i < layerCount; i++)
            {
                var states = builder.Allocate(ref layers[i].states, stateCountPerLayer);
                layers[i].defaultStateIndex = 0;
                layers[i].initialWeight = 1f;
                layers[i].syncedLayerIndex = -1;
                layers[i].syncedTiming = -1;

                for (var s = 0; s < stateCountPerLayer; s++)
                {
                    states[s].hash = 0;
                    states[s].speed = 1f;
                    states[s].speedMultiplierParameterIndex = -1;
                }
            }

            var blob = builder.CreateBlobAssetReference<ControllerBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static BlobAssetReference<ControllerAnimationsBlob> CreateControllerAnimationsBlob(int animationCount)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ControllerAnimationsBlob>();
            builder.Allocate(ref root.animations, animationCount);
            var blob = builder.CreateBlobAssetReference<ControllerAnimationsBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static AnimatorControllerParameterComponent GetParameter(DynamicBuffer<AnimatorControllerParameterComponent> parameters, uint hash)
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].hash == hash)
                {
                    return parameters[i];
                }
            }

            Assert.Fail($"Parameter with hash {hash} was not found.");
            return default;
        }

        private static BlobAssetReference<RukhankaAnimatorSpeedClipBlob> CreateSpeedClipBlob(
            RukhankaAnimatorSpeedMode mode, float minSpeed, float maxSpeed, bool relative, uint seed, AnimationCurve curve)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<RukhankaAnimatorSpeedClipBlob>();
            root.Mode = mode;
            root.MinSpeed = minSpeed;
            root.MaxSpeed = maxSpeed;
            root.Relative = relative;
            root.Seed = seed;

            if (mode == RukhankaAnimatorSpeedMode.Curve && curve != null)
            {
                BlobCurve.Construct(ref builder, ref root.Curve, curve);
            }

            var blob = builder.CreateBlobAssetReference<RukhankaAnimatorSpeedClipBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private Entity CreateBoundAnimatorEntity()
        {
            return this.Manager.CreateEntity();
        }

        private Entity CreateSpeedClip(
            Entity track, Entity bound, BlobAssetReference<RukhankaAnimatorSpeedClipBlob> clipBlob, bool includeCurveUpdateComponents, double localTimeSeconds)
        {
            var clip = includeCurveUpdateComponents
                ? this.Manager.CreateEntity(typeof(RukhankaAnimatorSpeedAnimated), typeof(RukhankaAnimatorSpeedClipData), typeof(Clip), typeof(TrackBinding),
                    typeof(TimelineActive), typeof(ClipActive), typeof(ClipActivePrevious), typeof(LocalTime), typeof(ClipBlobCurveCache))
                : this.Manager.CreateEntity(typeof(RukhankaAnimatorSpeedAnimated), typeof(RukhankaAnimatorSpeedClipData), typeof(Clip), typeof(TrackBinding),
                    typeof(TimelineActive), typeof(ClipActive), typeof(ClipActivePrevious));

            this.Manager.SetComponentData(clip, new RukhankaAnimatorSpeedAnimated { Value = 0f });
            this.Manager.SetComponentData(clip, new RukhankaAnimatorSpeedClipData { Value = clipBlob });
            this.Manager.SetComponentData(clip, new Clip { Track = track });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

            if (includeCurveUpdateComponents)
            {
                this.Manager.SetComponentData(clip, new LocalTime { Value = new DiscreteTime(localTimeSeconds) });
                this.Manager.SetComponentData(clip, ClipBlobCurveCache.Create());
            }

            return clip;
        }
    }
}

#endif
