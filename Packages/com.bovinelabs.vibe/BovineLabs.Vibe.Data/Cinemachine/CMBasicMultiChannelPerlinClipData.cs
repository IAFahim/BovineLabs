// <copyright file="CMBasicMultiChannelPerlinClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Differentiates Cinemachine basic multi channel perlin clip behaviour.
    /// </summary>
    public enum CMBasicMultiChannelPerlinClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each Cinemachine basic multi channel perlin clip.
    /// </summary>
    public struct CMBasicMultiChannelPerlinClipData : IComponentData
    {
        public BlobAssetReference<CMBasicMultiChannelPerlinClipBlob> Value;
        public UnityObjectRef<NoiseSettings> NoiseProfile;
    }

    /// <summary>
    /// Blob data for Cinemachine basic multi channel perlin clips.
    /// </summary>
    public struct CMBasicMultiChannelPerlinClipBlob
    {
        public CMBasicMultiChannelPerlinClipType Type;
        public float AmplitudeGain;
        public float FrequencyGain;
        public float3 PivotOffset;
        public bool OverrideNoiseProfile;
    }
}
#endif
