// <copyright file="CameraMatrixShiftAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Camera
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
     /// Blendable value used to drive CameraMatrixShiftOverride and CameraViewSpaceOffset.
     /// </summary>
    public struct CameraMatrixShiftBlend
    {
        public float2 ProjectionCenterOffset;
    }

    /// <summary>
    /// Runtime state used by timeline to animate camera matrix shift clips.
    /// </summary>
    public struct CameraMatrixShiftAnimated : IAnimatedComponent<CameraMatrixShiftBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CameraMatrixShiftBlend Value { get; set; }
    }
}
#endif
