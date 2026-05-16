// <copyright file="AudioReverbFilterClip.cs" company="BovineLabs">
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
    /// Timeline clip that sets audio reverb filter values.
    /// </summary>
    [Serializable]
    public class AudioReverbFilterClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Override the reverb preset while the clip is active.")]
        private bool overrideReverbPreset = true;

        [SerializeField]
        [Tooltip("Reverb preset to apply when override is enabled.")]
        private AudioReverbPreset reverbPreset = AudioReverbPreset.User;

        [SerializeField]
        [Tooltip("Dry signal level in millibels.")]
        private float dryLevel;

        [SerializeField]
        [Tooltip("Room effect level in millibels.")]
        private float room;

        [SerializeField]
        [Tooltip("High-frequency room effect level in millibels.")]
        private float roomHF;

        [SerializeField]
        [Tooltip("Low-frequency room effect level in millibels.")]
        private float roomLF;

        [SerializeField]
        [Tooltip("Reverb decay time in seconds.")]
        private float decayTime = 1f;

        [SerializeField]
        [Tooltip("Ratio of high-frequency decay time to the overall decay time.")]
        private float decayHFRatio = 0.5f;

        [SerializeField]
        [Tooltip("Reflections level in millibels.")]
        private float reflectionsLevel;

        [SerializeField]
        [Tooltip("Reflections delay in seconds.")]
        private float reflectionsDelay;

        [SerializeField]
        [Tooltip("Reverb level in millibels.")]
        private float reverbLevel;

        [SerializeField]
        [Tooltip("Reverb delay in seconds.")]
        private float reverbDelay;

        [SerializeField]
        [Tooltip("Reference frequency for high frequencies.")]
        private float hfReference = 5000f;

        [SerializeField]
        [Tooltip("Reference frequency for low frequencies.")]
        private float lfReference = 250f;

        [SerializeField]
        [Tooltip("Diffusion value for the reverb tail.")]
        private float diffusion = 100f;

        [SerializeField]
        [Tooltip("Density value for the reverb tail.")]
        private float density = 100f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<AudioReverbFilterAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioReverbFilterClipBlob>();
            root.Type = AudioReverbFilterClipType.Animated;
            root.Data = new AudioReverbFilterConstantData
            {
                OverrideReverbPreset = this.overrideReverbPreset,
                ReverbPreset = this.reverbPreset,
                DryLevel = this.dryLevel,
                Room = this.room,
                RoomHF = this.roomHF,
                RoomLF = this.roomLF,
                DecayTime = this.decayTime,
                DecayHFRatio = this.decayHFRatio,
                ReflectionsLevel = this.reflectionsLevel,
                ReflectionsDelay = this.reflectionsDelay,
                ReverbLevel = this.reverbLevel,
                ReverbDelay = this.reverbDelay,
                HFReference = this.hfReference,
                LFReference = this.lfReference,
                Diffusion = this.diffusion,
                Density = this.density,
            };

            var blob = builder.CreateBlobAssetReference<AudioReverbFilterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioReverbFilterClipData { Value = blob });
        }
    }
}
#endif
