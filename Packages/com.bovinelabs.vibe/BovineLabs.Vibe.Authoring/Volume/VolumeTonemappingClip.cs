// <copyright file="VolumeTonemappingClip.cs" company="BovineLabs">
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
    /// Timeline clip that applies constant tonemapping overrides.
    /// </summary>
    [Serializable]
    public class VolumeTonemappingClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the tonemapping override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override tonemapping mode while the clip is active.")]
        private bool overrideMode = true;

        [SerializeField]
        [Tooltip("Tonemapping mode to apply when override is enabled.")]
        private TonemappingMode mode = TonemappingMode.None;

        [SerializeField]
        [Tooltip("Override neutral HDR range reduction mode while the clip is active.")]
        private bool overrideNeutralHDRRangeReductionMode;

        [SerializeField]
        [Tooltip("Neutral HDR range reduction mode to apply when override is enabled.")]
        private NeutralRangeReductionMode neutralHDRRangeReductionMode = NeutralRangeReductionMode.BT2390;

        [SerializeField]
        [Tooltip("Override ACES preset while the clip is active.")]
        private bool overrideAcesPreset;

        [SerializeField]
        [Tooltip("ACES preset to apply when override is enabled.")]
        private HDRACESPreset acesPreset = HDRACESPreset.ACES1000Nits;

        [SerializeField]
        [Tooltip("Override hue shift amount while the clip is active.")]
        private bool overrideHueShiftAmount;

        [SerializeField]
        [Tooltip("Hue shift amount value.")]
        private float hueShiftAmount;

        [SerializeField]
        [Tooltip("Override detect paper white while the clip is active.")]
        private bool overrideDetectPaperWhite;

        [SerializeField]
        [Tooltip("Detect paper white value.")]
        private bool detectPaperWhite;

        [SerializeField]
        [Tooltip("Override paper white while the clip is active.")]
        private bool overridePaperWhite;

        [SerializeField]
        [Tooltip("Paper white value.")]
        private float paperWhite = 300f;

        [SerializeField]
        [Tooltip("Override detect brightness limits while the clip is active.")]
        private bool overrideDetectBrightnessLimits;

        [SerializeField]
        [Tooltip("Detect brightness limits value.")]
        private bool detectBrightnessLimits = true;

        [SerializeField]
        [Tooltip("Override min nits while the clip is active.")]
        private bool overrideMinNits;

        [SerializeField]
        [Tooltip("Min nits value.")]
        private float minNits = 0.005f;

        [SerializeField]
        [Tooltip("Override max nits while the clip is active.")]
        private bool overrideMaxNits;

        [SerializeField]
        [Tooltip("Max nits value.")]
        private float maxNits = 1000f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeTonemappingAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeTonemappingClipBlob>();
            blob.Type = VolumeTonemappingClipType.Constant;
            blob.Constant = new VolumeTonemappingConstantData
            {
                Active = this.active,
                ModeOverride = this.overrideMode,
                Mode = this.mode,
                NeutralHDRRangeReductionModeOverride = this.overrideNeutralHDRRangeReductionMode,
                NeutralHDRRangeReductionMode = this.neutralHDRRangeReductionMode,
                AcesPresetOverride = this.overrideAcesPreset,
                AcesPreset = this.acesPreset,
                HueShiftAmountOverride = this.overrideHueShiftAmount,
                HueShiftAmount = this.hueShiftAmount,
                DetectPaperWhiteOverride = this.overrideDetectPaperWhite,
                DetectPaperWhite = this.detectPaperWhite,
                PaperWhiteOverride = this.overridePaperWhite,
                PaperWhite = this.paperWhite,
                DetectBrightnessLimitsOverride = this.overrideDetectBrightnessLimits,
                DetectBrightnessLimits = this.detectBrightnessLimits,
                MinNitsOverride = this.overrideMinNits,
                MinNits = this.minNits,
                MaxNitsOverride = this.overrideMaxNits,
                MaxNits = this.maxNits,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeTonemappingClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeTonemappingClipData { Value = blobRef });
        }
    }
}
#endif
#endif
