// <copyright file="CMFreeLookModifierClip.cs" company="BovineLabs">
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
    /// Timeline clip that drives Cinemachine free look modifier easing.
    /// </summary>
    [Serializable]
    public class CMFreeLookModifierClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("The amount of easing to apply as the modifier value crosses the center rig.")]
        private float easing;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.Sanitize();
            context.Baker.AddComponent<CMFreeLookModifierAnimated>(clipEntity);
            context.Baker.AddComponent(
                clipEntity,
                new CMFreeLookModifierClipData
                {
                    Type = CMFreeLookModifierClipType.Animated,
                    Easing = this.easing,
                });
        }

        private void OnValidate()
        {
            this.Sanitize();
        }

        private void Sanitize()
        {
            this.easing = Mathf.Clamp01(this.easing);
        }
    }
}
#endif
