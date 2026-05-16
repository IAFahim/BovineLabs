// <copyright file="PhysicsImpulseAtPointClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS

namespace BovineLabs.Vibe.Authoring.Physics
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Physics;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Timeline clip that applies a linear impulse at a world-space point.
    /// </summary>
    [Serializable]
    public class PhysicsImpulseAtPointClip : PhysicsImpulseClipBase
    {
        [Tooltip("Linear impulse expressed in world space (Newton seconds).")]
        [SerializeField]
        private Vector3 impulse = Vector3.up;

        [Tooltip("World-space point where the impulse is applied.")]
        [SerializeField]
        private Vector3 point;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PhysicsImpulseClipBlob blob)
        {
            blob.Type = PhysicsImpulseClipType.ImpulseAtPoint;
            blob.Point = new PhysicsImpulseClipBlob.PointImpulseData
            {
                Impulse = this.impulse,
                Point = this.point,
            };
        }
    }
}

#endif
