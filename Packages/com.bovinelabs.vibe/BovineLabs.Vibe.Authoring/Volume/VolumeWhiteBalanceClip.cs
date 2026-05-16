// <copyright file="VolumeWhiteBalanceClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Volume
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Volume;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that applies constant white balance overrides.
    /// </summary>
    [Serializable]
    public class VolumeWhiteBalanceClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the white balance override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override temperature while the clip is active.")]
        private bool overrideTemperature = true;

        [SerializeField]
        [Tooltip("Temperature value.")]
        private float temperature;

        [SerializeField]
        [Tooltip("Override tint while the clip is active.")]
        private bool overrideTint = true;

        [SerializeField]
        [Tooltip("Tint value.")]
        private float tint;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeWhiteBalanceAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeWhiteBalanceClipBlob>();
            blob.Type = VolumeWhiteBalanceClipType.Constant;
            blob.Constant = new VolumeWhiteBalanceConstantData
            {
                Active = this.active,
                TemperatureOverride = this.overrideTemperature,
                Temperature = this.temperature,
                TintOverride = this.overrideTint,
                Tint = this.tint,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeWhiteBalanceClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeWhiteBalanceClipData { Value = blobRef });
        }
    }
}
#endif
#endif
