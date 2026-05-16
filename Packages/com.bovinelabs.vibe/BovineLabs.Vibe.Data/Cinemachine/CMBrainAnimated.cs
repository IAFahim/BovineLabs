// <copyright file="CMBrainAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Runtime data blended for Cinemachine brain tracks.
    /// </summary>
    public struct CMBrainBlend
    {
        public float DefaultBlendTime;
    }

    /// <summary>
    /// Animated state assigned per timeline clip.
    /// </summary>
    public struct CMBrainAnimated : IAnimatedComponent<CMBrainBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMBrainBlend Value { get; set; }
    }
}
#endif
