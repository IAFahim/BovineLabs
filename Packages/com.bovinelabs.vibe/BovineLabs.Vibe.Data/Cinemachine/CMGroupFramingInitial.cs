// <copyright file="CMGroupFramingInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Vibe.Data;
    using Unity.Properties;

    /// <summary>
    /// Stores Cinemachine group framing data captured when the track activates.
    /// </summary>
    public struct CMGroupFramingInitial : IInitial<CMGroupFraming>
    {
        /// <summary>
        /// Full baseline component recorded during track activation.
        /// </summary>
        [CreateProperty]
        public CMGroupFraming Value { get; set; }
    }
}
#endif
