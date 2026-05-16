// <copyright file="CMHardLockToTargetClip.cs" company="BovineLabs">
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
    /// Timeline clip that drives Cinemachine hard-lock-to-target damping.
    /// </summary>
    [Serializable]
    public class CMHardLockToTargetClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("How long it takes for the position to catch up to the follow target.")]
        [Min(0f)]
        private float damping = 0f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.damping = Mathf.Max(0f, this.damping);
            context.Baker.AddComponent<CMHardLockToTargetAnimated>(clipEntity);

            context.Baker.AddComponent(
                clipEntity,
                new CMHardLockToTargetClipData
                {
                    Type = CMHardLockToTargetClipType.Animated,
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
