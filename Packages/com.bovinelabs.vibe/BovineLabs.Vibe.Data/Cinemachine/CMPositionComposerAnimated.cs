// <copyright file="CMPositionComposerAnimated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Aggregated numeric values animated by the Cinemachine position composer track.
    /// </summary>
    public struct CMPositionComposerBlend
    {
        public float CameraDistance;
        public float DeadZoneDepth;
        public float3 TargetOffset;
        public float3 Damping;
        public float LookaheadTime;
        public float LookaheadSmoothing;
        public float2 ScreenPosition;
        public float2 DeadZoneSize;
        public float2 HardLimitSize;
        public float2 HardLimitOffset;
    }

    /// <summary>
    /// Runtime blend data stored per clip for the position composer track.
    /// </summary>
    public struct CMPositionComposerAnimated : IAnimatedComponent<CMPositionComposerBlend>
    {
        /// <inheritdoc/>
        [CreateProperty]
        public CMPositionComposerBlend Value { get; set; }
    }
}
#endif
