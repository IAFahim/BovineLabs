// <copyright file="CameraMatrixShiftClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Camera
{
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Differentiates clip behaviour when animating camera matrix shift values.
    /// </summary>
    public enum CameraMatrixShiftClipType : byte
    {
        Initial,
        Animated,
    }

    /// <summary>
    /// Serialized parameters describing how a clip should affect the camera matrix shift.
    /// </summary>
    public struct CameraMatrixShiftClipData : IComponentData
    {
        public float2 ProjectionCenterOffset;

        public CameraMatrixShiftClipType Type;
    }
}
#endif
