// <copyright file="PhysicsImpulseInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS

namespace BovineLabs.Vibe.Data.Physics
{
    using BovineLabs.Vibe.Data;
    using Unity.Physics;

    /// <summary>
    /// Captures the physics velocity present when a track activates.
    /// </summary>
    public struct PhysicsImpulseInitial : IInitial<PhysicsVelocity>
    {
        public PhysicsVelocity Value { get; set; }
    }
}

#endif
