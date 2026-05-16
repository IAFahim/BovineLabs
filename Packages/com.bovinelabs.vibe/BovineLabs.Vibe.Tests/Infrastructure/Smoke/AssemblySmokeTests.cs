// <copyright file="AssemblySmokeTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.Infrastructure.Smoke
{
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;

    public class AssemblySmokeTests
    {
        [Test]
        public void AssemblyIsDiscoveredByEditModeRunner()
        {
            Assert.AreEqual("BovineLabs.Vibe.Tests", typeof(AssemblySmokeTests).Assembly.GetName().Name);
        }
    }

    public class FixtureSmokeTests : VibeEcsTestsFixture
    {
        [Test]
        public void FixtureCreatesWorldAndEntityManager()
        {
            Assert.IsTrue(this.World.IsCreated);

            var entity = this.Manager.CreateEntity();
            Assert.IsTrue(this.Manager.Exists(entity));
        }
    }
}
