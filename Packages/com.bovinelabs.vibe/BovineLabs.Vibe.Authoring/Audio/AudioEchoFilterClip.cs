// <copyright file="AudioEchoFilterClip.cs" company="BovineLabs">
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
    /// Timeline clip that sets audio echo filter values.
    /// </summary>
    [Serializable]
    public class AudioEchoFilterClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Echo delay in milliseconds.")]
        [Min(0f)]
        private float delay = 500f;

        [SerializeField]
        [Tooltip("Echo decay ratio.")]
        [Min(0f)]
        private float decayRatio = 0.5f;

        [SerializeField]
        [Tooltip("Wet mix level.")]
        [Min(0f)]
        private float wetMix = 1f;

        [SerializeField]
        [Tooltip("Dry mix level.")]
        [Min(0f)]
        private float dryMix = 1f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<AudioEchoFilterAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioEchoFilterClipBlob>();
            root.Type = AudioEchoFilterClipType.Animated;
            root.Data = new AudioEchoFilterConstantData
            {
                Delay = Mathf.Max(0f, this.delay),
                DecayRatio = Mathf.Max(0f, this.decayRatio),
                WetMix = Mathf.Max(0f, this.wetMix),
                DryMix = Mathf.Max(0f, this.dryMix),
            };

            var blob = builder.CreateBlobAssetReference<AudioEchoFilterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioEchoFilterClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.delay = Mathf.Max(0f, this.delay);
            this.decayRatio = Mathf.Max(0f, this.decayRatio);
            this.wetMix = Mathf.Max(0f, this.wetMix);
            this.dryMix = Mathf.Max(0f, this.dryMix);
        }
    }
}
#endif
