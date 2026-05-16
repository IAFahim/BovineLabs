// <copyright file="VolumeChromaticAberrationClip.cs" company="BovineLabs">
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
    /// Timeline clip that applies constant chromatic aberration overrides.
    /// </summary>
    [Serializable]
    public class VolumeChromaticAberrationClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the chromatic aberration override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override chromatic aberration intensity while the clip is active.")]
        private bool overrideIntensity = true;

        [SerializeField]
        [Tooltip("Chromatic aberration intensity value.")]
        private float intensity;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeChromaticAberrationAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeChromaticAberrationClipBlob>();
            blob.Type = VolumeChromaticAberrationClipType.Constant;
            blob.Constant = new VolumeChromaticAberrationConstantData
            {
                Active = this.active,
                IntensityOverride = this.overrideIntensity,
                Intensity = this.intensity,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeChromaticAberrationClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeChromaticAberrationClipData { Value = blobRef });
        }
    }
}
#endif
#endif
