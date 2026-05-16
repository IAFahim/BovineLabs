// <copyright file="CMBrainClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Cinemachine;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that overrides Cinemachine brain defaults.
    /// </summary>
    [Serializable]
    public class CMBrainClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("If true, the Cinemachine brain will continue updating while timeScale is zero.")]
        private bool ignoreTimeScale;

        [SerializeField]
        [Tooltip("Determines when the Cinemachine brain updates its virtual cameras.")]
        private CinemachineBrain.UpdateMethods updateMethod = CinemachineBrain.UpdateMethods.LateUpdate;

        [SerializeField]
        [Tooltip("Sets when blend weights are updated.")]
        private CinemachineBrain.BrainUpdateMethods blendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;

        [SerializeField]
        [Tooltip("Style applied to the default camera blend.")]
        private CinemachineBlendDefinition.Styles defaultBlendStyle = CinemachineBlendDefinition.Styles.EaseInOut;

        [SerializeField]
        [Tooltip("Duration of the default camera blend.")]
        [Min(0f)]
        private float defaultBlendTime = 1f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.defaultBlendTime = Mathf.Max(0f, this.defaultBlendTime);
            context.Baker.AddComponent<CMBrainAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<CMBrainClipBlob>();
            blob.Type = CMBrainClipType.Animated;
            blob.IgnoreTimeScale = this.ignoreTimeScale;
            blob.UpdateMethod = this.updateMethod;
            blob.BlendUpdateMethod = this.blendUpdateMethod;
            blob.DefaultBlendStyle = this.defaultBlendStyle;
            blob.DefaultBlendTime = this.defaultBlendTime;

            var blobRef = builder.CreateBlobAssetReference<CMBrainClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new CMBrainClipData { Value = blobRef });
        }

        private void OnValidate()
        {
            this.defaultBlendTime = Mathf.Max(0f, this.defaultBlendTime);
        }
    }
}
#endif
