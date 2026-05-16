// <copyright file="AudioSourceDataClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    /// <summary>
    /// Differentiates audio source data clip behaviour.
    /// </summary>
    public enum AudioSourceDataClipType : byte
    {
        Animated,
        Initial,
        VolumeSweep,
        PitchSweep,
    }

    /// <summary>
    /// Serialized configuration baked for audio source data clips.
    /// </summary>
    public struct AudioSourceDataClipData : IComponentData
    {
        /// <summary>
        /// Blob holding the serialized configuration for the clip.
        /// </summary>
        public BlobAssetReference<AudioSourceDataClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AudioSourceDataClipBlob
    {
        [FieldOffset(0)]
        public AudioSourceDataClipType Type;

        [FieldOffset(4)]
        public AnimatedData Data;

        [FieldOffset(4)]
        public AudioCurveSweepData Sweep;
    }

    public struct AnimatedData
    {
        public float Volume;
        public float Pitch;
    }

    /// <summary>
    /// Curve-driven sweep configuration with optional remap and relative mode.
    /// </summary>
    public struct AudioCurveSweepData
    {
        public BlobCurve Curve;
        public float Min;
        public float Max;
        public bool Relative;
    }
}
#endif
