// <copyright file="PhysicsRadialImpulseClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS

namespace BovineLabs.Vibe.Authoring.Physics
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Physics;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Timeline clip that applies a radial impulse around an origin point.
    /// </summary>
    [Serializable]
    public class PhysicsRadialImpulseClip : PhysicsImpulseClipBase
    {
        [Tooltip("World-space origin of the radial impulse.")]
        [SerializeField]
        private Vector3 origin;

        [Min(0f)]
        [Tooltip("Radius of the radial impulse. Zero or less applies the impulse everywhere.")]
        [SerializeField]
        private float radius = 5f;

        [Tooltip("Impulse strength at the origin (Newton seconds).")]
        [SerializeField]
        private float strength = 10f;

        [Tooltip("Optional up axis used to keep the impulse planar.")]
        [SerializeField]
        private Vector3 upAxis = Vector3.zero;

        [Tooltip("Enable a falloff curve to scale the impulse by normalized distance.")]
        [SerializeField]
        private bool useFalloffCurve = true;

        [Tooltip("Curve evaluated over normalized distance (0 at origin, 1 at radius).")]
        [SerializeField]
        private AnimationCurve falloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PhysicsImpulseClipBlob blob)
        {
            blob.Type = PhysicsImpulseClipType.Radial;
            blob.Radial = new PhysicsImpulseClipBlob.RadialImpulseData
            {
                Origin = this.origin,
                Radius = math.max(0f, this.radius),
                Strength = this.strength,
                UpAxis = math.normalizesafe(this.upAxis),
            };

            if (this.useFalloffCurve && this.falloffCurve != null && this.falloffCurve.length > 0)
            {
                BlobCurve.Construct(ref builder, ref blob.Radial.FalloffCurve, this.falloffCurve);
            }
        }
    }
}

#endif
