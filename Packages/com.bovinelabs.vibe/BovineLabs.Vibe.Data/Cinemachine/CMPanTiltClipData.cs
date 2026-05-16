// <copyright file="CMPanTiltClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;

    /// <summary>
    /// Differentiates Cinemachine pan/tilt clip behaviour.
    /// </summary>
    public enum CMPanTiltClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each Cinemachine pan/tilt clip.
    /// </summary>
    public struct CMPanTiltClipData : IComponentData
    {
        public CMPanTiltClipType Type;
        public CinemachinePanTilt.ReferenceFrames ReferenceFrame;
        public CinemachinePanTilt.RecenterTargetModes RecenterTarget;
        public InputAxis PanAxis;
        public InputAxis TiltAxis;
    }
}
#endif
