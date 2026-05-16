// <copyright file="CMCameraOffsetClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Differentiates Cinemachine camera offset clip behaviour.
    /// </summary>
    public enum CMCameraOffsetClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each Cinemachine camera offset clip.
    /// </summary>
    public struct CMCameraOffsetClipData : IComponentData
    {
        public CMCameraOffsetClipType Type;
        public float3 Offset;
        public CinemachineCore.Stage ApplyAfter;
        public bool PreserveComposition;
    }
}
#endif
