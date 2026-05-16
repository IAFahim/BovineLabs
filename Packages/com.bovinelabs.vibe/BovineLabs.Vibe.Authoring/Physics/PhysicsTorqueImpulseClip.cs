// <copyright file="PhysicsTorqueImpulseClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS

namespace BovineLabs.Vibe.Authoring.Physics
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using BovineLabs.Vibe.Data.Physics;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Timeline clip that applies an angular impulse to the bound physics body.
    /// </summary>
    [Serializable]
    public class PhysicsTorqueImpulseClip : PhysicsImpulseClipBase
    {
        [Tooltip("Angular impulse applied in the selected space (Newton meters seconds).")]
        [SerializeField]
        private Vector3 torque = Vector3.up;

        [Tooltip("Space used to interpret the torque vector.")]
        [SerializeField]
        private TransformSpace space = TransformSpace.World;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PhysicsImpulseClipBlob blob)
        {
            if (this.space == TransformSpace.Local)
            {
                blob.Type = PhysicsImpulseClipType.LocalTorque;
                blob.LocalTorque = new PhysicsImpulseClipBlob.LocalTorqueImpulseData
                {
                    Torque = this.torque,
                };
            }
            else
            {
                blob.Type = PhysicsImpulseClipType.WorldTorque;
                blob.WorldTorque = new PhysicsImpulseClipBlob.WorldTorqueImpulseData
                {
                    Torque = this.torque,
                };
            }
        }
    }
}

#endif
