// <copyright file="CMRotationComposerAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Aggregated numeric values animated by the Cinemachine rotation composer track.
    /// </summary>
    public struct CMRotationComposerBlend
    {
        public float3 TargetOffset;
        public float2 Damping;
        public float LookaheadTime;
        public float LookaheadSmoothing;
        public float2 ScreenPosition;
        public float2 DeadZoneSize;
        public float2 HardLimitSize;
        public float2 HardLimitOffset;
    }

    /// <summary>
    /// Runtime blend data stored per clip for the rotation composer track.
    /// </summary>
    public struct CMRotationComposerAnimated : IAnimatedComponent<CMRotationComposerBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMRotationComposerBlend Value { get; set; }
    }
}
#endif
