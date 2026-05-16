// <copyright file="CMRotationComposerClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Differentiates Cinemachine rotation composer clip behaviour.
    /// </summary>
    public enum CMRotationComposerClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized parameters baked per Cinemachine rotation composer clip.
    /// </summary>
    public struct CMRotationComposerClipData : IComponentData
    {
        public BlobAssetReference<CMRotationComposerClipBlob> Value;
    }

    /// <summary>
    /// Blob data for Cinemachine rotation composer clips.
    /// </summary>
    public struct CMRotationComposerClipBlob
    {
        public CMRotationComposerClipType Type;
        public float3 TargetOffset;
        public float2 Damping;
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
