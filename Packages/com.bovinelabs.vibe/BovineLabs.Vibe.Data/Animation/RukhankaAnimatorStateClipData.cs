// <copyright file="RukhankaAnimatorStateClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Data.Animation
{
    using System.Runtime.InteropServices;
    using Rukhanka;
    using Unity.Entities;

    public enum RukhankaAnimatorStateClipType : byte
    {
        Crossfade,
        PlayState,
    }

    public enum RukhankaAnimatorCrossfadeMode : byte
    {
        Normalized,
        Seconds,
    }

    public enum RukhankaAnimatorPlayStateMode : byte
    {
        NormalizedTime,
        FixedTime,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct RukhankaAnimatorStateClipData : IComponentData
    {
        [FieldOffset(0)]
        public RukhankaAnimatorStateClipType Type;

        [FieldOffset(4)]
        public RukhankaAnimatorStateCrossfadeData Crossfade;

        [FieldOffset(4)]
        public RukhankaAnimatorStatePlayStateData PlayState;
    }

    public struct RukhankaAnimatorStateCrossfadeData
    {
        public uint StateHash;
        public RukhankaAnimatorCrossfadeMode Mode;
        public float TransitionDuration;
        public float TimeOffset;
        public float NormalizedTransitionDuration;
        public float NormalizedTimeOffset;
        public float NormalizedTransitionTime;
        public int LayerIndex;
        public uint Seed;
        public bool UseRandomState;
    }

    public struct RukhankaAnimatorStatePlayStateData
    {
        public uint StateHash;
        public RukhankaAnimatorPlayStateMode Mode;
        public float NormalizedTime;
        public float FixedTimeSeconds;
        public int LayerIndex;
        public bool SetLayerWeight;
        public int WeightLayerIndex;
        public float LayerWeight;
    }

    [InternalBufferCapacity(0)]
    public struct RukhankaAnimatorStateRandomHash : IBufferElementData
    {
        public uint Hash;
    }

    [InternalBufferCapacity(0)]
    public struct RukhankaAnimatorStateLayerUsage : IBufferElementData
    {
        public int LayerIndex;
        public bool RestoreState;
        public bool RestoreWeight;
    }

    [InternalBufferCapacity(0)]
    public struct RukhankaAnimatorStateLayerInitial : IBufferElementData
    {
        public int LayerIndex;
        public bool RestoreState;
        public bool RestoreWeight;
        public float Weight;
        public RuntimeAnimatorData RuntimeData;
    }
}

#endif
