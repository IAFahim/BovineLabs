namespace BovineLabs.Vibe.Data.NonUniformScale
{
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    /// <summary>
    /// Identifies the non-uniform scale animation mode.
    /// </summary>
    public enum NonUniformScaleType : byte
    {
        Initial,
        SquashStretchAbsolute,
        SquashStretchCurve,
        SquashStretchShake,
        SquashStretchSpring,
    }

    /// <summary>
    /// Axis selection for squash and stretch deformation.
    /// </summary>
    public enum SquashStretchNonUniformScaleAxis : byte
    {
        X,
        Y,
        Z,
    }

    /// <summary>
    /// Component holding the blob asset for a non-uniform scale clip.
    /// </summary>
    public struct NonUniformScaleClipData : IComponentData
    {
        /// <summary>
        /// Blob containing the serialized clip definition.
        /// </summary>
        public BlobAssetReference<NonUniformScaleClipBlob> Value;
    }

    /// <summary>
    /// Union storing the data required to evaluate a non-uniform scale clip.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct NonUniformScaleClipBlob
    {
        [FieldOffset(0)]
        public NonUniformScaleType Type;

        [FieldOffset(4)]
        public SquashStretchData SquashStretch;

        [FieldOffset(12)]
        public SquashStretchAbsoluteData SquashStretchAbsolute;

        [FieldOffset(12)]
        public SquashStretchCurveData SquashStretchCurve;

        [FieldOffset(12)]
        public SquashStretchShakeData SquashStretchShake;

        [FieldOffset(12)]
        public SquashStretchSpringData SquashStretchSpring;

        /// <summary>
        /// Shared configuration used by all squash and stretch clip variants.
        /// </summary>
        public struct SquashStretchData
        {
            public SquashStretchNonUniformScaleAxis Axis;
            public bool PreserveVolume;
            public bool TransformOnClipActivation;
            public float VolumeExponent;
        }

        /// <summary>
        /// Constant squash or stretch amount.
        /// </summary>
        public struct SquashStretchAbsoluteData
        {
            public float Amount;
        }

        /// <summary>
        /// Curve-driven squash or stretch profile.
        /// </summary>
        public struct SquashStretchCurveData
        {
            public BlobCurve Curve;
        }

        /// <summary>
        /// Shake parameters applied to the deformation amount.
        /// </summary>
        public struct SquashStretchShakeData
        {
            public float Amplitude;
            public float Frequency;
            public float Damping;
            public uint Seed;
            public BlobCurve AttenuationCurve;
        }

        /// <summary>
        /// Spring configuration used to animate squash or stretch towards a rest value.
        /// </summary>
        public struct SquashStretchSpringData
        {
            public float RestValue;
            public bool RestIsMultiplier;
            public float InitialVelocity;
            public float Frequency;
            public float Damping;
        }
    }
}
