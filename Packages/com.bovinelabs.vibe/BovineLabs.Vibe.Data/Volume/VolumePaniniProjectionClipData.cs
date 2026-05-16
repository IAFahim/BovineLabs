// <copyright file="VolumePaniniProjectionClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Volume
{
    using System.Runtime.InteropServices;
    using BovineLabs.Bridge.Data.Volume;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using Unity.Entities;

    /// <summary>
    /// Differentiates panini projection clip behaviour.
    /// </summary>
    public enum VolumePaniniProjectionClipType : byte
    {
        Constant,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for panini projection clips.
    /// </summary>
    public struct VolumePaniniProjectionClipData : IComponentData
    {
        public BlobAssetReference<VolumePaniniProjectionClipBlob> Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VolumePaniniProjectionClipBlob
    {
        [FieldOffset(0)]
        public VolumePaniniProjectionClipType Type;

        [FieldOffset(4)]
        public VolumePaniniProjectionConstantData Constant;
    }

    /// <summary>
    /// Constant panini projection overrides applied while the clip is active.
    /// </summary>
    public struct VolumePaniniProjectionConstantData
    {
        public float Distance;
        public float CropToFit;
        public bool Active;
        public bool DistanceOverride;
        public bool CropToFitOverride;
    }

    /// <summary>
    /// Blended values for panini projection animation.
    /// </summary>
    public struct VolumePaniniProjectionBlend
    {
        public float Distance;
        public float CropToFit;
        public bool DistanceOverride;
        public bool CropToFitOverride;
    }

    /// <summary>
    /// Runtime state stored per clip for panini projection animation.
    /// </summary>
    public struct VolumePaniniProjectionAnimated : IAnimatedComponent<VolumePaniniProjectionBlend>
    {
        /// <inheritdoc />
        public VolumePaniniProjectionBlend Value { get; set; }
    }

    /// <summary>
    /// Captures initial panini projection values when the track activates.
    /// </summary>
    public struct VolumePaniniProjectionInitial : IInitial<VolumePaniniProjection>
    {
        /// <inheritdoc />
        public VolumePaniniProjection Value { get; set; }
    }
}
#endif
