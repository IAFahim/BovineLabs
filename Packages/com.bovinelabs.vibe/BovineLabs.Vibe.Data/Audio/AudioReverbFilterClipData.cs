// <copyright file="AudioReverbFilterClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using System.Runtime.InteropServices;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Differentiates audio reverb filter clip behaviour.
    /// </summary>
    public enum AudioReverbFilterClipType : byte
    {
        Animated,
        Sweep,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for audio reverb filter clips.
    /// </summary>
    public struct AudioReverbFilterClipData : IComponentData
    {
        public BlobAssetReference<AudioReverbFilterClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AudioReverbFilterClipBlob
    {
        [FieldOffset(0)]
        public AudioReverbFilterClipType Type;

        [FieldOffset(4)]
        public AudioReverbFilterConstantData Data;

        [FieldOffset(4)]
        public AudioCurveSweepData Sweep;
    }

    public struct AudioReverbFilterConstantData
    {
        public AudioReverbPreset ReverbPreset;
        public bool OverrideReverbPreset;
        public float DryLevel;
        public float Room;
        public float RoomHF;
        public float RoomLF;
        public float DecayTime;
        public float DecayHFRatio;
        public float ReflectionsLevel;
        public float ReflectionsDelay;
        public float ReverbLevel;
        public float ReverbDelay;
        public float HFReference;
        public float LFReference;
        public float Diffusion;
        public float Density;
    }
}
#endif
