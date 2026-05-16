// <copyright file="CMSplineDollyInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Stores Cinemachine spline dolly data captured when the track activates.
    /// </summary>
    public struct CMSplineDollyInitial : IInitial<CMSplineDolly>
    {
        public CMSplineDolly Value { get; set; }
    }
}
#endif
