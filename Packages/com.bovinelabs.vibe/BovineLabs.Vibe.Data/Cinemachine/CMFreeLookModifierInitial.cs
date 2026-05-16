// <copyright file="CMFreeLookModifierInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;

    /// <summary>
    /// Stores Cinemachine free look modifier data captured when the track activates.
    /// </summary>
    public struct CMFreeLookModifierInitial : IInitial<CMFreeLookModifier>
    {
        public CMFreeLookModifier Value { get; set; }
    }
}
#endif
