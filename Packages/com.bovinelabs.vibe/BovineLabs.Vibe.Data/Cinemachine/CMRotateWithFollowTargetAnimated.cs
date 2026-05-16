// <copyright file="CMRotateWithFollowTargetAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Runtime data blended for rotate-with-follow-target tracks.
    /// </summary>
    public struct CMRotateWithFollowTargetBlend
    {
        public float Damping;
    }

    /// <summary>
    /// Animated state assigned per timeline clip.
    /// </summary>
    public struct CMRotateWithFollowTargetAnimated : IAnimatedComponent<CMRotateWithFollowTargetBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMRotateWithFollowTargetBlend Value { get; set; }
    }
}
#endif
