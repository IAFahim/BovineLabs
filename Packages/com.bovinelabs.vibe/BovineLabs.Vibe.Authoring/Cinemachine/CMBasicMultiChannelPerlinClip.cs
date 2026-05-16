// <copyright file="CMBasicMultiChannelPerlinClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Cinemachine;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that blends Cinemachine basic multi channel perlin shake parameters.
    /// </summary>
    [Serializable]
    public class CMBasicMultiChannelPerlinClip : DOTSClip, ITimelineClipAsset
    {
        [Min(0f)]
        [Tooltip("Amplitude gain applied to the perlin noise.")]
        [SerializeField]
        private float amplitudeGain = 1f;

        [Min(0f)]
        [Tooltip(Strings.ShakeFrequencyTooltip)]
        [SerializeField]
        private float frequencyGain = 1f;

        [Tooltip("Offset of the shake pivot relative to the camera.")]
        [SerializeField]
        private Vector3 pivotOffset = Vector3.zero;

        [Tooltip("If enabled, overrides the noise profile while the clip is active.")]
        [SerializeField]
        private bool overrideNoiseProfile;

        [SerializeField]
        private NoiseSettings noiseProfile;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.amplitudeGain = Mathf.Max(0f, this.amplitudeGain);
            this.frequencyGain = Mathf.Max(0f, this.frequencyGain);

            context.Baker.AddComponent<CMBasicMultiChannelPerlinAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<CMBasicMultiChannelPerlinClipBlob>();
            blob.Type = CMBasicMultiChannelPerlinClipType.Animated;
            blob.AmplitudeGain = this.amplitudeGain;
            blob.FrequencyGain = this.frequencyGain;
            blob.PivotOffset = new float3(this.pivotOffset.x, this.pivotOffset.y, this.pivotOffset.z);
            blob.OverrideNoiseProfile = this.overrideNoiseProfile;

            var blobRef = builder.CreateBlobAssetReference<CMBasicMultiChannelPerlinClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(
                clipEntity,
                new CMBasicMultiChannelPerlinClipData
                {
                    Value = blobRef,
                    NoiseProfile = this.overrideNoiseProfile ? this.noiseProfile : default,
                });
        }

        private void OnValidate()
        {
            this.amplitudeGain = Mathf.Max(0f, this.amplitudeGain);
            this.frequencyGain = Mathf.Max(0f, this.frequencyGain);
        }
    }
}
#endif
