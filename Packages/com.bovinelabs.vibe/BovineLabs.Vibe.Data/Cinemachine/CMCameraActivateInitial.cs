// <copyright file="CMCameraActivateInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;

    /// <summary>
    /// Stores Cinemachine camera settings captured when an activation track becomes active.
    /// </summary>
    public struct CMCameraActivateInitial : IComponentData
    {
        /// <summary>
        /// Original enabled state.
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Original priority settings.
        /// </summary>
        public PrioritySettings Priority;

        /// <summary>
        /// Original output channel.
        /// </summary>
        public OutputChannels OutputChannel;

        /// <summary>
        /// Original blend hint.
        /// </summary>
        public CinemachineCore.BlendHints BlendHint;
    }
}
#endif
