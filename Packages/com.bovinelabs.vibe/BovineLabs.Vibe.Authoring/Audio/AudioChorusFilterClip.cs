// <copyright file="AudioChorusFilterClip.cs" company="BovineLabs">
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
    /// Timeline clip that sets audio chorus filter values.
    /// </summary>
    [Serializable]
    public class AudioChorusFilterClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Dry mix level.")]
        [Min(0f)]
        private float dryMix = 0.5f;

        [SerializeField]
        [Tooltip("Wet mix 1 level.")]
        [Min(0f)]
        private float wetMix1 = 0.5f;

        [SerializeField]
        [Tooltip("Wet mix 2 level.")]
        [Min(0f)]
        private float wetMix2 = 0.5f;

        [SerializeField]
        [Tooltip("Wet mix 3 level.")]
        [Min(0f)]
        private float wetMix3 = 0.5f;

        [SerializeField]
        [Tooltip("Delay in milliseconds.")]
        [Min(0f)]
        private float delay = 40f;

        [SerializeField]
        [Tooltip("Modulation rate in hertz.")]
        [Min(0f)]
        private float rate = 0.8f;

        [SerializeField]
        [Tooltip("Modulation depth.")]
        [Min(0f)]
        private float depth = 0.03f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<AudioChorusFilterAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioChorusFilterClipBlob>();
            root.Type = AudioChorusFilterClipType.Animated;
            root.Data = new AudioChorusFilterConstantData
            {
                DryMix = Mathf.Max(0f, this.dryMix),
                WetMix1 = Mathf.Max(0f, this.wetMix1),
                WetMix2 = Mathf.Max(0f, this.wetMix2),
                WetMix3 = Mathf.Max(0f, this.wetMix3),
                Delay = Mathf.Max(0f, this.delay),
                Rate = Mathf.Max(0f, this.rate),
                Depth = Mathf.Max(0f, this.depth),
            };

            var blob = builder.CreateBlobAssetReference<AudioChorusFilterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioChorusFilterClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.dryMix = Mathf.Max(0f, this.dryMix);
            this.wetMix1 = Mathf.Max(0f, this.wetMix1);
            this.wetMix2 = Mathf.Max(0f, this.wetMix2);
            this.wetMix3 = Mathf.Max(0f, this.wetMix3);
            this.delay = Mathf.Max(0f, this.delay);
            this.rate = Mathf.Max(0f, this.rate);
            this.depth = Mathf.Max(0f, this.depth);
        }
    }
}
#endif
