// <copyright file="CMRotateWithFollowTargetClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that drives Cinemachine rotate-with-follow-target damping.
    /// </summary>
    [Serializable]
    public class CMRotateWithFollowTargetClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("How long it takes for the aim to match the follow target's rotation.")]
        [Min(0f)]
        private float damping = 0f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.damping = Mathf.Max(0f, this.damping);
            context.Baker.AddComponent<CMRotateWithFollowTargetAnimated>(clipEntity);

            context.Baker.AddComponent(
                clipEntity,
                new CMRotateWithFollowTargetClipData
                {
                    Type = CMRotateWithFollowTargetClipType.Animated,
                    Damping = this.damping,
                });
        }

        private void OnValidate()
        {
            this.damping = Mathf.Max(0f, this.damping);
        }
    }
}
#endif
