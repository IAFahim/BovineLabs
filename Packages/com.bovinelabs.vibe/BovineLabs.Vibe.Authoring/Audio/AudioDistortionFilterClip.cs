// <copyright file="AudioDistortionFilterClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Audio
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Audio;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that sets audio distortion filter values.
    /// </summary>
    [Serializable]
    public class AudioDistortionFilterClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Distortion amount (0-1).")]
        [Range(0f, 1f)]
        private float distortionLevel = 0.5f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<AudioDistortionFilterAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioDistortionFilterClipBlob>();
            root.Type = AudioDistortionFilterClipType.Animated;
            root.Data = new AudioDistortionFilterConstantData
            {
                DistortionLevel = Mathf.Clamp01(this.distortionLevel),
            };

            var blob = builder.CreateBlobAssetReference<AudioDistortionFilterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioDistortionFilterClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.distortionLevel = Mathf.Clamp01(this.distortionLevel);
        }
    }
}
#endif
