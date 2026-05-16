// <copyright file="CMHardLookAtAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Runtime data blended for hard-look-at tracks.
    /// </summary>
    public struct CMHardLookAtBlend
    {
        public float3 LookAtOffset;
    }

    /// <summary>
    /// Animated state assigned per timeline clip.
    /// </summary>
    public struct CMHardLookAtAnimated : IAnimatedComponent<CMHardLookAtBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMHardLookAtBlend Value { get; set; }
    }
}
#endif
