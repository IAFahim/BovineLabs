namespace BovineLabs.Vibe.Data.NonUniformScale
{
    using BovineLabs.Timeline.Data;
    using Unity.Mathematics;
    using Unity.Properties;

    /// <summary>
    /// Runtime state used by timeline to animate non-uniform scale clips.
    /// </summary>
    public struct NonUniformScaleAnimated : IAnimatedComponent<float3>
    {
        public float3 BaseScale;

        /// <inheritdoc/>
        [CreateProperty]
        public float3 Value { get; set; }
    }
}
