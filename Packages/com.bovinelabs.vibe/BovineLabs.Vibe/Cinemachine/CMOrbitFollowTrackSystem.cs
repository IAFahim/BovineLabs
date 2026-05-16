// <copyright file="CMOrbitFollowTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Core.Jobs;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Cinemachine;
    using BovineLabs.Vibe.Jobs;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Cinemachine;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Blends Cinemachine orbit follow timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMOrbitFollowTrackSystem : ISystem
    {
        private TrackLifeImpl<CMOrbitFollow, CMOrbitFollowInitial> lifeImpl;
        private TrackBlendImpl<CMOrbitFollowBlend, CMOrbitFollowAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMOrbitFollowClipData>();
            this.lifeImpl.OnCreate(ref state);
            this.impl.OnCreate(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            this.impl.OnDestroy(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.lifeImpl.OnUpdate(ref state);

            var clipActivateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<CMOrbitFollowAnimated>()
                .WithAll<CMOrbitFollowClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMOrbitFollowAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMOrbitFollowClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                OrbitFollows = SystemAPI.GetComponentLookup<CMOrbitFollow>(),
                Initials = SystemAPI.GetComponentLookup<CMOrbitFollowInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                OrbitFollows = SystemAPI.GetComponentLookup<CMOrbitFollow>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static CMOrbitFollowBlend CreateBlend(in CMOrbitFollow follow)
        {
            var horizontalAxis = follow.HorizontalAxis;
            horizontalAxis.Restrictions &=
                ~(InputAxis.RestrictionFlags.NoRecentering | InputAxis.RestrictionFlags.RangeIsDriven);
            CinemachineAxisUtility.SanitizeAxis(ref horizontalAxis);

            var verticalAxis = follow.VerticalAxis;
            CinemachineAxisUtility.SanitizeAxis(ref verticalAxis);

            var radialAxis = follow.RadialAxis;
            CinemachineAxisUtility.ClampRadialRangeMin(ref radialAxis);
            CinemachineAxisUtility.SanitizeAxis(ref radialAxis);

            return new CMOrbitFollowBlend
            {
                TargetOffset = follow.TargetOffset,
                Radius = math.max(follow.Radius, 0f),
                PositionDamping = math.max(follow.TrackerSettings.PositionDamping, float3.zero),
                RotationDamping = math.max(follow.TrackerSettings.RotationDamping, float3.zero),
                QuaternionDamping = math.max(follow.TrackerSettings.QuaternionDamping, 0f),
                Orbits = SanitizeOrbits(follow.Orbits),
                HorizontalAxisValue = horizontalAxis.Value,
                VerticalAxisValue = verticalAxis.Value,
                RadialAxisValue = radialAxis.Value,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMOrbitFollowAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMOrbitFollowClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMOrbitFollow> OrbitFollows;

            [ReadOnly]
            public ComponentLookup<CMOrbitFollowInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMOrbitFollowAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMOrbitFollowClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var trackBindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clipData = ref clipDatas[entityIndexInChunk];
                    ref readonly var trackBinding = ref trackBindings[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        CMOrbitFollowClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMOrbitFollowClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, clipData),
                        _ => animated.Value,
                    };
                }
            }

            private CMOrbitFollowBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return CreateBlend(initial.Value);
            }

            private CMOrbitFollowBlend SelectAnimatedValue(Entity boundEntity, in CMOrbitFollowClipData data)
            {
                var horizontalAxis = data.HorizontalAxis;
                horizontalAxis.Restrictions &=
                    ~(InputAxis.RestrictionFlags.NoRecentering | InputAxis.RestrictionFlags.RangeIsDriven);
                CinemachineAxisUtility.SanitizeAxis(ref horizontalAxis);

                var verticalAxis = data.VerticalAxis;
                CinemachineAxisUtility.SanitizeAxis(ref verticalAxis);

                var radialAxis = data.RadialAxis;
                CinemachineAxisUtility.ClampRadialRangeMin(ref radialAxis);
                CinemachineAxisUtility.SanitizeAxis(ref radialAxis);

                if (this.OrbitFollows.TryGetRefRW(boundEntity, out var orbitFollow))
                {
                    ref var value = ref orbitFollow.ValueRW;
                    var tracker = value.TrackerSettings;
                    tracker.BindingMode = data.TrackerSettings.BindingMode;
                    tracker.AngularDampingMode = data.TrackerSettings.AngularDampingMode;
                    value.TrackerSettings = tracker;

                    value.OrbitStyle = data.OrbitStyle;
                    value.RecenteringTarget = data.RecenteringTarget;
                    value.HorizontalAxis = horizontalAxis;
                    value.VerticalAxis = verticalAxis;
                    value.RadialAxis = radialAxis;
                }

                return new CMOrbitFollowBlend
                {
                    TargetOffset = data.TargetOffset,
                    Radius = math.max(data.Radius, 0f),
                    PositionDamping = math.max(data.TrackerSettings.PositionDamping, float3.zero),
                    RotationDamping = math.max(data.TrackerSettings.RotationDamping, float3.zero),
                    QuaternionDamping = math.max(data.TrackerSettings.QuaternionDamping, 0f),
                    Orbits = SanitizeOrbits(data.Orbits),
                    HorizontalAxisValue = horizontalAxis.Value,
                    VerticalAxisValue = verticalAxis.Value,
                    RadialAxisValue = radialAxis.Value,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMOrbitFollowBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMOrbitFollow> OrbitFollows;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.OrbitFollows.TryGetRefRW(entity, out var orbitFollow))
                {
                    return;
                }

                ref var value = ref orbitFollow.ValueRW;
                var current = CreateBlend(value);
                var blended = JobHelpers.Blend(ref mixData, current, default(CMOrbitFollowMixer));
                ApplyBlend(ref value, in blended);
                Sanitize(ref value);
            }

            private static void ApplyBlend(ref CMOrbitFollow follow, in CMOrbitFollowBlend blend)
            {
                follow.TargetOffset = blend.TargetOffset;
                follow.Radius = blend.Radius;
                follow.Orbits = blend.Orbits;

                var tracker = follow.TrackerSettings;
                tracker.PositionDamping = blend.PositionDamping;
                tracker.RotationDamping = blend.RotationDamping;
                tracker.QuaternionDamping = blend.QuaternionDamping;
                follow.TrackerSettings = tracker;

                var horizontalAxis = follow.HorizontalAxis;
                horizontalAxis.Value = blend.HorizontalAxisValue;
                follow.HorizontalAxis = horizontalAxis;

                var verticalAxis = follow.VerticalAxis;
                verticalAxis.Value = blend.VerticalAxisValue;
                follow.VerticalAxis = verticalAxis;

                var radialAxis = follow.RadialAxis;
                radialAxis.Value = blend.RadialAxisValue;
                follow.RadialAxis = radialAxis;
            }

            private static void Sanitize(ref CMOrbitFollow follow)
            {
                follow.Radius = math.max(follow.Radius, 0f);

                var tracker = follow.TrackerSettings;
                tracker.PositionDamping = math.max(tracker.PositionDamping, float3.zero);
                tracker.RotationDamping = math.max(tracker.RotationDamping, float3.zero);
                tracker.QuaternionDamping = math.max(tracker.QuaternionDamping, 0f);
                follow.TrackerSettings = tracker;

                follow.Orbits = SanitizeOrbits(follow.Orbits);

                var horizontalAxis = follow.HorizontalAxis;
                horizontalAxis.Restrictions &=
                    ~(InputAxis.RestrictionFlags.NoRecentering | InputAxis.RestrictionFlags.RangeIsDriven);
                CinemachineAxisUtility.SanitizeAxis(ref horizontalAxis);
                follow.HorizontalAxis = horizontalAxis;

                var verticalAxis = follow.VerticalAxis;
                CinemachineAxisUtility.SanitizeAxis(ref verticalAxis);
                follow.VerticalAxis = verticalAxis;

                var radialAxis = follow.RadialAxis;
                CinemachineAxisUtility.ClampRadialRangeMin(ref radialAxis);
                CinemachineAxisUtility.SanitizeAxis(ref radialAxis);
                follow.RadialAxis = radialAxis;
            }
        }

        private static Cinemachine3OrbitRig.Settings SanitizeOrbits(Cinemachine3OrbitRig.Settings settings)
        {
            settings.SplineCurvature = math.clamp(settings.SplineCurvature, 0f, 1f);
            settings.Top.Radius = math.max(settings.Top.Radius, 0f);
            settings.Center.Radius = math.max(settings.Center.Radius, 0f);
            settings.Bottom.Radius = math.max(settings.Bottom.Radius, 0f);
            return settings;
        }

        private static Cinemachine3OrbitRig.Orbit LerpOrbit(in Cinemachine3OrbitRig.Orbit a, in Cinemachine3OrbitRig.Orbit b, in float t)
        {
            return new Cinemachine3OrbitRig.Orbit
            {
                Radius = math.lerp(a.Radius, b.Radius, t),
                Height = math.lerp(a.Height, b.Height, t),
            };
        }

        private static Cinemachine3OrbitRig.Settings LerpOrbits(
            in Cinemachine3OrbitRig.Settings a, in Cinemachine3OrbitRig.Settings b, in float t)
        {
            return new Cinemachine3OrbitRig.Settings
            {
                Top = LerpOrbit(a.Top, b.Top, t),
                Center = LerpOrbit(a.Center, b.Center, t),
                Bottom = LerpOrbit(a.Bottom, b.Bottom, t),
                SplineCurvature = math.lerp(a.SplineCurvature, b.SplineCurvature, t),
            };
        }

        private static Cinemachine3OrbitRig.Orbit AddOrbit(in Cinemachine3OrbitRig.Orbit a, in Cinemachine3OrbitRig.Orbit b)
        {
            return new Cinemachine3OrbitRig.Orbit
            {
                Radius = a.Radius + b.Radius,
                Height = a.Height + b.Height,
            };
        }

        private static Cinemachine3OrbitRig.Settings AddOrbits(in Cinemachine3OrbitRig.Settings a, in Cinemachine3OrbitRig.Settings b)
        {
            return new Cinemachine3OrbitRig.Settings
            {
                Top = AddOrbit(a.Top, b.Top),
                Center = AddOrbit(a.Center, b.Center),
                Bottom = AddOrbit(a.Bottom, b.Bottom),
                SplineCurvature = a.SplineCurvature + b.SplineCurvature,
            };
        }

        private struct CMOrbitFollowMixer : IMixer<CMOrbitFollowBlend>
        {
            public CMOrbitFollowBlend Lerp(in CMOrbitFollowBlend a, in CMOrbitFollowBlend b, in float s)
            {
                return new CMOrbitFollowBlend
                {
                    TargetOffset = math.lerp(a.TargetOffset, b.TargetOffset, s),
                    Radius = math.lerp(a.Radius, b.Radius, s),
                    PositionDamping = math.lerp(a.PositionDamping, b.PositionDamping, s),
                    RotationDamping = math.lerp(a.RotationDamping, b.RotationDamping, s),
                    QuaternionDamping = math.lerp(a.QuaternionDamping, b.QuaternionDamping, s),
                    Orbits = LerpOrbits(a.Orbits, b.Orbits, s),
                    HorizontalAxisValue = math.lerp(a.HorizontalAxisValue, b.HorizontalAxisValue, s),
                    VerticalAxisValue = math.lerp(a.VerticalAxisValue, b.VerticalAxisValue, s),
                    RadialAxisValue = math.lerp(a.RadialAxisValue, b.RadialAxisValue, s),
                };
            }

            public CMOrbitFollowBlend Add(in CMOrbitFollowBlend a, in CMOrbitFollowBlend b)
            {
                return new CMOrbitFollowBlend
                {
                    TargetOffset = a.TargetOffset + b.TargetOffset,
                    Radius = a.Radius + b.Radius,
                    PositionDamping = a.PositionDamping + b.PositionDamping,
                    RotationDamping = a.RotationDamping + b.RotationDamping,
                    QuaternionDamping = a.QuaternionDamping + b.QuaternionDamping,
                    Orbits = AddOrbits(a.Orbits, b.Orbits),
                    HorizontalAxisValue = a.HorizontalAxisValue + b.HorizontalAxisValue,
                    VerticalAxisValue = a.VerticalAxisValue + b.VerticalAxisValue,
                    RadialAxisValue = a.RadialAxisValue + b.RadialAxisValue,
                };
            }
        }
    }
}
#endif
