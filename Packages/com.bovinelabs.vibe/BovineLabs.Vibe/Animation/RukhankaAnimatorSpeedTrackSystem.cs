// <copyright file="RukhankaAnimatorSpeedTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Animation
{
    using BovineLabs.Core.Jobs;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.Animation;
    using Rukhanka;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Blends animator speed clips and applies the result to Rukhanka animator layers.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct RukhankaAnimatorSpeedTrackSystem : ISystem
    {
        private TrackBlendImpl<float, RukhankaAnimatorSpeedAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RukhankaAnimatorSpeedClipData>();
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
            state.Dependency = new TrackDeactivateJob
            {
                Layers = SystemAPI.GetBufferLookup<AnimatorControllerLayerComponent>(),
            }.Schedule(state.Dependency);

            state.Dependency = new TrackActivateJob
            {
                Layers = SystemAPI.GetBufferLookup<AnimatorControllerLayerComponent>(true),
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new ClipActivateJob
            {
                TrackInitials = SystemAPI.GetBufferLookup<RukhankaAnimatorSpeedInitial>(true),
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new ClipUpdateJob
            {
                TrackInitials = SystemAPI.GetBufferLookup<RukhankaAnimatorSpeedInitial>(true),
            }.ScheduleParallel(state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Layers = SystemAPI.GetBufferLookup<AnimatorControllerLayerComponent>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static float ResolveBaseSpeed(Entity trackEntity, in BufferLookup<RukhankaAnimatorSpeedInitial> trackInitials)
        {
            if (!trackInitials.TryGetBuffer(trackEntity, out var initials) || initials.Length == 0)
            {
                return 1f;
            }

            for (var i = 0; i < initials.Length; i++)
            {
                if (initials[i].LayerIndex == 0)
                {
                    return initials[i].Speed;
                }
            }

            return initials[0].Speed;
        }

        private static float ApplyRelative(float value, float baseSpeed, bool relative)
        {
            return relative ? baseSpeed + value : value;
        }

        private static float SelectRandomSpeed(ref RukhankaAnimatorSpeedClipBlob clipData, ref Random random)
        {
            var min = math.min(clipData.MinSpeed, clipData.MaxSpeed);
            var max = math.max(clipData.MinSpeed, clipData.MaxSpeed);

            if (math.abs(max - min) <= math.FLT_MIN_NORMAL)
            {
                return min;
            }

            return random.NextFloat(min, max);
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))]
        [WithAll(typeof(TimelineActivePrevious))]
        [WithDisabled(typeof(TimelineActive))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            public BufferLookup<AnimatorControllerLayerComponent> Layers;

            private void Execute(in DynamicBuffer<RukhankaAnimatorSpeedInitial> initials, in TrackBinding trackBinding)
            {
                if (trackBinding.Value == Entity.Null)
                {
                    return;
                }

                if (!this.Layers.TryGetBuffer(trackBinding.Value, out var layers))
                {
                    return;
                }

                for (var i = 0; i < initials.Length; i++)
                {
                    var entry = initials[i];
                    if (entry.LayerIndex < 0 || entry.LayerIndex >= layers.Length)
                    {
                        continue;
                    }

                    var layer = layers[entry.LayerIndex];
                    layer.speed = entry.Speed;
                    layers[entry.LayerIndex] = layer;
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        private partial struct TrackActivateJob : IJobEntity
        {
            [ReadOnly]
            public BufferLookup<AnimatorControllerLayerComponent> Layers;

            private void Execute(DynamicBuffer<RukhankaAnimatorSpeedInitial> initials, in TrackBinding trackBinding)
            {
                initials.Clear();

                if (trackBinding.Value == Entity.Null)
                {
                    return;
                }

                if (!this.Layers.TryGetBuffer(trackBinding.Value, out var layers))
                {
                    return;
                }

                for (var i = 0; i < layers.Length; i++)
                {
                    var layer = layers[i];
                    initials.Add(new RukhankaAnimatorSpeedInitial
                    {
                        LayerIndex = i,
                        Speed = layer.speed,
                    });
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct ClipActivateJob : IJobEntity
        {
            [ReadOnly]
            public BufferLookup<RukhankaAnimatorSpeedInitial> TrackInitials;

            private void Execute(
                Entity entity,
                ref RukhankaAnimatorSpeedAnimated animated,
                in RukhankaAnimatorSpeedClipData clipComponent,
                in Clip clip,
                in TrackBinding trackBinding)
            {
                var baseSpeed = ResolveBaseSpeed(clip.Track, this.TrackInitials);
                ref var clipData = ref clipComponent.Value.Value;

                switch (clipData.Mode)
                {
                    case RukhankaAnimatorSpeedMode.Random:
                    {
                        var seed = clipData.Seed != 0
                            ? clipData.Seed
                            : math.hash(new uint2((uint)entity.Index, (uint)trackBinding.Value.Index));

                        if (seed == 0)
                        {
                            seed = 1u;
                        }

                        var random = Random.CreateFromIndex(seed);
                        var value = SelectRandomSpeed(ref clipData, ref random);
                        animated.Value = ApplyRelative(value, baseSpeed, clipData.Relative);
                        break;
                    }

                    case RukhankaAnimatorSpeedMode.Constant:
                        animated.Value = ApplyRelative(clipData.MinSpeed, baseSpeed, clipData.Relative);
                        break;
                    case RukhankaAnimatorSpeedMode.Curve:
                        animated.Value = baseSpeed;
                        break;
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        private partial struct ClipUpdateJob : IJobEntity
        {
            [ReadOnly]
            public BufferLookup<RukhankaAnimatorSpeedInitial> TrackInitials;

            private void Execute(
                ref RukhankaAnimatorSpeedAnimated animated,
                in RukhankaAnimatorSpeedClipData clipComponent,
                in Clip clip,
                in LocalTime localTime,
                ref ClipBlobCurveCache clipCurveCache)
            {
                ref var clipData = ref clipComponent.Value.Value;
                if (clipData.Mode != RukhankaAnimatorSpeedMode.Curve || !clipData.Curve.IsCreated)
                {
                    return;
                }

                var baseSpeed = ResolveBaseSpeed(clip.Track, this.TrackInitials);
                ref var curve = ref clipData.Curve;

                animated.Value = CurveSweepUtility.Evaluate(
                    ref curve,
                    clipData.MinSpeed,
                    clipData.MaxSpeed,
                    clipData.Relative,
                    (float)localTime.Value,
                    ref clipCurveCache,
                    baseSpeed);
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<float>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public BufferLookup<AnimatorControllerLayerComponent> Layers;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Layers.TryGetBuffer(entity, out var layers))
                {
                    return;
                }

                for (var i = 0; i < layers.Length; i++)
                {
                    ref var layer = ref layers.ElementAt(i);
                    var mix = mixData;
                    layer.speed = JobHelpers.Blend<float, FloatMixer>(ref mix, layer.speed);
                }
            }
        }
    }
}

#endif
