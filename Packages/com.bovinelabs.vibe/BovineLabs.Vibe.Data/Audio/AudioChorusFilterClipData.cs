// <copyright file="AudioChorusFilterClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using System.Runtime.InteropServices;
    using Unity.Entities;

    /// <summary>
    /// Differentiates audio chorus filter clip behaviour.
    /// </summary>
    public enum AudioChorusFilterClipType : byte
    {
        Animated,
        Sweep,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for audio chorus filter clips.
    /// </summary>
    public struct AudioChorusFilterClipData : IComponentData
    {
        public BlobAssetReference<AudioChorusFilterClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AudioChorusFilterClipBlob
    {
        [FieldOffset(0)]
        public AudioChorusFilterClipType Type;

        [FieldOffset(4)]
        public AudioChorusFilterConstantData Data;

        [FieldOffset(4)]
        public AudioCurveSweepData Sweep;
    }

    public struct AudioChorusFilterConstantData
    {
        public float DryMix;
        public float WetMix1;
        public float WetMix2;
        public float WetMix3;
        public float Delay;
        public float Rate;
        public float Depth;
    }
}
#endif
