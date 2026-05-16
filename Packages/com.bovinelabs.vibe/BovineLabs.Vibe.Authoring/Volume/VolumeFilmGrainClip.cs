// <copyright file="VolumeFilmGrainClip.cs" company="BovineLabs">
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
    /// Timeline clip that applies constant film grain overrides.
    /// </summary>
    [Serializable]
    public class VolumeFilmGrainClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the film grain override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override the film grain type while the clip is active.")]
        private bool overrideType = true;

        [SerializeField]
        [Tooltip("Film grain type to apply when override is enabled.")]
        private FilmGrainLookup type = FilmGrainLookup.Thin1;

        [SerializeField]
        [Tooltip("Override the film grain intensity while the clip is active.")]
        private bool overrideIntensity = true;

        [SerializeField]
        [Tooltip("Film grain intensity value.")]
        private float intensity;

        [SerializeField]
        [Tooltip("Override the film grain response while the clip is active.")]
        private bool overrideResponse;

        [SerializeField]
        [Tooltip("Film grain response value.")]
        private float response;

        [SerializeField]
        [Tooltip("Override the film grain texture while the clip is active.")]
        private bool overrideTexture;

        [SerializeField]
        [Tooltip("Film grain texture to apply when override is enabled.")]
        private Texture texture;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeFilmGrainAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeFilmGrainClipBlob>();
            blob.Type = VolumeFilmGrainClipType.Constant;
            blob.Constant = new VolumeFilmGrainConstantData
            {
                Active = this.active,
                TypeOverride = this.overrideType,
                Type = this.type,
                IntensityOverride = this.overrideIntensity,
                Intensity = this.intensity,
                ResponseOverride = this.overrideResponse,
                Response = this.response,
                TextureOverride = this.overrideTexture,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeFilmGrainClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(
                clipEntity,
                new VolumeFilmGrainClipData
                {
                    Value = blobRef,
                    Texture = this.texture,
                });
        }
    }
}
#endif
#endif
