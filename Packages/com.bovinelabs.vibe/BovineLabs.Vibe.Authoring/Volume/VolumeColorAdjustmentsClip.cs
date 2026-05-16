// <copyright file="VolumeColorAdjustmentsClip.cs" company="BovineLabs">
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
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that applies constant color adjustments overrides.
    /// </summary>
    [Serializable]
    public class VolumeColorAdjustmentsClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the color adjustments override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override post exposure while the clip is active.")]
        private bool overridePostExposure = true;

        [SerializeField]
        [Tooltip("Post exposure value in EV.")]
        private float postExposure;

        [SerializeField]
        [Tooltip("Override contrast while the clip is active.")]
        private bool overrideContrast = true;

        [SerializeField]
        [Tooltip("Contrast value.")]
        private float contrast;

        [SerializeField]
        [Tooltip("Override color filter while the clip is active.")]
        private bool overrideColorFilter = true;

        [SerializeField]
        [Tooltip("Color filter value.")]
        private Color colorFilter = Color.white;

        [SerializeField]
        [Tooltip("Override hue shift while the clip is active.")]
        private bool overrideHueShift;

        [SerializeField]
        [Tooltip("Hue shift value.")]
        private float hueShift;

        [SerializeField]
        [Tooltip("Override saturation while the clip is active.")]
        private bool overrideSaturation;

        [SerializeField]
        [Tooltip("Saturation value.")]
        private float saturation;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeColorAdjustmentsAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeColorAdjustmentsClipBlob>();
            blob.Type = VolumeColorAdjustmentsClipType.Constant;

            blob.Constant = new VolumeColorAdjustmentsConstantData
            {
                Active = this.active,
                PostExposureOverride = this.overridePostExposure,
                PostExposure = this.postExposure,
                ContrastOverride = this.overrideContrast,
                Contrast = this.contrast,
                ColorFilterOverride = this.overrideColorFilter,
                ColorFilter = new float4(this.colorFilter.r, this.colorFilter.g, this.colorFilter.b, this.colorFilter.a),
                HueShiftOverride = this.overrideHueShift,
                HueShift = this.hueShift,
                SaturationOverride = this.overrideSaturation,
                Saturation = this.saturation,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeColorAdjustmentsClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeColorAdjustmentsClipData { Value = blobRef });
        }
    }
}
#endif
#endif
