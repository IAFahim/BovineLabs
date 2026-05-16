// <copyright file="RotationClipData.cs" company="BovineLabs">
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
    /// Identifies the algorithm used to evaluate a rotation clip.
    /// </summary>
    public enum RotationType : byte
    {
#if BL_REACTION
        LookAtTarget = 0,
#endif
        LookAtStart = 1,
        LookInDirection = 2,
        LookAtRotation = 3,
        Shake = 4,
        Spring = 5,
        Wiggle = 6,
        Initial = 7,
    }

    /// <summary>
    /// Component containing the blob asset for a rotation clip configuration.
    /// </summary>
    public struct RotationClipData : IComponentData
    {
        /// <summary>
        /// Blob containing the serialized clip definition.
        /// </summary>
        public BlobAssetReference<RotationClipBlob> Value;
    }

    /// <summary>
    /// Union storing rotation clip parameters.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RotationClipBlob
    {
        [FieldOffset(0)]
        public RotationType Type;

        [FieldOffset(1)]
        public bool TransformOnClipActivation;

#if BL_REACTION
        [FieldOffset(4)]
        public LookAtTargetData LookAtTarget;
#endif

        [FieldOffset(4)]
        public LookInDirectionData LookInDirection;

        [FieldOffset(4)]
        public LookAtRotationData LookAtRotation;

        [FieldOffset(4)]
        public ShakeData Shake;

        [FieldOffset(4)]
        public SpringData Spring;

        [FieldOffset(4)]
        public WiggleData Wiggle;

#if BL_REACTION
        /// <summary>
        /// Data used to rotate towards a target entity.
        /// </summary>
        public struct LookAtTargetData
        {
            public Target Target;
            public bool FixedRotation;
            public TransformSpace Space;
            public quaternion Offset;
            public TransformSpace AnchorSpace;
            public float3 AnchorPosition;
        }
#endif

        /// <summary>
        /// Parameters describing a fixed direction look-at.
        /// </summary>
        public struct LookInDirectionData
        {
            public TransformSpace Space;
            public float3 Direction;
        }

        /// <summary>
        /// Static rotation applied when the clip runs.
        /// </summary>
        public struct LookAtRotationData
        {
            public TransformSpace Space;
            public quaternion Rotation;
        }

        /// <summary>
        /// Shake configuration applied in Euler angles.
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
        /// Spring configuration that eases towards a rest rotation.
        /// </summary>
        public struct SpringData
        {
            public float3 RestEuler;
            public float3 InitialVelocity;
            public float3 Frequency;
            public float3 Damping;
        }

        /// <summary>
        /// Wiggle settings for rotational noise.
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
