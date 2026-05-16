// <copyright file="TargetResolverTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_REACTION
namespace BovineLabs.Vibe.Tests.Runtime.Utility
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Unity.Mathematics;
    using Unity.Transforms;

    public class TargetResolverTests : VibeEcsTestsFixture
    {
        [Test]
        public void TryResolveLocalTransform_TargetNone_ReturnsFalse()
        {
            var director = new DirectorRoot { Director = this.Manager.CreateEntity() };
            var targetsLookup = this.Manager.GetComponentLookup<Targets>(true);
            var targetsCustomLookup = this.Manager.GetComponentLookup<TargetsCustom>(true);
            var localTransforms = this.Manager.GetComponentLookup<LocalTransform>(true);

            var resolved = TargetResolver.TryResolveLocalTransform(
                in director,
                Target.None,
                ref targetsLookup,
                ref targetsCustomLookup,
                ref localTransforms,
                out _);

            Assert.IsFalse(resolved);
        }

        [Test]
        public void TryResolveLocalTransform_MissingTargetsOnDirector_ReturnsFalse()
        {
            var director = new DirectorRoot { Director = this.Manager.CreateEntity() };
            var targetsLookup = this.Manager.GetComponentLookup<Targets>(true);
            var targetsCustomLookup = this.Manager.GetComponentLookup<TargetsCustom>(true);
            var localTransforms = this.Manager.GetComponentLookup<LocalTransform>(true);

            var resolved = TargetResolver.TryResolveLocalTransform(
                in director,
                Target.Target,
                ref targetsLookup,
                ref targetsCustomLookup,
                ref localTransforms,
                out _);

            Assert.IsFalse(resolved);
        }

        [Test]
        public void TryResolveLocalTransform_MissingLocalTransformOnTarget_ReturnsFalse()
        {
            var directorEntity = this.Manager.CreateEntity();
            var targetEntity = this.Manager.CreateEntity();
            this.Manager.AddComponentData(directorEntity, new Targets
            {
                Owner = directorEntity,
                Source = directorEntity,
                Target = targetEntity,
            });

            var director = new DirectorRoot { Director = directorEntity };
            var targetsLookup = this.Manager.GetComponentLookup<Targets>(true);
            var targetsCustomLookup = this.Manager.GetComponentLookup<TargetsCustom>(true);
            var localTransforms = this.Manager.GetComponentLookup<LocalTransform>(true);

            var resolved = TargetResolver.TryResolveLocalTransform(
                in director,
                Target.Target,
                ref targetsLookup,
                ref targetsCustomLookup,
                ref localTransforms,
                out _);

            Assert.IsFalse(resolved);
        }

        [Test]
        public void TryResolveLocalTransform_ValidTargetAndTransform_ReturnsTrue()
        {
            var directorEntity = this.Manager.CreateEntity();
            var targetEntity = this.Manager.CreateEntity();
            var expected = LocalTransform.FromPositionRotationScale(new float3(1f, 2f, 3f), quaternion.identity, 2f);

            this.Manager.AddComponentData(directorEntity, new Targets
            {
                Owner = directorEntity,
                Source = directorEntity,
                Target = targetEntity,
            });
            this.Manager.AddComponentData(targetEntity, expected);

            var director = new DirectorRoot { Director = directorEntity };
            var targetsLookup = this.Manager.GetComponentLookup<Targets>(true);
            var targetsCustomLookup = this.Manager.GetComponentLookup<TargetsCustom>(true);
            var localTransforms = this.Manager.GetComponentLookup<LocalTransform>(true);

            var resolved = TargetResolver.TryResolveLocalTransform(
                in director,
                Target.Target,
                ref targetsLookup,
                ref targetsCustomLookup,
                ref localTransforms,
                out var targetTransform);

            Assert.IsTrue(resolved);
            Assert.AreEqual(expected.Position, targetTransform.Position);
            Assert.AreEqual(expected.Scale, targetTransform.Scale);
            Assert.AreEqual(expected.Rotation.value, targetTransform.Rotation.value);
        }
    }
}
#endif
