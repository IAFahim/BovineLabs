// <copyright file="RukhankaAnimatorPlayStateClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Authoring.Animation
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Animation;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that forces an animator state on activation.
    /// </summary>
    [Serializable]
    public class RukhankaAnimatorPlayStateClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("State name to force when the clip activates.")]
        private string stateName = string.Empty;

        [SerializeField]
        [Tooltip("How to interpret the play time.")]
        private RukhankaAnimatorPlayStateMode mode = RukhankaAnimatorPlayStateMode.NormalizedTime;

        [SerializeField]
        [Tooltip("Normalized play time (0-1).")]
        private float normalizedTime;

        [SerializeField]
        [Tooltip("Fixed play time in seconds.")]
        private float fixedTimeSeconds;

        [SerializeField]
        [Tooltip("Optional layer name for the state.")]
        private string layerName = string.Empty;

        [SerializeField]
        [Tooltip("Layer index used if no layer name is provided or resolved.")]
        private int layerIndex;

        [SerializeField]
        [Tooltip("Whether to set an animator layer weight on activation.")]
        private bool setLayerWeight;

        [SerializeField]
        [Tooltip("Optional layer name for weight updates.")]
        private string weightLayerName = string.Empty;

        [SerializeField]
        [Tooltip("Layer index for weight updates if no name is provided or resolved.")]
        private int weightLayerIndex;

        [SerializeField]
        [Tooltip("Layer weight to set.")]
        private float layerWeight = 1f;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.None;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            var animator = RukhankaAuthoringUtility.ResolveBindingAnimator(context);
            var resolvedLayerIndex = RukhankaAuthoringUtility.ResolveLayerIndex(animator, this.layerName, this.layerIndex);
            var resolvedWeightLayerIndex = this.setLayerWeight
                ? RukhankaAuthoringUtility.ResolveLayerIndex(animator, this.weightLayerName, this.weightLayerIndex)
                : this.weightLayerIndex;

            context.Baker.AddComponent(
                clipEntity,
                new RukhankaAnimatorStateClipData
                {
                    Type = RukhankaAnimatorStateClipType.PlayState,
                    PlayState = new RukhankaAnimatorStatePlayStateData
                    {
                        StateHash = RukhankaAuthoringUtility.HashName(this.stateName),
                        Mode = this.mode,
                        NormalizedTime = this.normalizedTime,
                        FixedTimeSeconds = this.fixedTimeSeconds,
                        LayerIndex = resolvedLayerIndex,
                        SetLayerWeight = this.setLayerWeight,
                        WeightLayerIndex = resolvedWeightLayerIndex,
                        LayerWeight = this.layerWeight,
                    },
                });
        }

        internal void AddTrackLayerUsage(Animator animator, DynamicBuffer<RukhankaAnimatorStateLayerUsage> layerUsages)
        {
            var resolvedLayerIndex = RukhankaAuthoringUtility.ResolveLayerIndex(animator, this.layerName, this.layerIndex);
            layerUsages.Add(new RukhankaAnimatorStateLayerUsage
            {
                LayerIndex = resolvedLayerIndex,
                RestoreState = true,
                RestoreWeight = false,
            });

            if (!this.setLayerWeight)
            {
                return;
            }

            var resolvedWeightLayerIndex = RukhankaAuthoringUtility.ResolveLayerIndex(animator, this.weightLayerName, this.weightLayerIndex);
            layerUsages.Add(new RukhankaAnimatorStateLayerUsage
            {
                LayerIndex = resolvedWeightLayerIndex,
                RestoreState = false,
                RestoreWeight = true,
            });
        }
    }
}

#endif
