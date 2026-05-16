// <copyright file="AudioHighPassFilterClip.cs" company="BovineLabs">
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
    /// Timeline clip that sets audio high-pass filter values.
    /// </summary>
    [Serializable]
    public class AudioHighPassFilterClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Cutoff frequency in hertz.")]
        [Min(0f)]
        private float cutoffFrequency = 500f;

        [SerializeField]
        [Tooltip("High pass resonance Q value.")]
        [Min(0f)]
        private float highpassResonanceQ = 1f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<AudioHighPassFilterAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioHighPassFilterClipBlob>();
            root.Type = AudioHighPassFilterClipType.Animated;
            root.Data = new AudioHighPassFilterConstantData
            {
                CutoffFrequency = Mathf.Max(0f, this.cutoffFrequency),
                HighpassResonanceQ = Mathf.Max(0f, this.highpassResonanceQ),
            };

            var blob = builder.CreateBlobAssetReference<AudioHighPassFilterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioHighPassFilterClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.cutoffFrequency = Mathf.Max(0f, this.cutoffFrequency);
            this.highpassResonanceQ = Mathf.Max(0f, this.highpassResonanceQ);
        }
    }
}
#endif
