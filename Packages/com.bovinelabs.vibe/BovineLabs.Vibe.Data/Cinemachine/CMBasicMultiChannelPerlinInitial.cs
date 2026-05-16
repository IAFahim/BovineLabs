// <copyright file="CMBasicMultiChannelPerlinInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Stores Cinemachine basic multi channel perlin data captured when the track activates.
    /// </summary>
    public struct CMBasicMultiChannelPerlinInitial : IInitial<CMBasicMultiChannelPerlin>
    {
        /// <inheritdoc/>
        public CMBasicMultiChannelPerlin Value { get; set; }
    }
}
#endif
