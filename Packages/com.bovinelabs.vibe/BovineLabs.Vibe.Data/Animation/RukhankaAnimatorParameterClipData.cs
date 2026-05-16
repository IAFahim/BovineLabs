// <copyright file="RukhankaAnimatorParameterClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Data.Animation
{
    using Rukhanka;
    using Unity.Entities;

    public enum RukhankaAnimatorParameterTriggerMode : byte
    {
        Set,
        Reset,
    }

    public enum RukhankaAnimatorParameterValueMode : byte
    {
        Constant,
        Random,
        Increment,
    }

    public struct RukhankaAnimatorParameterClipData : IComponentData
    {
        public BlobAssetReference<RukhankaAnimatorParameterClipBlob> Value;
    }

    public struct RukhankaAnimatorParameterClipBlob
    {
        public uint TriggerHash;
        public uint BoolHash;
        public uint IntHash;
        public uint FloatHash;
        public RukhankaAnimatorParameterTriggerMode TriggerMode;
        public RukhankaAnimatorParameterValueMode IntMode;
        public RukhankaAnimatorParameterValueMode FloatMode;
        public bool UpdateTrigger;
        public bool UpdateRandomTrigger;
        public bool UpdateBool;
        public bool UpdateRandomBool;
        public bool SetLayerWeight;
        public bool BoolValue;
        public int IntValue;
        public int IntMin;
        public int IntMax;
        public int IntIncrement;
        public float FloatValue;
        public float FloatMin;
        public float FloatMax;
        public float FloatIncrement;
        public int LayerIndex;
        public float LayerWeight;
        public uint Seed;
    }

    [InternalBufferCapacity(0)]
    public struct RukhankaAnimatorParameterRandomHash : IBufferElementData
    {
        public uint Hash;
    }

    [InternalBufferCapacity(0)]
    public struct RukhankaAnimatorParameterTrackHash : IBufferElementData
    {
        public uint Hash;
    }

    [InternalBufferCapacity(0)]
    public struct RukhankaAnimatorLayerIndex : IBufferElementData
    {
        public int Value;
    }

    [InternalBufferCapacity(0)]
    public struct RukhankaAnimatorParameterInitial : IBufferElementData
    {
        public uint Hash;
        public ParameterValue Value;
    }

    [InternalBufferCapacity(0)]
    public struct RukhankaAnimatorLayerInitial : IBufferElementData
    {
        public int LayerIndex;
        public float Weight;
    }
}

#endif
