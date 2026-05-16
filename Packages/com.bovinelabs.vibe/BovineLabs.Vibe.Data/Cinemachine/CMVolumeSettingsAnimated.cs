// <copyright file="CMVolumeSettingsAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Runtime data blended for Cinemachine volume settings tracks.
    /// </summary>
    public struct CMVolumeSettingsBlend
    {
        public float Weight;
        public float FocusOffset;
    }

    /// <summary>
    /// Animated state assigned per timeline clip.
    /// </summary>
    public struct CMVolumeSettingsAnimated : IAnimatedComponent<CMVolumeSettingsBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMVolumeSettingsBlend Value { get; set; }
    }
}
#endif
