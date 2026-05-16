// <copyright file="VibeEcsTestsFixture.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.Fixtures
{
    using BovineLabs.Testing;
    using Unity.Entities;

    /// <summary>
    /// Common ECS fixture for vibe runtime tests.
    /// </summary>
    public abstract class VibeEcsTestsFixture : ECSTestsFixture
    {
        /// <summary>
        /// Runs a system and completes tracked jobs to make assertions deterministic.
        /// </summary>
        /// <param name="system">The system handle to update.</param>
        protected void RunSystem(SystemHandle system)
        {
            system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
        }
    }
}
