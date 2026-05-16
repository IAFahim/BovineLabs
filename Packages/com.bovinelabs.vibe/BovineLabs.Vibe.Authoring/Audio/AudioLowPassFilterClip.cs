// <copyright file="AudioLowPassFilterClip.cs" company="BovineLabs">
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
    /// Timeline clip that sets audio low-pass filter values.
    /// </summary>
    [Serializable]
    public class AudioLowPassFilterClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Cutoff frequency in hertz.")]
        [Min(0f)]
        private float cutoffFrequency = 5000f;

        [SerializeField]
        [Tooltip("Low pass resonance Q value.")]
        [Min(0f)]
        private float lowpassResonanceQ = 1f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<AudioLowPassFilterAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioLowPassFilterClipBlob>();
            root.Type = AudioLowPassFilterClipType.Animated;
            root.Data = new AudioLowPassFilterConstantData
            {
                CutoffFrequency = Mathf.Max(0f, this.cutoffFrequency),
                LowpassResonanceQ = Mathf.Max(0f, this.lowpassResonanceQ),
            };

            var blob = builder.CreateBlobAssetReference<AudioLowPassFilterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioLowPassFilterClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.cutoffFrequency = Mathf.Max(0f, this.cutoffFrequency);
            this.lowpassResonanceQ = Mathf.Max(0f, this.lowpassResonanceQ);
        }
    }
}
#endif
