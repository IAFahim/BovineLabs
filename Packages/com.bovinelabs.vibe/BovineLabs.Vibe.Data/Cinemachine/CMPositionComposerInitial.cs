// <copyright file="CMPositionComposerInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Vibe.Data;
    using Unity.Properties;

    /// <summary>
    /// Stores Cinemachine position composer data captured when the track activates.
    /// </summary>
    public struct CMPositionComposerInitial : IInitial<CMPositionComposer>
    {
        /// <summary>
        /// Full baseline component recorded during track activation.
        /// </summary>
        [CreateProperty]
        public CMPositionComposer Value { get; set; }
    }
}
#endif
