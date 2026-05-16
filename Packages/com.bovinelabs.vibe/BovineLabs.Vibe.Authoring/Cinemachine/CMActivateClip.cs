// <copyright file="CMActivateClip.cs" company="BovineLabs">
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
    /// Timeline clip that activates a Cinemachine camera without blending.
    /// </summary>
    [Serializable]
    public class CMActivateClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("When enabled the clip toggles the Cinemachine camera Enabled flag.")]
        private bool setEnabled = true;

        [SerializeField]
        [Tooltip("Desired enabled state when Set Enabled is checked.")]
        private bool enabled = true;

        [SerializeField]
        [Tooltip("When enabled the clip overrides the Cinemachine camera priority.")]
        private bool setPriority;

        [SerializeField]
        [Tooltip("Priority settings applied when Set Priority is checked.")]
        private PrioritySettings priority = new() { Value = 10 };

        [SerializeField]
        [Tooltip("When enabled the clip overrides the Cinemachine camera output channel.")]
        private bool setOutputChannel;

        [SerializeField]
        [Tooltip("Output channel applied when Set Output Channel is checked.")]
        private OutputChannels outputChannel = OutputChannels.Default;

        [SerializeField]
        [Tooltip("When enabled the clip overrides the Cinemachine camera blend hint.")]
        private bool setBlendHint;

        [SerializeField]
        [Tooltip("Blend hint applied when Set Blend Hint is checked.")]
        private CinemachineCore.BlendHints blendHint = default;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.None;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<CMCameraActivateClipBlob>();
            blob.SetEnabled = this.setEnabled;
            blob.Enabled = this.enabled;
            blob.SetPriority = this.setPriority;
            blob.Priority = this.priority;
            blob.SetOutputChannel = this.setOutputChannel;
            blob.OutputChannel = this.outputChannel;
            blob.SetBlendHint = this.setBlendHint;
            blob.BlendHint = this.blendHint;

            var blobRef = builder.CreateBlobAssetReference<CMCameraActivateClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new CMCameraActivateClipData { Value = blobRef });
        }
    }
}
#endif
