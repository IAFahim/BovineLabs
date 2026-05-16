// <copyright file="CMSplineDollyTargetInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Stores Cinemachine spline dolly target captured when the track activates.
    /// </summary>
    public struct CMSplineDollyTargetInitial : IInitial<CMSplineDollyTarget>
    {
        public CMSplineDollyTarget Value { get; set; }
    }
}
#endif
