// <copyright file="PositionWorldClip.cs" company="BovineLabs">
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
    /// Drives the bound transform to an absolute world-space position.
    /// </summary>
    [Serializable]
    public class PositionWorldClip : PositionClipBase
    {
        [Tooltip("World-space coordinates applied while the clip is active.")]
        [SerializeField]
        private Vector3 position;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PositionClipBlob blob)
        {
            blob.Type = PositionType.World;
            blob.TransformOnClipActivation = false;
            blob.World.Position = new float3(this.position.x, this.position.y, this.position.z);
        }
    }
}
