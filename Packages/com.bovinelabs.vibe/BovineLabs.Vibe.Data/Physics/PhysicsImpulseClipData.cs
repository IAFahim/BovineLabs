// <copyright file="PhysicsImpulseClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS

namespace BovineLabs.Vibe.Data.Physics
{
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Identifies how a physics impulse should be generated.
    /// </summary>
    public enum PhysicsImpulseClipType : byte
    {
        World,
        LocalAxis,
        WorldTorque,
        LocalTorque,
        ImpulseAtPoint,
        Radial,
    }

    /// <summary>
    /// Axis selection used by local-axis impulses.
    /// </summary>
    public enum PhysicsImpulseAxis : byte
    {
        Forward,
        Up,
        Right,
    }

    /// <summary>
    /// Blob-backed configuration for a Physics impulse clip.
    /// </summary>
    public struct PhysicsImpulseClipData : IComponentData
    {
        /// <summary>
        /// Blob containing the impulse settings.
        /// </summary>
        public BlobAssetReference<PhysicsImpulseClipBlob> Value;
    }

    /// <summary>
    /// Union storing the data required to apply a physics impulse.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PhysicsImpulseClipBlob
    {
        [FieldOffset(0)]
        public PhysicsImpulseClipType Type;

        [FieldOffset(4)]
        public WorldImpulseData World;

        [FieldOffset(4)]
        public LocalAxisImpulseData LocalAxis;

        [FieldOffset(4)]
        public WorldTorqueImpulseData WorldTorque;

        [FieldOffset(4)]
        public LocalTorqueImpulseData LocalTorque;

        [FieldOffset(4)]
        public PointImpulseData Point;

        [FieldOffset(4)]
        public RadialImpulseData Radial;

        /// <summary>
        /// World-space linear impulse (Newton seconds).
        /// </summary>
        public struct WorldImpulseData
        {
            public float3 Impulse;
        }

        /// <summary>
        /// Local-axis impulse described by magnitude and axis selection.
        /// </summary>
        public struct LocalAxisImpulseData
        {
            public PhysicsImpulseAxis Axis;
            public float Magnitude;
        }

        /// <summary>
        /// World-space angular impulse (Newton meters seconds).
        /// </summary>
        public struct WorldTorqueImpulseData
        {
            public float3 Torque;
        }

        /// <summary>
        /// Local-space angular impulse (Newton meters seconds).
        /// </summary>
        public struct LocalTorqueImpulseData
        {
            public float3 Torque;
        }

        /// <summary>
        /// Linear impulse applied at a world-space point.
        /// </summary>
        public struct PointImpulseData
        {
            public float3 Impulse;
            public float3 Point;
        }

        /// <summary>
        /// Radial impulse with distance-based falloff.
        /// </summary>
        public struct RadialImpulseData
        {
            public float3 Origin;
            public float Radius;
            public float Strength;
            public float3 UpAxis;
            public BlobCurve FalloffCurve;
        }
    }
}

#endif
