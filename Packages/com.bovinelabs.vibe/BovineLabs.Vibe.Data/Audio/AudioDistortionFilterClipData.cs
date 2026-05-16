// <copyright file="AudioDistortionFilterClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using System.Runtime.InteropServices;
    using Unity.Entities;

    /// <summary>
    /// Differentiates audio distortion filter clip behaviour.
    /// </summary>
    public enum AudioDistortionFilterClipType : byte
    {
        Animated,
        Sweep,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for audio distortion filter clips.
    /// </summary>
    public struct AudioDistortionFilterClipData : IComponentData
    {
        public BlobAssetReference<AudioDistortionFilterClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AudioDistortionFilterClipBlob
    {
        [FieldOffset(0)]
        public AudioDistortionFilterClipType Type;

        [FieldOffset(4)]
        public AudioDistortionFilterConstantData Data;

        [FieldOffset(4)]
        public AudioCurveSweepData Sweep;
    }

    public struct AudioDistortionFilterConstantData
    {
        public float DistortionLevel;
    }
}
#endif
