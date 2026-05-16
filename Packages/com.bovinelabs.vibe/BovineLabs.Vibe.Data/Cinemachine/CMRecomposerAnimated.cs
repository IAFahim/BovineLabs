// <copyright file="CMRecomposerAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Properties;

    /// <summary>
    /// Aggregated numeric values animated by the Cinemachine recomposer track.
    /// </summary>
    public struct CMRecomposerBlend
    {
        public float Tilt;
        public float Pan;
        public float Dutch;
        public float ZoomScale;
        public float FollowAttachment;
        public float LookAtAttachment;
    }

    /// <summary>
    /// Runtime blend data stored per clip for the recomposer track.
    /// </summary>
    public struct CMRecomposerAnimated : IAnimatedComponent<CMRecomposerBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMRecomposerBlend Value { get; set; }
    }
}
#endif
