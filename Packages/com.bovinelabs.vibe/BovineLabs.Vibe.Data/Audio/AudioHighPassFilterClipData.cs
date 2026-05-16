// <copyright file="AudioHighPassFilterClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using System.Runtime.InteropServices;
    using Unity.Entities;

    /// <summary>
    /// Differentiates audio high-pass filter clip behaviour.
    /// </summary>
    public enum AudioHighPassFilterClipType : byte
    {
        Animated,
        Sweep,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for audio high-pass filter clips.
    /// </summary>
    public struct AudioHighPassFilterClipData : IComponentData
    {
        public BlobAssetReference<AudioHighPassFilterClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AudioHighPassFilterClipBlob
    {
        [FieldOffset(0)]
        public AudioHighPassFilterClipType Type;

        [FieldOffset(4)]
        public AudioHighPassFilterConstantData Data;

        [FieldOffset(4)]
        public AudioCurveSweepData Sweep;
    }

    public struct AudioHighPassFilterConstantData
    {
        public float CutoffFrequency;
        public float HighpassResonanceQ;
    }
}
#endif
