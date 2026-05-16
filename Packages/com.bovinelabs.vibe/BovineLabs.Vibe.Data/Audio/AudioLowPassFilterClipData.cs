// <copyright file="AudioLowPassFilterClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using System.Runtime.InteropServices;
    using Unity.Entities;

    /// <summary>
    /// Differentiates audio low-pass filter clip behaviour.
    /// </summary>
    public enum AudioLowPassFilterClipType : byte
    {
        Animated,
        Sweep,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for audio low-pass filter clips.
    /// </summary>
    public struct AudioLowPassFilterClipData : IComponentData
    {
        public BlobAssetReference<AudioLowPassFilterClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AudioLowPassFilterClipBlob
    {
        [FieldOffset(0)]
        public AudioLowPassFilterClipType Type;

        [FieldOffset(4)]
        public AudioLowPassFilterConstantData Data;

        [FieldOffset(4)]
        public AudioCurveSweepData Sweep;
    }

    public struct AudioLowPassFilterConstantData
    {
        public float CutoffFrequency;
        public float LowpassResonanceQ;
    }
}
#endif
