// <copyright file="LightInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Light
{
    using BovineLabs.Bridge.Data.Lighting;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures the initial light data when the track activates.
    /// </summary>
    public struct LightInitial : IInitial<LightData>
    {
        /// <inheritdoc/>
        public LightData Value { get; set; }
    }
}
#endif
