// <copyright file="TrackLifeImplTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.Runtime.Jobs
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Jobs;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Unity.Entities;

    public class TrackLifeImplTests : VibeEcsTestsFixture
    {
        [Test]
        public void OnUpdate_ActivateCopiesBoundTargetIntoInitial()
        {
            var target = this.CreateTarget(17);
            var track = this.CreateTrack(
                target,
                initialValue: 2,
                timelineActiveEnabled: true,
                timelineActivePreviousEnabled: false,
                resetOnDeactivate: false);

            this.RunTrackLifeSystem();

            var initial = this.Manager.GetComponentData<TestTrackInitial>(track);
            Assert.AreEqual(17, initial.Value.Value);
        }

        [Test]
        public void OnUpdate_ActivateMissingTargetIsNoOp()
        {
            var missingTarget = this.Manager.CreateEntity();
            var track = this.CreateTrack(
                missingTarget,
                initialValue: 9,
                timelineActiveEnabled: true,
                timelineActivePreviousEnabled: false,
                resetOnDeactivate: false);

            this.RunTrackLifeSystem();

            var initial = this.Manager.GetComponentData<TestTrackInitial>(track);
            Assert.AreEqual(9, initial.Value.Value);
        }

        [Test]
        public void OnUpdate_DeactivateRestoresInitialToBoundTarget()
        {
            var target = this.CreateTarget(33);
            this.CreateTrack(
                target,
                initialValue: 5,
                timelineActiveEnabled: false,
                timelineActivePreviousEnabled: true,
                resetOnDeactivate: true);

            this.RunTrackLifeSystem();

            var value = this.Manager.GetComponentData<TestTrackValue>(target);
            Assert.AreEqual(5, value.Value);
        }

        [Test]
        public void OnUpdate_DeactivateMissingTargetIsNoOp()
        {
            var missingTarget = this.Manager.CreateEntity();
            this.CreateTrack(
                missingTarget,
                initialValue: 25,
                timelineActiveEnabled: false,
                timelineActivePreviousEnabled: true,
                resetOnDeactivate: true);

            this.RunTrackLifeSystem();

            Assert.IsFalse(this.Manager.HasComponent<TestTrackValue>(missingTarget));
        }

        [Test]
        public void OnUpdate_QueryGatesRespectTimelineActiveAndPreviousFlags()
        {
            var activateTarget = this.CreateTarget(21);
            var deactivateTarget = this.CreateTarget(55);
            var unchangedTarget = this.CreateTarget(99);

            var activateTrack = this.CreateTrack(
                activateTarget,
                initialValue: 1,
                timelineActiveEnabled: true,
                timelineActivePreviousEnabled: false,
                resetOnDeactivate: false);

            var deactivateTrack = this.CreateTrack(
                deactivateTarget,
                initialValue: 7,
                timelineActiveEnabled: false,
                timelineActivePreviousEnabled: true,
                resetOnDeactivate: true);

            var activeAndPreviousTrack = this.CreateTrack(
                unchangedTarget,
                initialValue: 11,
                timelineActiveEnabled: true,
                timelineActivePreviousEnabled: true,
                resetOnDeactivate: true);

            var inactiveAndNotPreviousTrack = this.CreateTrack(
                unchangedTarget,
                initialValue: 13,
                timelineActiveEnabled: false,
                timelineActivePreviousEnabled: false,
                resetOnDeactivate: true);

            this.RunTrackLifeSystem();

            var activateInitial = this.Manager.GetComponentData<TestTrackInitial>(activateTrack);
            Assert.AreEqual(21, activateInitial.Value.Value);

            var deactivateValue = this.Manager.GetComponentData<TestTrackValue>(deactivateTarget);
            Assert.AreEqual(7, deactivateValue.Value);

            var activeAndPreviousInitial = this.Manager.GetComponentData<TestTrackInitial>(activeAndPreviousTrack);
            Assert.AreEqual(11, activeAndPreviousInitial.Value.Value);

            var inactiveAndNotPreviousInitial = this.Manager.GetComponentData<TestTrackInitial>(inactiveAndNotPreviousTrack);
            Assert.AreEqual(13, inactiveAndNotPreviousInitial.Value.Value);

            var unchangedValue = this.Manager.GetComponentData<TestTrackValue>(unchangedTarget);
            Assert.AreEqual(99, unchangedValue.Value);
        }

        private static void ConfigureTimelineState(EntityManager manager, Entity track, bool timelineActiveEnabled, bool timelineActivePreviousEnabled)
        {
            manager.SetComponentEnabled<TimelineActive>(track, timelineActiveEnabled);
            manager.SetComponentEnabled<TimelineActivePrevious>(track, timelineActivePreviousEnabled);
        }

        private Entity CreateTarget(int value)
        {
            var entity = this.Manager.CreateEntity(typeof(TestTrackValue));
            this.Manager.SetComponentData(entity, new TestTrackValue { Value = value });
            return entity;
        }

        private Entity CreateTrack(
            Entity target,
            int initialValue,
            bool timelineActiveEnabled,
            bool timelineActivePreviousEnabled,
            bool resetOnDeactivate)
        {
            var entity = this.Manager.CreateEntity(
                typeof(TestTrackInitial),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(TimelineActivePrevious));

            if (resetOnDeactivate)
            {
                this.Manager.AddComponent<TrackResetOnDeactivate>(entity);
            }

            this.Manager.SetComponentData(entity, new TrackBinding { Value = target });
            this.Manager.SetComponentData(entity, new TestTrackInitial { Value = new TestTrackValue { Value = initialValue } });
            ConfigureTimelineState(this.Manager, entity, timelineActiveEnabled, timelineActivePreviousEnabled);

            return entity;
        }

        private void RunTrackLifeSystem()
        {
            var system = this.World.CreateSystem<TestTrackLifeSystem>();
            this.RunSystem(system);
        }

        private struct TestTrackValue : IComponentData
        {
            public int Value;
        }

        private struct TestTrackInitial : IInitial<TestTrackValue>
        {
            public TestTrackValue Value { get; set; }
        }

        private partial struct TestTrackLifeSystem : ISystem
        {
            private TrackLifeImpl<TestTrackValue, TestTrackInitial> lifeImpl;

            public void OnCreate(ref SystemState state)
            {
                this.lifeImpl.OnCreate(ref state);
            }

            public void OnUpdate(ref SystemState state)
            {
                this.lifeImpl.OnUpdate(ref state);
            }
        }
    }
}
