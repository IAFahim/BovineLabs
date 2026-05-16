// <copyright file="PhysicsDirectionalImpulseClip.cs" company="BovineLabs">
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
    /// Timeline clip that applies an impulse along one of the binding's local axes.
    /// </summary>
    [Serializable]
    public class PhysicsDirectionalImpulseClip : PhysicsImpulseClipBase
    {
        [Tooltip("Magnitude of the impulse to apply along the chosen axis (Newton seconds).")]
        [SerializeField]
        private float magnitude = 10f;

        [Tooltip("Local axis used to build the impulse direction.")]
        [SerializeField]
        private PhysicsImpulseAxis axis = PhysicsImpulseAxis.Forward;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PhysicsImpulseClipBlob blob)
        {
            blob.Type = PhysicsImpulseClipType.LocalAxis;
            blob.LocalAxis = new PhysicsImpulseClipBlob.LocalAxisImpulseData
            {
                Axis = this.axis,
                Magnitude = this.magnitude,
            };
        }
    }
}

#endif
