// <copyright file="CMRotationComposerInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using Unity.Entities;
    using Unity.Properties;

    /// <summary>
    /// Stores Cinemachine rotation composer data captured when the track activates.
    /// </summary>
    public struct CMRotationComposerInitial : IComponentData, IInitial<CMRotationComposer>
    {
        /// <summary>
        /// Gets or sets full baseline component recorded during track activation.
        /// </summary>
        [CreateProperty]
        public CMRotationComposer Value { get; set; }
    }
}
#endif
