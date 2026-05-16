// <copyright file="RukhankaAnimatorCrossfadeClip.cs" company="BovineLabs">
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
    /// Timeline clip that crossfades to an animator state on activation.
    /// </summary>
    [Serializable]
    public class RukhankaAnimatorCrossfadeClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("State name to crossfade to when the clip activates.")]
        private string stateName = string.Empty;

        [SerializeField]
        [Tooltip("Whether to select a random state from the list when the clip activates.")]
        private bool useRandomState;

        [SerializeField]
        [Tooltip("State names used for random selection.")]
        private string[] randomStates = Array.Empty<string>();

        [SerializeField]
        [Tooltip("Seed used for deterministic randomization. If 0, a stable hash is used.")]
        private uint seed;

        [SerializeField]
        [Tooltip("How to interpret transition durations.")]
        private RukhankaAnimatorCrossfadeMode mode = RukhankaAnimatorCrossfadeMode.Normalized;

        [SerializeField]
        [Tooltip("Transition duration as a normalized fraction of the current state length.")]
        private float normalizedTransitionDuration = 0.25f;

        [SerializeField]
        [Tooltip("Normalized time offset applied to the destination state.")]
        private float normalizedTimeOffset;

        [SerializeField]
        [Tooltip("Normalized transition start time.")]
        private float normalizedTransitionTime;

        [SerializeField]
        [Tooltip("Transition duration in seconds when using Fixed Time mode.")]
        private float transitionDuration = 0.25f;

        [SerializeField]
        [Tooltip("Time offset in seconds when using Fixed Time mode.")]
        private float timeOffset;

        [SerializeField]
        [Tooltip("Optional layer name for the crossfade.")]
        private string layerName = string.Empty;

        [SerializeField]
        [Tooltip("Layer index used if no layer name is provided or resolved.")]
        private int layerIndex;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.None;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            var animator = RukhankaAuthoringUtility.ResolveBindingAnimator(context);
            var resolvedLayerIndex = RukhankaAuthoringUtility.ResolveLayerIndex(animator, this.layerName, this.layerIndex);

            context.Baker.AddComponent(
                clipEntity,
                new RukhankaAnimatorStateClipData
                {
                    Type = RukhankaAnimatorStateClipType.Crossfade,
                    Crossfade = new RukhankaAnimatorStateCrossfadeData
                    {
                        StateHash = RukhankaAuthoringUtility.HashName(this.stateName),
                        Mode = this.mode,
                        TransitionDuration = this.transitionDuration,
                        TimeOffset = this.timeOffset,
                        NormalizedTransitionDuration = this.normalizedTransitionDuration,
                        NormalizedTimeOffset = this.normalizedTimeOffset,
                        NormalizedTransitionTime = this.normalizedTransitionTime,
                        LayerIndex = resolvedLayerIndex,
                        Seed = this.seed,
                        UseRandomState = this.useRandomState,
                    },
                });

            var randomHashes = context.Baker.AddBuffer<RukhankaAnimatorStateRandomHash>(clipEntity);
            if (this.randomStates == null || this.randomStates.Length == 0)
            {
                return;
            }

            foreach (var name in this.randomStates)
            {
                var hash = RukhankaAuthoringUtility.HashName(name);
                if (hash == 0)
                {
                    continue;
                }

                randomHashes.Add(new RukhankaAnimatorStateRandomHash { Hash = hash });
            }
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
        }
    }
}

#endif
