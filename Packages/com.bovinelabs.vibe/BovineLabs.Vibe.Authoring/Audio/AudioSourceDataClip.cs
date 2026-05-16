// <copyright file="AudioSourceDataClip.cs" company="BovineLabs">
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
    /// Timeline clip that sets audio source volume and pitch.
    /// </summary>
    [Serializable]
    public class AudioSourceDataClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Volume applied while the clip is active.")]
        [Min(0f)]
        private float volume = 1f;

        [SerializeField]
        [Tooltip("Pitch applied while the clip is active.")]
        private float pitch = 1f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<AudioSourceDataAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioSourceDataClipBlob>();
            root.Type = AudioSourceDataClipType.Animated;
            root.Data = new AnimatedData
            {
                Volume = Mathf.Max(0f, this.volume),
                Pitch = this.pitch,
            };

            var blob = builder.CreateBlobAssetReference<AudioSourceDataClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioSourceDataClipData { Value = blob });
        }

        private void OnValidate()
        {
            this.volume = Mathf.Max(0f, this.volume);
        }
    }
}
#endif
