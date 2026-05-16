// <copyright file="VolumeColorLookupClip.cs" company="BovineLabs">
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
    /// Timeline clip that applies constant color lookup overrides.
    /// </summary>
    [Serializable]
    public class VolumeColorLookupClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the color lookup override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override the lookup texture while the clip is active.")]
        private bool overrideTexture = true;

        [SerializeField]
        [Tooltip("Lookup texture applied when override is enabled.")]
        private Texture texture;

        [SerializeField]
        [Tooltip("Override the contribution while the clip is active.")]
        private bool overrideContribution = true;

        [SerializeField]
        [Tooltip("Contribution value.")]
        private float contribution = 1f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeColorLookupAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeColorLookupClipBlob>();
            blob.Type = VolumeColorLookupClipType.Constant;
            blob.Constant = new VolumeColorLookupConstantData
            {
                Active = this.active,
                TextureOverride = this.overrideTexture,
                ContributionOverride = this.overrideContribution,
                Contribution = this.contribution,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeColorLookupClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(
                clipEntity,
                new VolumeColorLookupClipData
                {
                    Value = blobRef,
                    Texture = this.texture,
                });
        }
    }
}
#endif
#endif
