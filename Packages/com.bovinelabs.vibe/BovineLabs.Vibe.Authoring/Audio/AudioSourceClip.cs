// <copyright file="AudioSourceClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Audio
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Audio;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that swaps the audio clip on an audio source.
    /// </summary>
    [Serializable]
    public class AudioSourceClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Audio clip assigned while the clip is active.")]
        private AudioClip clip;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.None;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent(clipEntity, new AudioSourceClipData { Clip = this.clip });
        }
    }
}
#endif
