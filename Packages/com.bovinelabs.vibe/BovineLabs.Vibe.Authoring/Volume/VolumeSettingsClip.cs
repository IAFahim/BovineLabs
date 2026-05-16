// <copyright file="VolumeSettingsClip.cs" company="BovineLabs">
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
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that applies constant volume settings.
    /// </summary>
    [Serializable]
    public class VolumeSettingsClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Weight applied to the volume profile.")]
        [Range(0f, 1f)]
        private float weight = 1f;

        [SerializeField]
        [Tooltip("Override the volume priority while the clip is active.")]
        private bool overridePriority;

        [SerializeField]
        [Tooltip("Priority value applied when override is enabled.")]
        private float priority;

        [SerializeField]
        [Tooltip("Override the volume blend distance while the clip is active.")]
        private bool overrideBlendDistance;

        [SerializeField]
        [Tooltip("Blend distance value applied when override is enabled.")]
        private float blendDistance;

        [SerializeField]
        [Tooltip("Override the global flag while the clip is active.")]
        private bool overrideIsGlobal;

        [SerializeField]
        [Tooltip("Whether the volume is treated as global.")]
        private bool isGlobal = true;

        [SerializeField]
        [Tooltip("Override the volume profile while the clip is active.")]
        private bool overrideProfile;

        [SerializeField]
        [Tooltip("Volume profile to apply when override is enabled.")]
        private VolumeProfile profile;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);
            context.Baker.AddComponent<VolumeSettingsAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeSettingsClipBlob>();
            blob.Type = VolumeSettingsClipType.Constant;
            blob.Weight = Mathf.Clamp01(this.weight);
            blob.OverridePriority = this.overridePriority;
            blob.Priority = this.priority;
            blob.OverrideBlendDistance = this.overrideBlendDistance;
            blob.BlendDistance = this.blendDistance;
            blob.OverrideIsGlobal = this.overrideIsGlobal;
            blob.IsGlobal = this.isGlobal;
            blob.OverrideProfile = this.overrideProfile;

            var blobRef = builder.CreateBlobAssetReference<VolumeSettingsClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(
                clipEntity,
                new VolumeSettingsClipData
                {
                    Value = blobRef,
                    Profile = this.overrideProfile ? this.profile : default,
                });
        }

        private void OnValidate()
        {
            this.weight = Mathf.Clamp01(this.weight);
        }
    }
}
#endif
#endif
