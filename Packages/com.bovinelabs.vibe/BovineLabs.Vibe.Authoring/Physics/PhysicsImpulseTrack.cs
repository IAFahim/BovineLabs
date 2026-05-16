// <copyright file="PhysicsImpulseTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS

namespace BovineLabs.Vibe.Authoring.Physics
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Physics;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that binds physics impulse clips to a DOTS physics body.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(PhysicsImpulseClip))]
    [TrackClipType(typeof(PhysicsDirectionalImpulseClip))]
    [TrackClipType(typeof(PhysicsTorqueImpulseClip))]
    [TrackClipType(typeof(PhysicsImpulseAtPointClip))]
    [TrackClipType(typeof(PhysicsRadialImpulseClip))]
    [TrackBindingType(typeof(Rigidbody))]
    [TrackColor(0.1f, 0.35f, 0.55f)]
    [DisplayName("DOTS/Physics/Impulse Track")]
    public class PhysicsImpulseTrack : DOTSTrack
    {
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<PhysicsImpulseInitial>(context.TrackEntity);
        }
    }
}

#endif
