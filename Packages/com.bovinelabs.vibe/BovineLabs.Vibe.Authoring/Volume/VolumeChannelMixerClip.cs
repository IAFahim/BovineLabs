// <copyright file="VolumeChannelMixerClip.cs" company="BovineLabs">
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
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that applies constant channel mixer overrides.
    /// </summary>
    [Serializable]
    public class VolumeChannelMixerClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the channel mixer override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override red-out/red-in while the clip is active.")]
        private bool overrideRedOutRedIn = true;

        [SerializeField]
        [Tooltip("Red-out/red-in value.")]
        private float redOutRedIn = 1f;

        [SerializeField]
        [Tooltip("Override red-out/green-in while the clip is active.")]
        private bool overrideRedOutGreenIn;

        [SerializeField]
        [Tooltip("Red-out/green-in value.")]
        private float redOutGreenIn;

        [SerializeField]
        [Tooltip("Override red-out/blue-in while the clip is active.")]
        private bool overrideRedOutBlueIn;

        [SerializeField]
        [Tooltip("Red-out/blue-in value.")]
        private float redOutBlueIn;

        [SerializeField]
        [Tooltip("Override green-out/red-in while the clip is active.")]
        private bool overrideGreenOutRedIn;

        [SerializeField]
        [Tooltip("Green-out/red-in value.")]
        private float greenOutRedIn;

        [SerializeField]
        [Tooltip("Override green-out/green-in while the clip is active.")]
        private bool overrideGreenOutGreenIn = true;

        [SerializeField]
        [Tooltip("Green-out/green-in value.")]
        private float greenOutGreenIn = 1f;

        [SerializeField]
        [Tooltip("Override green-out/blue-in while the clip is active.")]
        private bool overrideGreenOutBlueIn;

        [SerializeField]
        [Tooltip("Green-out/blue-in value.")]
        private float greenOutBlueIn;

        [SerializeField]
        [Tooltip("Override blue-out/red-in while the clip is active.")]
        private bool overrideBlueOutRedIn;

        [SerializeField]
        [Tooltip("Blue-out/red-in value.")]
        private float blueOutRedIn;

        [SerializeField]
        [Tooltip("Override blue-out/green-in while the clip is active.")]
        private bool overrideBlueOutGreenIn;

        [SerializeField]
        [Tooltip("Blue-out/green-in value.")]
        private float blueOutGreenIn;

        [SerializeField]
        [Tooltip("Override blue-out/blue-in while the clip is active.")]
        private bool overrideBlueOutBlueIn = true;

        [SerializeField]
        [Tooltip("Blue-out/blue-in value.")]
        private float blueOutBlueIn = 1f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeChannelMixerAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeChannelMixerClipBlob>();
            blob.Type = VolumeChannelMixerClipType.Constant;

            ref var data = ref blob.Constant;
            data.Active = this.active;
            data.RedOutRedInOverride = this.overrideRedOutRedIn;
            data.RedOutRedIn = this.redOutRedIn;
            data.RedOutGreenInOverride = this.overrideRedOutGreenIn;
            data.RedOutGreenIn = this.redOutGreenIn;
            data.RedOutBlueInOverride = this.overrideRedOutBlueIn;
            data.RedOutBlueIn = this.redOutBlueIn;
            data.GreenOutRedInOverride = this.overrideGreenOutRedIn;
            data.GreenOutRedIn = this.greenOutRedIn;
            data.GreenOutGreenInOverride = this.overrideGreenOutGreenIn;
            data.GreenOutGreenIn = this.greenOutGreenIn;
            data.GreenOutBlueInOverride = this.overrideGreenOutBlueIn;
            data.GreenOutBlueIn = this.greenOutBlueIn;
            data.BlueOutRedInOverride = this.overrideBlueOutRedIn;
            data.BlueOutRedIn = this.blueOutRedIn;
            data.BlueOutGreenInOverride = this.overrideBlueOutGreenIn;
            data.BlueOutGreenIn = this.blueOutGreenIn;
            data.BlueOutBlueInOverride = this.overrideBlueOutBlueIn;
            data.BlueOutBlueIn = this.blueOutBlueIn;

            var blobRef = builder.CreateBlobAssetReference<VolumeChannelMixerClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeChannelMixerClipData { Value = blobRef });
        }
    }
}
#endif
#endif
