// <copyright file="RotationLookAtRotationClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Applies a fixed rotation relative to the chosen transform space.
    /// </summary>
    [Serializable]
    public class RotationLookAtRotationClip : RotationClipBase
    {
        [Tooltip("Euler rotation expressed in the selected transform space.")]
        [SerializeField]
        private SpaceVector3 rotation = SpaceVector3.World(Vector3.zero);

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref RotationClipBlob blob)
        {
            blob.Type = RotationType.LookAtRotation;
            blob.TransformOnClipActivation = this.rotation.UseClipActivation;
            blob.LookAtRotation = new RotationClipBlob.LookAtRotationData
            {
                Space = this.rotation.Space,
                Rotation = quaternion.Euler(math.radians(this.rotation.Value)),
            };
        }
    }
}
