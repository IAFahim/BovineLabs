// <copyright file="CMHardLockToTargetInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures hard-lock-to-target values when the track activates.
    /// </summary>
    public struct CMHardLockToTargetInitial : IInitial<CMHardLockToTarget>
    {
        /// <inheritdoc/>
        public CMHardLockToTarget Value { get; set; }
    }
}
#endif
