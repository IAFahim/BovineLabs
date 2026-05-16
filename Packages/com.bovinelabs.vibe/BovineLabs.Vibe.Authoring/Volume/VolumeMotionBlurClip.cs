// <copyright file="VolumeMotionBlurClip.cs" company="BovineLabs">
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
    using UnityEngine.Rendering.Universal;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that applies constant motion blur overrides.
    /// </summary>
    [Serializable]
    public class VolumeMotionBlurClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the motion blur override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override motion blur mode while the clip is active.")]
        private bool overrideMode = true;

        [SerializeField]
        [Tooltip("Motion blur mode to apply when override is enabled.")]
        private MotionBlurMode mode = MotionBlurMode.CameraAndObjects;

        [SerializeField]
        [Tooltip("Override motion blur quality while the clip is active.")]
        private bool overrideQuality = true;

        [SerializeField]
        [Tooltip("Motion blur quality to apply when override is enabled.")]
        private MotionBlurQuality quality = MotionBlurQuality.High;

        [SerializeField]
        [Tooltip("Override motion blur intensity while the clip is active.")]
        private bool overrideIntensity = true;

        [SerializeField]
        [Tooltip("Motion blur intensity value.")]
        private float intensity = 0.5f;

        [SerializeField]
        [Tooltip("Override motion blur clamp while the clip is active.")]
        private bool overrideClamp;

        [SerializeField]
        [Tooltip("Motion blur clamp value.")]
        private float clamp = 0.05f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeMotionBlurAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeMotionBlurClipBlob>();
            blob.Type = VolumeMotionBlurClipType.Constant;
            blob.Constant = new VolumeMotionBlurConstantData
            {
                Active = this.active,
                ModeOverride = this.overrideMode,
                Mode = this.mode,
                QualityOverride = this.overrideQuality,
                Quality = this.quality,
                IntensityOverride = this.overrideIntensity,
                Intensity = this.intensity,
                ClampOverride = this.overrideClamp,
                Clamp = this.clamp,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeMotionBlurClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeMotionBlurClipData { Value = blobRef });
        }
    }
}
#endif
#endif
