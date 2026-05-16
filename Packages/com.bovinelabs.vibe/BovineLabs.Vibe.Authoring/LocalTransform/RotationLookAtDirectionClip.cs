// <copyright file="RotationLookAtDirectionClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Rotates the binding so it faces a constant direction.
    /// </summary>
    [Serializable]
    public class RotationLookAtDirectionClip : RotationClipBase
    {
        [Tooltip("Direction vector expressed in the chosen transform space.")]
        [SerializeField]
        private SpaceVector3 direction = SpaceVector3.World(Vector3.forward);

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref RotationClipBlob blob)
        {
            blob.Type = RotationType.LookInDirection;
            blob.TransformOnClipActivation = this.direction.UseClipActivation;
            blob.LookInDirection = new RotationClipBlob.LookInDirectionData
            {
                Space = this.direction.Space,
                Direction = this.direction.Value,
            };
        }
    }
}
