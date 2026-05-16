// <copyright file="CMHardLockToTargetAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Runtime data blended for hard-lock-to-target tracks.
    /// </summary>
    public struct CMHardLockToTargetBlend
    {
        public float Damping;
    }

    /// <summary>
    /// Animated state assigned per timeline clip.
    /// </summary>
    public struct CMHardLockToTargetAnimated : IAnimatedComponent<CMHardLockToTargetBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMHardLockToTargetBlend Value { get; set; }
    }
}
#endif
