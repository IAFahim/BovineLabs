// <copyright file="ScaleClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.LocalTransform
{
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    /// <summary>
    /// Identifies how a scale clip should animate the bound transform.
    /// </summary>
    public enum ScaleType : byte
    {
        Absolute,
        Offset,
        Curve,
        Shake,
        Spring,
        Wiggle,
        Initial,
    }

    /// <summary>
    /// Component holding the blob asset for a scale clip configuration.
    /// </summary>
    public struct ScaleClipData : IComponentData
    {
        /// <summary>
        /// Blob containing the serialized clip definition.
        /// </summary>
        public BlobAssetReference<ScaleClipBlob> Value;
    }

    /// <summary>
    /// Union storing the data required to evaluate a scale clip.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ScaleClipBlob
    {
        [FieldOffset(0)]
        public ScaleType Type;

        [FieldOffset(4)]
        public AbsoluteData Absolute;

        [FieldOffset(4)]
        public OffsetData Offset;

        [FieldOffset(4)]
        public CurveData Curve;

        [FieldOffset(4)]
        public ShakeData Shake;

        [FieldOffset(4)]
        public SpringData Spring;

        [FieldOffset(4)]
        public WiggleData Wiggle;

        /// <summary>
        /// Absolute scale to apply for the clip duration.
        /// </summary>
        public struct AbsoluteData
        {
            public float Value;
        }

        /// <summary>
        /// Offset or multiplier applied relative to the incoming scale.
        /// </summary>
        public struct OffsetData
        {
            public float Value;
            public bool IsMultiplier;
        }

        /// <summary>
        /// Parameters for curve-driven scale animation.
        /// </summary>
        public struct CurveData
        {
            public bool TransformOnClipActivation;
            public BlobCurve Curve;
        }

        /// <summary>
        /// Shake settings producing high-frequency noise.
        /// </summary>
        public struct ShakeData
        {
            public bool TransformOnClipActivation;
            public float Amplitude;
            public float Frequency;
            public float Damping;
            public uint Seed;
            public float PerAxisFrequencyMultiplier;
            public BlobCurve AttenuationCurve;
            public BlobCurve AmplitudeCurve;
            public BlobCurve FrequencyCurve;
        }

        /// <summary>
        /// Settings for a scalar spring simulation.
        /// </summary>
        public struct SpringData
        {
            public bool TransformOnClipActivation;
            public float RestScale;
            public bool RestIsMultiplier;
            public float InitialVelocity;
            public float Frequency;
            public float Damping;
        }

        /// <summary>
        /// Configuration for procedural wiggle noise.
        /// </summary>
        public struct WiggleData
        {
            public bool TransformOnClipActivation;
            public float Amplitude;
            public float Frequency;
            public float Smoothness;
            public uint Seed;
            public float PerAxisFrequencyMultiplier;
            public BlobCurve AmplitudeCurve;
            public BlobCurve FrequencyCurve;
        }
    }
}
