// <copyright file="RukhankaAnimatorStateTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Animation
{
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Animation;
    using Rukhanka;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Applies Rukhanka animator state changes for timeline clips.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct RukhankaAnimatorStateTrackSystem : ISystem
    {
        private const int CrossfadeTransitionId = 0xffffff;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RukhankaAnimatorStateClipData>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hasBlobDatabase = SystemAPI.TryGetSingleton<BlobDatabaseSingleton>(out var blobDatabase);

            state.Dependency = new TrackDeactivateJob
            {
                Layers = SystemAPI.GetBufferLookup<AnimatorControllerLayerComponent>(),
            }.Schedule(state.Dependency);

            state.Dependency = new TrackActivateJob
            {
                Layers = SystemAPI.GetBufferLookup<AnimatorControllerLayerComponent>(true),
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new CrossfadeClipActivateJob
            {
                Layers = SystemAPI.GetBufferLookup<AnimatorControllerLayerComponent>(),
                Parameters = SystemAPI.GetBufferLookup<AnimatorControllerParameterComponent>(true),
                BlobDatabase = blobDatabase,
                HasBlobDatabase = hasBlobDatabase,
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new PlayStateClipActivateJob
            {
                Layers = SystemAPI.GetBufferLookup<AnimatorControllerLayerComponent>(),
                Parameters = SystemAPI.GetBufferLookup<AnimatorControllerParameterComponent>(true),
                BlobDatabase = blobDatabase,
                HasBlobDatabase = hasBlobDatabase,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))]
        [WithAll(typeof(TimelineActivePrevious))]
        [WithDisabled(typeof(TimelineActive))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            public BufferLookup<AnimatorControllerLayerComponent> Layers;

            private void Execute(in DynamicBuffer<RukhankaAnimatorStateLayerInitial> initials, in TrackBinding trackBinding)
            {
                if (trackBinding.Value == Entity.Null)
                {
                    return;
                }

                if (!this.Layers.TryGetBuffer(trackBinding.Value, out var layers))
                {
                    return;
                }

                foreach (var entry in initials)
                {
                    if (entry.LayerIndex < 0 || entry.LayerIndex >= layers.Length)
                    {
                        continue;
                    }

                    var layer = layers[entry.LayerIndex];
                    if (entry.RestoreState)
                    {
                        layer.rtd = entry.RuntimeData;
                    }

                    if (entry.RestoreWeight)
                    {
                        layer.weight = entry.Weight;
                    }

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

            private void Execute(
                DynamicBuffer<RukhankaAnimatorStateLayerInitial> initials, in DynamicBuffer<RukhankaAnimatorStateLayerUsage> layerUsages,
                in TrackBinding trackBinding)
            {
                initials.Clear();

                if (layerUsages.Length == 0 || trackBinding.Value == Entity.Null)
                {
                    return;
                }

                if (!this.Layers.TryGetBuffer(trackBinding.Value, out var layers))
                {
                    return;
                }

                foreach (var usage in layerUsages)
                {
                    if (usage.LayerIndex < 0 || usage.LayerIndex >= layers.Length)
                    {
                        continue;
                    }

                    var existingIndex = FindInitialIndex(initials, usage.LayerIndex);
                    if (existingIndex >= 0)
                    {
                        ref var existing = ref initials.ElementAt(existingIndex);
                        existing.RestoreState |= usage.RestoreState;
                        existing.RestoreWeight |= usage.RestoreWeight;
                        continue;
                    }

                    var layer = layers[usage.LayerIndex];
                    initials.Add(new RukhankaAnimatorStateLayerInitial
                    {
                        LayerIndex = usage.LayerIndex,
                        RestoreState = usage.RestoreState,
                        RestoreWeight = usage.RestoreWeight,
                        Weight = layer.weight,
                        RuntimeData = layer.rtd,
                    });
                }
            }

            private static int FindInitialIndex(in DynamicBuffer<RukhankaAnimatorStateLayerInitial> initials, int layerIndex)
            {
                for (var i = 0; i < initials.Length; i++)
                {
                    if (initials[i].LayerIndex == layerIndex)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct CrossfadeClipActivateJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public BufferLookup<AnimatorControllerLayerComponent> Layers;

            [ReadOnly]
            public BufferLookup<AnimatorControllerParameterComponent> Parameters;

            [ReadOnly]
            [NativeDisableContainerSafetyRestriction]
            public BlobDatabaseSingleton BlobDatabase;

            public bool HasBlobDatabase;

            private void Execute(
                Entity entity, in RukhankaAnimatorStateClipData clipData, in TrackBinding trackBinding,
                in DynamicBuffer<RukhankaAnimatorStateRandomHash> randomHashes)
            {
                if (clipData.Type != RukhankaAnimatorStateClipType.Crossfade)
                {
                    return;
                }

                var data = clipData.Crossfade;

                if (trackBinding.Value == Entity.Null)
                {
                    return;
                }

                if (!this.Layers.TryGetBuffer(trackBinding.Value, out var layers))
                {
                    return;
                }

                if (data.LayerIndex < 0 || data.LayerIndex >= layers.Length)
                {
                    return;
                }

                var stateHash = ResolveStateHash(entity, data, trackBinding, randomHashes);
                if (stateHash == 0)
                {
                    return;
                }

                var layer = layers[data.LayerIndex];
                var stateIndex = RukhankaAnimatorStateUtility.GetStateIndex(layer.controller, data.LayerIndex, stateHash);
                if (stateIndex < 0)
                {
                    return;
                }

                if (data.Mode == RukhankaAnimatorCrossfadeMode.Normalized)
                {
                    ScriptedAnimator.CrossFade(ref layer, stateIndex, math.abs(data.NormalizedTransitionDuration), data.NormalizedTimeOffset,
                        data.NormalizedTransitionTime);

                    layers[data.LayerIndex] = layer;
                    return;
                }

                var parameters = this.Parameters.TryGetBuffer(trackBinding.Value, out var parameterBuffer) ? parameterBuffer.AsNativeArray() : default;

                var stateDuration = this.HasBlobDatabase
                    ? RukhankaAnimatorStateUtility.GetStateDurationSeconds(data.LayerIndex, stateIndex, layer.controller, layer.animations,
                        layers.AsNativeArray(), parameters, this.BlobDatabase)
                    : 1f;

                var normalizedOffset = stateDuration > math.FLT_MIN_NORMAL ? data.TimeOffset / stateDuration : 0f;

                var transition = layer.rtd.MakeDefaultTransition();
                transition.id = CrossfadeTransitionId;
                transition.length = math.abs(data.TransitionDuration);
                transition.normalizedDuration = data.NormalizedTransitionTime;
                layer.rtd.activeTransition = transition;

                var dstState = layer.rtd.MakeDefaultState();
                dstState.id = stateIndex;
                dstState.normalizedDuration = normalizedOffset;
                layer.rtd.dstState = dstState;

                layers[data.LayerIndex] = layer;
            }

            private static uint ResolveStateHash(
                Entity entity, in RukhankaAnimatorStateCrossfadeData clipData, in TrackBinding trackBinding,
                in DynamicBuffer<RukhankaAnimatorStateRandomHash> randomHashes)
            {
                if (!clipData.UseRandomState || randomHashes.Length == 0)
                {
                    return clipData.StateHash;
                }

                var seed = clipData.Seed != 0 ? clipData.Seed : math.hash(new uint2((uint)entity.Index, (uint)trackBinding.Value.Index));

                if (seed == 0)
                {
                    seed = 1u;
                }

                var random = Random.CreateFromIndex(seed);
                var index = random.NextInt(randomHashes.Length);
                return randomHashes[index].Hash;
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct PlayStateClipActivateJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public BufferLookup<AnimatorControllerLayerComponent> Layers;

            [ReadOnly]
            public BufferLookup<AnimatorControllerParameterComponent> Parameters;

            [ReadOnly]
            [NativeDisableContainerSafetyRestriction]
            public BlobDatabaseSingleton BlobDatabase;

            public bool HasBlobDatabase;

            private void Execute(in RukhankaAnimatorStateClipData clipData, in TrackBinding trackBinding)
            {
                if (clipData.Type != RukhankaAnimatorStateClipType.PlayState)
                {
                    return;
                }

                var data = clipData.PlayState;
                if (trackBinding.Value == Entity.Null || data.StateHash == 0)
                {
                    return;
                }

                if (!this.Layers.TryGetBuffer(trackBinding.Value, out var layers))
                {
                    return;
                }

                if (data.LayerIndex < 0 || data.LayerIndex >= layers.Length)
                {
                    return;
                }

                var layer = layers[data.LayerIndex];
                var stateIndex = RukhankaAnimatorStateUtility.GetStateIndex(layer.controller, data.LayerIndex, data.StateHash);
                if (stateIndex < 0)
                {
                    return;
                }

                ApplyLayerWeight(layers, ref layer, data);

                var normalizedTime = data.Mode == RukhankaAnimatorPlayStateMode.FixedTime
                    ? this.GetNormalizedTimeFromSeconds(data, stateIndex, layer, layers, trackBinding)
                    : data.NormalizedTime;

                var state = layer.rtd.MakeDefaultState();
                state.id = stateIndex;
                state.normalizedDuration = normalizedTime;

                layer.rtd.srcState = state;
                layer.rtd.dstState = layer.rtd.MakeDefaultState();
                layer.rtd.activeTransition = layer.rtd.MakeDefaultTransition();
                layer.rtd.ClearStateSnapshots();

                layers[data.LayerIndex] = layer;
            }

            private static void ApplyLayerWeight(
                DynamicBuffer<AnimatorControllerLayerComponent> layers, ref AnimatorControllerLayerComponent layer,
                in RukhankaAnimatorStatePlayStateData clipData)
            {
                if (!clipData.SetLayerWeight)
                {
                    return;
                }

                var weightLayerIndex = clipData.WeightLayerIndex;
                if (weightLayerIndex < 0 || weightLayerIndex >= layers.Length)
                {
                    return;
                }

                var weightLayer = layers[weightLayerIndex];
                weightLayer.weight = clipData.LayerWeight;
                layers[weightLayerIndex] = weightLayer;

                if (weightLayerIndex == clipData.LayerIndex)
                {
                    layer = weightLayer;
                }
            }

            private float GetNormalizedTimeFromSeconds(
                in RukhankaAnimatorStatePlayStateData clipData, int stateIndex, AnimatorControllerLayerComponent layer,
                in DynamicBuffer<AnimatorControllerLayerComponent> layers, in TrackBinding trackBinding)
            {
                var parameters = this.Parameters.TryGetBuffer(trackBinding.Value, out var parameterBuffer) ? parameterBuffer.AsNativeArray() : default;

                var stateDuration = this.HasBlobDatabase
                    ? RukhankaAnimatorStateUtility.GetStateDurationSeconds(clipData.LayerIndex, stateIndex, layer.controller, layer.animations,
                        layers.AsNativeArray(), parameters, this.BlobDatabase)
                    : 1f;

                return stateDuration > math.FLT_MIN_NORMAL ? clipData.FixedTimeSeconds / stateDuration : 0f;
            }
        }
    }
}
#endif
