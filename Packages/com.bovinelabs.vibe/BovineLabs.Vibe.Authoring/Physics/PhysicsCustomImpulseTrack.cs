// <copyright file="PhysicsCustomImpulseTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS && UNITY_PHYSICS_CUSTOM

namespace BovineLabs.Vibe.Authoring.Physics
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Physics;
    using Unity.Entities;
    using Unity.Physics.Authoring;
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
    [TrackBindingType(typeof(PhysicsBodyAuthoring))]
    [TrackColor(0.1f, 0.35f, 0.55f)]
    [DisplayName("DOTS/Physics/Custom Impulse Track")]
    public class PhysicsCustomImpulseTrack : DOTSTrack
    {
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<PhysicsImpulseInitial>(context.TrackEntity);
        }
    }
}

#endif
