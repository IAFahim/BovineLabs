// <copyright file="CMGroupFramingClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Differentiates Cinemachine group framing clip behaviour.
    /// </summary>
    public enum CMGroupFramingClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each Cinemachine group framing clip.
    /// </summary>
    public struct CMGroupFramingClipData : IComponentData
    {
        public BlobAssetReference<CMGroupFramingClipBlob> Value;
    }

    /// <summary>
    /// Blob data for Cinemachine group framing clips.
    /// </summary>
    public struct CMGroupFramingClipBlob
    {
        public CMGroupFramingClipType Type;
        public CinemachineGroupFraming.FramingModes FramingMode;
        public float FramingSize;
        public float2 CenterOffset;
        public float Damping;
        public CinemachineGroupFraming.SizeAdjustmentModes SizeAdjustment;
        public CinemachineGroupFraming.LateralAdjustmentModes LateralAdjustment;
        public float2 FovRange;
        public float2 DollyRange;
        public float2 OrthoSizeRange;
    }
}
#endif
