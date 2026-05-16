// <copyright file="VolumePaniniProjectionClip.cs" company="BovineLabs">
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
    /// Timeline clip that applies constant panini projection overrides.
    /// </summary>
    [Serializable]
    public class VolumePaniniProjectionClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the panini projection override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override panini distance while the clip is active.")]
        private bool overrideDistance = true;

        [SerializeField]
        [Tooltip("Panini distance value.")]
        private float distance;

        [SerializeField]
        [Tooltip("Override crop to fit while the clip is active.")]
        private bool overrideCropToFit;

        [SerializeField]
        [Tooltip("Crop to fit value.")]
        private float cropToFit = 1f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumePaniniProjectionAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumePaniniProjectionClipBlob>();
            blob.Type = VolumePaniniProjectionClipType.Constant;
            blob.Constant = new VolumePaniniProjectionConstantData
            {
                Active = this.active,
                DistanceOverride = this.overrideDistance,
                Distance = this.distance,
                CropToFitOverride = this.overrideCropToFit,
                CropToFit = this.cropToFit,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumePaniniProjectionClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumePaniniProjectionClipData { Value = blobRef });
        }
    }
}
#endif
#endif
