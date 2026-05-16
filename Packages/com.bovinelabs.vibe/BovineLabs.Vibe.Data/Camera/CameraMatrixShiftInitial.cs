// <copyright file="CameraMatrixShiftInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Camera
{
    using BovineLabs.Bridge.Data.Camera;
    using Unity.Entities;

    /// <summary>
    /// Captured initial camera matrix shift values for resetting when the track deactivates.
    /// </summary>
    public struct CameraMatrixShiftInitial : IComponentData
    {
        public CameraViewSpaceOffset Offset;
    }
}
#endif
