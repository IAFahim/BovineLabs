// <copyright file="CMCameraInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Entities;

    /// <summary>
    /// Stores Cinemachine lens data captured when a timeline track activates.
    /// </summary>
    public struct CMCameraInitial : IComponentData
    {
        /// <summary>
        /// Field of view recorded at track activation.
        /// </summary>
        public float FieldOfView;

        /// <summary>
        /// Orthographic size recorded at track activation.
        /// </summary>
        public float OrthographicSize;
    }
}
#endif
