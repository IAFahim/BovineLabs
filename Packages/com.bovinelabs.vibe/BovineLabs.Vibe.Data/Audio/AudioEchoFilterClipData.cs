// <copyright file="AudioEchoFilterClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using System.Runtime.InteropServices;
    using Unity.Entities;

    /// <summary>
    /// Differentiates audio echo filter clip behaviour.
    /// </summary>
    public enum AudioEchoFilterClipType : byte
    {
        Animated,
        Sweep,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for audio echo filter clips.
    /// </summary>
    public struct AudioEchoFilterClipData : IComponentData
    {
        public BlobAssetReference<AudioEchoFilterClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AudioEchoFilterClipBlob
    {
        [FieldOffset(0)]
        public AudioEchoFilterClipType Type;

        [FieldOffset(4)]
        public AudioEchoFilterConstantData Data;

        [FieldOffset(4)]
        public AudioCurveSweepData Sweep;
    }

    public struct AudioEchoFilterConstantData
    {
        public float Delay;
        public float DecayRatio;
        public float WetMix;
        public float DryMix;
    }
}
#endif
