// <copyright file="VolumeLiftGammaGainClip.cs" company="BovineLabs">
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
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that applies constant lift/gamma/gain overrides.
    /// </summary>
    [Serializable]
    public class VolumeLiftGammaGainClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether the lift/gamma/gain override is active.")]
        private bool active = true;

        [SerializeField]
        [Tooltip("Override lift while the clip is active.")]
        private bool overrideLift = true;

        [SerializeField]
        [Tooltip("Lift value.")]
        private Vector4 lift = Vector4.zero;

        [SerializeField]
        [Tooltip("Override gamma while the clip is active.")]
        private bool overrideGamma = true;

        [SerializeField]
        [Tooltip("Gamma value.")]
        private Vector4 gamma = Vector4.zero;

        [SerializeField]
        [Tooltip("Override gain while the clip is active.")]
        private bool overrideGain = true;

        [SerializeField]
        [Tooltip("Gain value.")]
        private Vector4 gain = Vector4.zero;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeLiftGammaGainAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeLiftGammaGainClipBlob>();
            blob.Type = VolumeLiftGammaGainClipType.Constant;
            blob.Constant = new VolumeLiftGammaGainConstantData
            {
                Active = this.active,
                LiftOverride = this.overrideLift,
                Lift = new float4(this.lift.x, this.lift.y, this.lift.z, this.lift.w),
                GammaOverride = this.overrideGamma,
                Gamma = new float4(this.gamma.x, this.gamma.y, this.gamma.z, this.gamma.w),
                GainOverride = this.overrideGain,
                Gain = new float4(this.gain.x, this.gain.y, this.gain.z, this.gain.w),
            };

            var blobRef = builder.CreateBlobAssetReference<VolumeLiftGammaGainClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeLiftGammaGainClipData { Value = blobRef });
        }
    }
}
#endif
#endif
