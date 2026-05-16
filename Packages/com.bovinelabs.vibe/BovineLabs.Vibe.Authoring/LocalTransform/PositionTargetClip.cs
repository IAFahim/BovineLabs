// <copyright file="PositionTargetClip.cs" company="BovineLabs">
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
    using UnityEngine;

    /// <summary>
    /// Follows a configured target entity while applying an optional offset.
    /// </summary>
    [Serializable]
    public class PositionTargetClip : PositionClipBase
    {
        [Tooltip("Target binding to follow during playback.")]
        [SerializeField]
        private Target target = Target.Target;

        [Tooltip(Strings.FixedPosition)]
        [SerializeField]
        private bool fixedPosition;

        [Tooltip("Offset applied relative to the target transform.")]
        [SerializeField]
        private SpaceVector3 offset = SpaceVector3.Local(Vector3.zero);

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PositionClipBlob blob)
        {
            blob.Type = PositionType.Target;
            blob.TransformOnClipActivation = this.offset.UseClipActivation;
            blob.Target = new PositionClipBlob.TargetData
            {
                Target = this.target,
                FixedPosition = this.fixedPosition,
                Space = this.offset.Space,
                Offset = this.offset.Value,
            };
        }
    }
}
#endif
