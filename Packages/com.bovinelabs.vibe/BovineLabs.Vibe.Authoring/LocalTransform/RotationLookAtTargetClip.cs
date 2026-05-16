// <copyright file="RotationLookAtTargetClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_REACTION
namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Rotates the binding so it faces a configured target entity.
    /// </summary>
    [Serializable]
    public class RotationLookAtTargetClip : RotationClipBase
    {
        [Tooltip("Target binding the transform should look at.")]
        [SerializeField]
        private Target target;

        [Tooltip(Strings.FixedRotation)]
        [SerializeField]
        private bool fixedRotation;

        [Tooltip("Offset applied after orienting towards the target.")]
        [SerializeField]
        private SpaceVector3 offset = SpaceVector3.Local(Vector3.zero);

        [Tooltip("Anchor position evaluated in the selected transform space when no target binding is provided.")]
        [SerializeField]
        private SpaceVector3 anchorPoint = SpaceVector3.Local(Vector3.forward);

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref RotationClipBlob blob)
        {
            blob.Type = RotationType.LookAtTarget;
            var useAnchor = this.target == Target.None;
            blob.TransformOnClipActivation = this.offset.UseClipActivation || (useAnchor && this.anchorPoint.UseClipActivation);

            blob.LookAtTarget = new RotationClipBlob.LookAtTargetData
            {
                Target = this.target,
                FixedRotation = this.fixedRotation,
                Space = this.offset.Space,
                Offset = quaternion.Euler(math.radians(this.offset.Value)),
                AnchorSpace = this.anchorPoint.Space,
                AnchorPosition = this.anchorPoint.Value,
            };
        }
    }
}
#endif
