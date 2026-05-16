// <copyright file="CMCameraClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Entities;

    /// <summary>
    /// Differentiates Cinemachine camera clip behaviour.
    /// </summary>
    public enum CMCameraClipType : byte
    {
        Initial,
        Animated,
    }

    /// <summary>
    /// Serialized parameters describing how a lens clip should affect a Cinemachine camera.
    /// </summary>
    public struct CMCameraClipData : IComponentData
    {
        /// <summary>
        /// Field of view in degrees.
        /// </summary>
        public float FieldOfView;

        /// <summary>
        /// Orthographic size value.
        /// </summary>
        public float OrthographicSize;

        /// <summary>
        /// Interpretation mode for the clip.
        /// </summary>
        public CMCameraClipType Type;
    }
}
#endif
