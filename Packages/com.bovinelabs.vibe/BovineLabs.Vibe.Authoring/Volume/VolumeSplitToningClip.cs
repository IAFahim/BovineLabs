// <copyright file="VolumeSplitToningClip.cs" company="BovineLabs">
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
    /// Timeline clip that applies constant split toning overrides.
    /// </summary>
    [Serializable]
    public class VolumeSplitToningClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the split toning override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override shadows while the clip is active.")]
        private bool overrideShadows = true;

        [SerializeField]
        [Tooltip("Shadows color.")]
        private Color shadows = Color.gray;

        [SerializeField]
        [Tooltip("Override highlights while the clip is active.")]
        private bool overrideHighlights = true;

        [SerializeField]
        [Tooltip("Highlights color.")]
        private Color highlights = Color.gray;

        [SerializeField]
        [Tooltip("Override balance while the clip is active.")]
        private bool overrideBalance = true;

        [SerializeField]
        [Tooltip("Balance value.")]
        private float balance;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeSplitToningAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeSplitToningClipBlob>();
            blob.Type = VolumeSplitToningClipType.Constant;
            blob.Constant = new VolumeSplitToningConstantData
            {
                Active = this.active,
                ShadowsOverride = this.overrideShadows,
                Shadows = new float4(this.shadows.r, this.shadows.g, this.shadows.b, this.shadows.a),
                HighlightsOverride = this.overrideHighlights,
                Highlights = new float4(this.highlights.r, this.highlights.g, this.highlights.b, this.highlights.a),
                BalanceOverride = this.overrideBalance,
                Balance = this.balance,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeSplitToningClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeSplitToningClipData { Value = blobRef });
        }
    }
}
#endif
#endif
