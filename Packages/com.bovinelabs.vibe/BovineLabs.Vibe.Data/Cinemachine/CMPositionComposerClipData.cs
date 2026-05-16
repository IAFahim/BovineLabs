// <copyright file="CMPositionComposerClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Differentiates Cinemachine position composer clip behaviour.
    /// </summary>
    public enum CMPositionComposerClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized parameters baked per Cinemachine position composer clip.
    /// </summary>
    public struct CMPositionComposerClipData : IComponentData
    {
        public BlobAssetReference<CMPositionComposerClipBlob> Value;
    }

    /// <summary>
    /// Blob data for Cinemachine position composer clips.
    /// </summary>
    public struct CMPositionComposerClipBlob
    {
        public CMPositionComposerClipType Type;
        public float CameraDistance;
        public float DeadZoneDepth;
        public float3 TargetOffset;
        public float3 Damping;
        public float LookaheadTime;
        public float LookaheadSmoothing;
        public float2 ScreenPosition;
        public float2 DeadZoneSize;
        public float2 HardLimitSize;
        public float2 HardLimitOffset;
        public bool LookaheadEnabled;
        public bool LookaheadIgnoreY;
        public bool DeadZoneEnabled;
        public bool HardLimitsEnabled;
        public bool CenterOnActivate;
    }
}
#endif
