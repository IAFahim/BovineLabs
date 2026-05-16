// <copyright file="PhysicsImpulseTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Vibe
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Physics;
    using BovineLabs.Vibe.Jobs;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Physics;
    using Unity.Physics.Extensions;
    using Unity.Transforms;

    /// <summary>
    /// Applies physics impulses authored on timeline clips when they become active.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct PhysicsImpulseTrackSystem : ISystem
    {
        private TrackLifeImpl<PhysicsVelocity, PhysicsImpulseInitial> lifeImpl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsImpulseClipData>();
            this.lifeImpl.OnCreate(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var physicsVelocities = SystemAPI.GetComponentLookup<PhysicsVelocity>();
            var physicsMasses = SystemAPI.GetComponentLookup<PhysicsMass>(true);
            var localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true);

            this.lifeImpl.OnUpdate(ref state);

            state.Dependency = new ApplyImpulseJob
            {
                PhysicsVelocities = physicsVelocities,
                PhysicsMasses = physicsMasses,
                LocalTransforms = localTransforms,
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct ApplyImpulseJob : IJobEntity
        {
            public ComponentLookup<PhysicsVelocity> PhysicsVelocities;

            [ReadOnly]
            public ComponentLookup<PhysicsMass> PhysicsMasses;

            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransforms;

            private void Execute(in PhysicsImpulseClipData clipData, in TrackBinding trackBinding)
            {
                if (!clipData.Value.IsCreated)
                {
                    return;
                }

                if (!this.PhysicsVelocities.TryGetRefRW(trackBinding.Value, out var velocity))
                {
                    return;
                }

                if (!this.PhysicsMasses.TryGetComponent(trackBinding.Value, out var mass))
                {
                    return;
                }

                ref var data = ref clipData.Value.Value;
                var requiresTransform = data.Type != PhysicsImpulseClipType.World;
                var transform = default(LocalTransform);
                if (requiresTransform && !this.LocalTransforms.TryGetComponent(trackBinding.Value, out transform))
                {
                    return;
                }

                switch (data.Type)
                {
                    case PhysicsImpulseClipType.World:
                        velocity.ValueRW.ApplyLinearImpulse(mass, data.World.Impulse);
                        break;
                    case PhysicsImpulseClipType.LocalAxis:
                        var direction = data.LocalAxis.Axis switch
                        {
                            PhysicsImpulseAxis.Forward => transform.Forward(),
                            PhysicsImpulseAxis.Up => transform.Up(),
                            PhysicsImpulseAxis.Right => transform.Right(),
                            _ => float3.zero,
                        };

                        var impulse = direction * data.LocalAxis.Magnitude;
                        velocity.ValueRW.ApplyLinearImpulse(mass, impulse);
                        break;
                    case PhysicsImpulseClipType.WorldTorque:
                        velocity.ValueRW.ApplyAngularImpulseWorldSpace(mass, transform.Position, transform.Rotation, data.WorldTorque.Torque);
                        break;
                    case PhysicsImpulseClipType.LocalTorque:
                        var torque = math.rotate(transform.Rotation, data.LocalTorque.Torque);
                        velocity.ValueRW.ApplyAngularImpulseWorldSpace(mass, transform.Position, transform.Rotation, torque);
                        break;
                    case PhysicsImpulseClipType.ImpulseAtPoint:
                        velocity.ValueRW.ApplyImpulse(mass, transform.Position, transform.Rotation, data.Point.Impulse, data.Point.Point);
                        break;
                    case PhysicsImpulseClipType.Radial:
                        ApplyRadialImpulse(ref velocity.ValueRW, in mass, in transform, ref data.Radial);
                        break;
                    default:
                        return;
                }
            }

            private static void ApplyRadialImpulse(
                ref PhysicsVelocity velocity, in PhysicsMass mass, in LocalTransform transform, ref PhysicsImpulseClipBlob.RadialImpulseData radial)
            {
                var center = mass.GetCenterOfMassWorldSpace(transform.Position, transform.Rotation);
                var offset = center - radial.Origin;

                if (math.lengthsq(radial.UpAxis) > math.FLT_MIN_NORMAL)
                {
                    // Project onto the plane to keep the impulse planar when an up axis is provided.
                    var up = math.normalizesafe(radial.UpAxis);
                    offset -= up * math.dot(offset, up);
                }

                var distance = math.length(offset);
                if (radial.Radius > math.FLT_MIN_NORMAL && distance > radial.Radius)
                {
                    return;
                }

                var direction = math.normalizesafe(offset);
                if (math.lengthsq(direction) <= math.FLT_MIN_NORMAL)
                {
                    return;
                }

                var falloff = 1f;
                if (radial.FalloffCurve.IsCreated)
                {
                    var normalizedDistance = radial.Radius > math.FLT_MIN_NORMAL ? math.saturate(distance / radial.Radius) : 0f;
                    var cache = BlobCurveCache.Empty;
                    falloff = radial.FalloffCurve.Evaluate(normalizedDistance, ref cache);
                }

                var impulse = direction * (radial.Strength * falloff);
                velocity.ApplyLinearImpulse(mass, impulse);
            }
        }
    }
}

#endif
