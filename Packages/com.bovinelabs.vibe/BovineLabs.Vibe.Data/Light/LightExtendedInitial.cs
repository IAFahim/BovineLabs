// <copyright file="LightExtendedInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Light
{
    using BovineLabs.Bridge.Data.Lighting;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures the initial extended light data when the track activates.
    /// </summary>
    public struct LightExtendedInitial : IInitial<LightDataExtended>
    {
        /// <inheritdoc/>
        public LightDataExtended Value { get; set; }
    }
}
#endif
