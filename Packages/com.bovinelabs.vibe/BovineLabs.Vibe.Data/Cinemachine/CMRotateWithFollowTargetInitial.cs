// <copyright file="CMRotateWithFollowTargetInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Vibe.Data;

    /// <summary>
    /// Captures rotate-with-follow-target values when the track activates.
    /// </summary>
    public struct CMRotateWithFollowTargetInitial : IInitial<CMRotateWithFollowTarget>
    {
        /// <inheritdoc/>
        public CMRotateWithFollowTarget Value { get; set; }
    }
}
#endif
