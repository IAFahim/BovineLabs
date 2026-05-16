// <copyright file="AudioSourceTriggerClip.cs" company="BovineLabs">
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
    /// Timeline clip that triggers audio source playback actions with optional randomization.
    /// </summary>
    [Serializable]
    public class AudioSourceTriggerClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Playback action to apply when the clip activates.")]
        private AudioSourceTriggerAction action = AudioSourceTriggerAction.Play;

        [SerializeField]
        [Tooltip("Audio clips to randomly select from when triggering playback.")]
        private AudioClip[] clips = Array.Empty<AudioClip>();

        [SerializeField]
        [Tooltip("Minimum volume for randomized playback.")]
        [Min(0f)]
        private float minVolume = 1f;

        [SerializeField]
        [Tooltip("Maximum volume for randomized playback.")]
        [Min(0f)]
        private float maxVolume = 1f;

        [SerializeField]
        [Tooltip("Minimum pitch for randomized playback.")]
        private float minPitch = 1f;

        [SerializeField]
        [Tooltip("Maximum pitch for randomized playback.")]
        private float maxPitch = 1f;

        [SerializeField]
        [Tooltip("Seed used for deterministic randomization. If 0, a stable hash is used.")]
        private uint seed;

        [SerializeField]
        [Tooltip("Force restart even if the selected clip matches the current clip.")]
        private bool forceRestart;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.None;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<AudioSourceTriggerClipBlob>();
            blob.Action = this.action;
            blob.MinVolume = Mathf.Max(0f, this.minVolume);
            blob.MaxVolume = Mathf.Max(0f, this.maxVolume);
            blob.MinPitch = this.minPitch;
            blob.MaxPitch = this.maxPitch;
            blob.Seed = this.seed;
            blob.ForceRestart = this.forceRestart;

            var blobRef = builder.CreateBlobAssetReference<AudioSourceTriggerClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new AudioSourceTriggerClipData { Value = blobRef });

            var buffer = context.Baker.AddBuffer<AudioSourceTriggerClipEntry>(clipEntity);

            if (this.clips == null)
            {
                return;
            }

            foreach (var clip in this.clips)
            {
                if (clip == null)
                {
                    continue;
                }

                buffer.Add(new AudioSourceTriggerClipEntry { Clip = clip });
            }
        }

        private void OnValidate()
        {
            this.minVolume = Mathf.Max(0f, this.minVolume);
            this.maxVolume = Mathf.Max(0f, this.maxVolume);
        }
    }
}
#endif
