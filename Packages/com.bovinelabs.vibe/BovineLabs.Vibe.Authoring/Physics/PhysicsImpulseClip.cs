// <copyright file="PhysicsImpulseClip.cs" company="BovineLabs">
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
    /// Timeline clip that applies a linear impulse to the bound physics body when it becomes active.
    /// </summary>
    [Serializable]
    public class PhysicsImpulseClip : PhysicsImpulseClipBase
    {
        [Tooltip("Linear impulse expressed in world space (Newton seconds).")]
        [SerializeField]
        private Vector3 linearImpulse = Vector3.up;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PhysicsImpulseClipBlob blob)
        {
            blob.Type = PhysicsImpulseClipType.World;
            blob.World = new PhysicsImpulseClipBlob.WorldImpulseData
            {
                Impulse = this.linearImpulse,
            };
        }
    }
}

#endif
