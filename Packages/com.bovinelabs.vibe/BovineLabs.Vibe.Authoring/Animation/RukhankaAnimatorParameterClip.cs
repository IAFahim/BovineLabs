// <copyright file="RukhankaAnimatorParameterClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Authoring.Animation
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Animation;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that applies animator parameter changes when activated.
    /// </summary>
    [Serializable]
    public class RukhankaAnimatorParameterClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Whether to update a trigger parameter when the clip activates.")]
        private bool updateTrigger = true;

        [SerializeField]
        [Tooltip("Trigger parameter name.")]
        private string triggerParameter = string.Empty;

        [SerializeField]
        [Tooltip("Trigger mode applied when updating trigger parameters.")]
        private RukhankaAnimatorParameterTriggerMode triggerMode = RukhankaAnimatorParameterTriggerMode.Set;

        [SerializeField]
        [Tooltip("Whether to update a bool parameter when the clip activates.")]
        private bool updateBool;

        [SerializeField]
        [Tooltip("Bool parameter name.")]
        private string boolParameter = string.Empty;

        [SerializeField]
        [Tooltip("Whether to choose a random trigger from the list when the clip activates.")]
        private bool updateRandomTrigger;

        [SerializeField]
        [Tooltip("Whether to choose a random bool from the list when the clip activates.")]
        private bool updateRandomBool;

        [SerializeField]
        [Tooltip("Bool value to apply.")]
        private bool boolValue;

        [SerializeField]
        [Tooltip("Parameters used for random trigger/bool selection.")]
        private string[] randomParameters = Array.Empty<string>();

        [SerializeField]
        [Tooltip("Seed used for deterministic randomization. If 0, a stable hash is used.")]
        private uint seed;

        [SerializeField]
        [Tooltip("Int parameter name.")]
        private string intParameter = string.Empty;

        [SerializeField]
        [Tooltip("How to calculate the int value.")]
        private RukhankaAnimatorParameterValueMode intMode = RukhankaAnimatorParameterValueMode.Constant;

        [SerializeField]
        [Tooltip("Int value to apply when using Constant mode.")]
        private int intValue;

        [SerializeField]
        [Tooltip("Minimum int value for Random mode.")]
        private int intMin;

        [SerializeField]
        [Tooltip("Maximum int value for Random mode.")]
        private int intMax = 1;

        [SerializeField]
        [Tooltip("Increment to add in Increment mode.")]
        private int intIncrement = 1;

        [SerializeField]
        [Tooltip("Float parameter name.")]
        private string floatParameter = string.Empty;

        [SerializeField]
        [Tooltip("How to calculate the float value.")]
        private RukhankaAnimatorParameterValueMode floatMode = RukhankaAnimatorParameterValueMode.Constant;

        [SerializeField]
        [Tooltip("Float value to apply when using Constant mode.")]
        private float floatValue;

        [SerializeField]
        [Tooltip("Minimum float value for Random mode.")]
        private float floatMin;

        [SerializeField]
        [Tooltip("Maximum float value for Random mode.")]
        private float floatMax = 1f;

        [SerializeField]
        [Tooltip("Increment to add in Increment mode.")]
        private float floatIncrement = 1f;

        [SerializeField]
        [Tooltip("Whether to set an animator layer weight on activation.")]
        private bool setLayerWeight;

        [SerializeField]
        [Tooltip("Optional layer name for weight updates.")]
        private string layerName = string.Empty;

        [SerializeField]
        [Tooltip("Layer index for weight updates if no name is provided or resolved.")]
        private int layerIndex;

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
            var resolvedLayerIndex = this.setLayerWeight
                ? RukhankaAuthoringUtility.ResolveLayerIndex(animator, this.layerName, this.layerIndex)
                : this.layerIndex;

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<RukhankaAnimatorParameterClipBlob>();
            blob.TriggerHash = RukhankaAuthoringUtility.HashName(this.triggerParameter);
            blob.BoolHash = RukhankaAuthoringUtility.HashName(this.boolParameter);
            blob.IntHash = RukhankaAuthoringUtility.HashName(this.intParameter);
            blob.FloatHash = RukhankaAuthoringUtility.HashName(this.floatParameter);
            blob.TriggerMode = this.triggerMode;
            blob.IntMode = this.intMode;
            blob.FloatMode = this.floatMode;
            blob.UpdateTrigger = this.updateTrigger;
            blob.UpdateRandomTrigger = this.updateRandomTrigger;
            blob.UpdateBool = this.updateBool;
            blob.UpdateRandomBool = this.updateRandomBool;
            blob.SetLayerWeight = this.setLayerWeight;
            blob.BoolValue = this.boolValue;
            blob.IntValue = this.intValue;
            blob.IntMin = this.intMin;
            blob.IntMax = this.intMax;
            blob.IntIncrement = this.intIncrement;
            blob.FloatValue = this.floatValue;
            blob.FloatMin = this.floatMin;
            blob.FloatMax = this.floatMax;
            blob.FloatIncrement = this.floatIncrement;
            blob.LayerIndex = resolvedLayerIndex;
            blob.LayerWeight = this.layerWeight;
            blob.Seed = this.seed;

            var blobRef = builder.CreateBlobAssetReference<RukhankaAnimatorParameterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new RukhankaAnimatorParameterClipData { Value = blobRef });

            var randomHashes = context.Baker.AddBuffer<RukhankaAnimatorParameterRandomHash>(clipEntity);
            foreach (var name in this.randomParameters)
            {
                var hash = RukhankaAuthoringUtility.HashName(name);
                if (hash == 0)
                {
                    continue;
                }

                randomHashes.Add(new RukhankaAnimatorParameterRandomHash { Hash = hash });
            }
        }

        internal void AddTrackHashes(
            Animator animator,
            DynamicBuffer<RukhankaAnimatorParameterTrackHash> parameterHashes,
            DynamicBuffer<RukhankaAnimatorLayerIndex> layerIndices)
        {
            if (this.updateTrigger)
            {
                this.AddHash(parameterHashes, this.triggerParameter);
            }

            if (this.updateBool)
            {
                this.AddHash(parameterHashes, this.boolParameter);
            }

            this.AddHash(parameterHashes, this.intParameter);
            this.AddHash(parameterHashes, this.floatParameter);

            if (this.updateRandomTrigger || this.updateRandomBool)
            {
                foreach (var name in this.randomParameters)
                {
                    this.AddHash(parameterHashes, name);
                }
            }

            if (this.setLayerWeight)
            {
                var resolvedLayerIndex = RukhankaAuthoringUtility.ResolveLayerIndex(animator, this.layerName, this.layerIndex);
                layerIndices.Add(new RukhankaAnimatorLayerIndex { Value = resolvedLayerIndex });
            }
        }

        private void AddHash(DynamicBuffer<RukhankaAnimatorParameterTrackHash> parameterHashes, string name)
        {
            var hash = RukhankaAuthoringUtility.HashName(name);
            if (hash == 0)
            {
                return;
            }

            parameterHashes.Add(new RukhankaAnimatorParameterTrackHash { Hash = hash });
        }
    }
}

#endif
