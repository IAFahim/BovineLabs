// <copyright file="RukhankaAnimatorSpeedClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Data.Animation
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Timeline.Data;
    using Unity.Entities;
    using Unity.Properties;

    public enum RukhankaAnimatorSpeedMode : byte
    {
        Constant,
        Random,
        Curve,
    }

    public struct RukhankaAnimatorSpeedClipData : IComponentData
    {
        public BlobAssetReference<RukhankaAnimatorSpeedClipBlob> Value;
    }

    public struct RukhankaAnimatorSpeedClipBlob
    {
        public RukhankaAnimatorSpeedMode Mode;
        public float MinSpeed;
        public float MaxSpeed;
        public bool Relative;
        public uint Seed;
        public BlobCurve Curve;
    }

    public struct RukhankaAnimatorSpeedAnimated : IAnimatedComponent<float>
    {
        [CreateProperty]
        public float Value { get; set; }
    }

    [InternalBufferCapacity(0)]
    public struct RukhankaAnimatorSpeedInitial : IBufferElementData
    {
        public int LayerIndex;
        public float Speed;
    }
}

#endif
