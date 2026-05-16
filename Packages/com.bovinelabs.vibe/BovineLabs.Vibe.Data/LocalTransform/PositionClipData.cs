// <copyright file="PositionClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.LocalTransform
{
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;
#if BL_REACTION
    using BovineLabs.Reaction.Data.Core;
#endif
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Describes the playback mode used when animating a position clip.
    /// </summary>
    public enum PositionType : byte
    {
        World = 0,
        Offset = 1,
#if BL_REACTION
        Target = 2,
#endif
        Curve = 3,
        Shake = 4,
        Spring = 5,
        Orbit = 6,
        Wiggle = 7,
        Initial = 8,
    }

    /// <summary>
    /// Component containing the blob asset that defines a position clip.
    /// </summary>
    public struct PositionClipData : IComponentData
    {
        /// <summary>
        /// Blob holding the serialized configuration for the clip.
        /// </summary>
        public BlobAssetReference<PositionClipBlob> Value;
    }

    /// <summary>
    /// Union-style blob that stores the data required to evaluate a position clip.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PositionClipBlob
    {
        [FieldOffset(0)]
        public PositionType Type;

        [FieldOffset(1)]
        public bool TransformOnClipActivation;

        [FieldOffset(4)]
        public WorldData World;

        [FieldOffset(4)]
        public OffsetData Offset;

#if BL_REACTION
        [FieldOffset(4)]
        public TargetData Target;
#endif

        [FieldOffset(4)]
        public CurveData Curve;

        [FieldOffset(4)]
        public ShakeData Shake;

        [FieldOffset(4)]
        public SpringData Spring;

        [FieldOffset(4)]
        public OrbitData Orbit;

        [FieldOffset(4)]
        public WiggleData Wiggle;

        /// <summary>
        /// Absolute world-space transform to apply for the clip duration.
        /// </summary>
        public struct WorldData
        {
            public float3 Position;
        }

        /// <summary>
        /// Offset sampled in a chosen transform space.
        /// </summary>
        public struct OffsetData
        {
            public TransformSpace Space;
            public float3 Value;
        }

#if BL_REACTION
        /// <summary>
        /// Configuration for following a target transform.
        /// </summary>
        public struct TargetData
        {
            public Target Target;
            public bool FixedPosition;
            public TransformSpace Space;
            public float3 Offset;
        }
#endif

        /// <summary>
        /// Independent curves evaluated per-axis when animating the position.
        /// </summary>
        public struct CurveData
        {
            public BlobCurve CurveX;
            public BlobCurve CurveY;
            public BlobCurve CurveZ;
        }

        /// <summary>
        /// Parameters for Perlin-style shake behaviour.
        /// </summary>
        public struct ShakeData
        {
            public TransformSpace Space;
            public float3 Amplitude;
            public float Frequency;
            public float Damping;
            public uint Seed;
            public float3 PerAxisFrequencyMultiplier;
            public BlobCurve AttenuationCurve;
            public BlobCurve AmplitudeCurve;
            public BlobCurve FrequencyCurve;
        }

        /// <summary>
        /// Describes a damped spring towards a rest point.
        /// </summary>
        public struct SpringData
        {
            public PositionSpringMode Mode;
            public TransformSpace Space;
#if BL_REACTION
            public Target Target;
#endif
            public float3 RestPoint;
            public float3 InitialVelocity;
            public float3 Frequency;
            public float3 Damping;

            /// <summary>
            /// Describes how a position spring should interpret its rest point.
            /// </summary>
            public enum PositionSpringMode : byte
            {
                Bump = 0,
                MoveTo = 1,
                MoveToAdditive = 2,
            }
        }

        /// <summary>
        /// Settings for orbiting a target around an axis.
        /// </summary>
        public struct OrbitData
        {
#if BL_REACTION
            public Target Target;
#endif
            public TransformSpace PivotSpace;
            public TransformSpace AxisSpace;
            public TransformSpace InitialOffsetSpace;
            public bool UseCustomInitialOffset;
            public float Radius;
            public float AngularSpeed;
            public float3 PivotOffset;
            public float3 Axis;
            public float3 InitialOffset;
            public BlobCurve AngleCurve;
            public BlobCurve RadiusCurve;
        }

        /// <summary>
        /// Settings for procedural wiggle noise.
        /// </summary>
        public struct WiggleData
        {
            public TransformSpace Space;
            public float3 Amplitude;
            public float Frequency;
            public float Smoothness;
            public uint Seed;
            public float3 PerAxisFrequencyMultiplier;
            public BlobCurve AmplitudeCurve;
            public BlobCurve FrequencyCurve;
        }
    }
}
