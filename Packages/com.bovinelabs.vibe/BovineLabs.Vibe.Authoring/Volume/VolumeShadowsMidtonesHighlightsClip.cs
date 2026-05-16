// <copyright file="VolumeShadowsMidtonesHighlightsClip.cs" company="BovineLabs">
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
    /// Timeline clip that applies constant shadows/midtones/highlights overrides.
    /// </summary>
    [Serializable]
    public class VolumeShadowsMidtonesHighlightsClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the shadows/midtones/highlights override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override shadows while the clip is active.")]
        private bool overrideShadows = true;

        [SerializeField]
        [Tooltip("Shadows value.")]
        private Vector4 shadows = new Vector4(1f, 1f, 1f, 0f);

        [SerializeField]
        [Tooltip("Override midtones while the clip is active.")]
        private bool overrideMidtones = true;

        [SerializeField]
        [Tooltip("Midtones value.")]
        private Vector4 midtones = new Vector4(1f, 1f, 1f, 0f);

        [SerializeField]
        [Tooltip("Override highlights while the clip is active.")]
        private bool overrideHighlights = true;

        [SerializeField]
        [Tooltip("Highlights value.")]
        private Vector4 highlights = new Vector4(1f, 1f, 1f, 0f);

        [SerializeField]
        [Tooltip("Override shadows start while the clip is active.")]
        private bool overrideShadowsStart;

        [SerializeField]
        [Tooltip("Shadows start value.")]
        private float shadowsStart;

        [SerializeField]
        [Tooltip("Override shadows end while the clip is active.")]
        private bool overrideShadowsEnd;

        [SerializeField]
        [Tooltip("Shadows end value.")]
        private float shadowsEnd = 0.3f;

        [SerializeField]
        [Tooltip("Override highlights start while the clip is active.")]
        private bool overrideHighlightsStart;

        [SerializeField]
        [Tooltip("Highlights start value.")]
        private float highlightsStart = 0.55f;

        [SerializeField]
        [Tooltip("Override highlights end while the clip is active.")]
        private bool overrideHighlightsEnd;

        [SerializeField]
        [Tooltip("Highlights end value.")]
        private float highlightsEnd = 1f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeShadowsMidtonesHighlightsAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeShadowsMidtonesHighlightsClipBlob>();
            blob.Type = VolumeShadowsMidtonesHighlightsClipType.Constant;
            blob.Constant = new VolumeShadowsMidtonesHighlightsConstantData
            {
                Active = this.active,
                ShadowsOverride = this.overrideShadows,
                Shadows = new float4(this.shadows.x, this.shadows.y, this.shadows.z, this.shadows.w),
                MidtonesOverride = this.overrideMidtones,
                Midtones = new float4(this.midtones.x, this.midtones.y, this.midtones.z, this.midtones.w),
                HighlightsOverride = this.overrideHighlights,
                Highlights = new float4(this.highlights.x, this.highlights.y, this.highlights.z, this.highlights.w),
                ShadowsStartOverride = this.overrideShadowsStart,
                ShadowsStart = this.shadowsStart,
                ShadowsEndOverride = this.overrideShadowsEnd,
                ShadowsEnd = this.shadowsEnd,
                HighlightsStartOverride = this.overrideHighlightsStart,
                HighlightsStart = this.highlightsStart,
                HighlightsEndOverride = this.overrideHighlightsEnd,
                HighlightsEnd = this.highlightsEnd,
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeShadowsMidtonesHighlightsClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeShadowsMidtonesHighlightsClipData { Value = blobRef });
        }
    }
}
#endif
#endif
