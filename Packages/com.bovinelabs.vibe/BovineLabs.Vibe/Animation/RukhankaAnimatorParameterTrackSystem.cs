// <copyright file="RukhankaAnimatorParameterTrackSystem.cs" company="BovineLabs">
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
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Applies animator parameter changes for Rukhanka animator tracks.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    // [UpdateBefore(typeof(RukhankaAnimationSystemGroup))] // TODO we'll sort out ordering later, probably need a separate system group
    public partial struct RukhankaAnimatorParameterTrackSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RukhankaAnimatorParameterClipData>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new TrackDeactivateJob
            {
                IndexTables = SystemAPI.GetComponentLookup<AnimatorControllerParameterIndexTableComponent>(true),
                Parameters = SystemAPI.GetBufferLookup<AnimatorControllerParameterComponent>(),
                Layers = SystemAPI.GetBufferLookup<AnimatorControllerLayerComponent>(),
            }.Schedule(state.Dependency);

            state.Dependency = new TrackActivateJob
            {
                IndexTables = SystemAPI.GetComponentLookup<AnimatorControllerParameterIndexTableComponent>(true),
                Parameters = SystemAPI.GetBufferLookup<AnimatorControllerParameterComponent>(true),
                Layers = SystemAPI.GetBufferLookup<AnimatorControllerLayerComponent>(true),
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new ClipActivateJob
            {
                IndexTables = SystemAPI.GetComponentLookup<AnimatorControllerParameterIndexTableComponent>(true),
                Parameters = SystemAPI.GetBufferLookup<AnimatorControllerParameterComponent>(),
                Layers = SystemAPI.GetBufferLookup<AnimatorControllerLayerComponent>(),
            }.ScheduleParallel(state.Dependency);
        }

        private static int GetParameterIndex(
            uint hash, bool useIndexTable, in AnimatorControllerParameterIndexTableComponent indexTable,
            DynamicBuffer<AnimatorControllerParameterComponent> parameters)
        {
            if (hash == 0)
            {
                return -1;
            }

            var fastParam = new FastAnimatorParameter(hash);
            return useIndexTable ? fastParam.GetRuntimeParameterIndex(indexTable.value, parameters) : fastParam.GetRuntimeParameterIndex(parameters);
        }

        private static bool ContainsHash(in DynamicBuffer<RukhankaAnimatorParameterInitial> entries, uint hash)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].Hash == hash)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsLayer(in DynamicBuffer<RukhankaAnimatorLayerInitial> entries, int layerIndex)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].LayerIndex == layerIndex)
                {
                    return true;
                }
            }

            return false;
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))]
        [WithAll(typeof(TimelineActivePrevious))]
        [WithDisabled(typeof(TimelineActive))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<AnimatorControllerParameterIndexTableComponent> IndexTables;

            public BufferLookup<AnimatorControllerParameterComponent> Parameters;

            public BufferLookup<AnimatorControllerLayerComponent> Layers;

            private void Execute(
                in TrackBinding trackBinding, in DynamicBuffer<RukhankaAnimatorParameterInitial> parameterInitials,
                in DynamicBuffer<RukhankaAnimatorLayerInitial> layerInitials)
            {
                if (trackBinding.Value == Entity.Null)
                {
                    return;
                }

                if (parameterInitials.Length > 0 && this.Parameters.TryGetBuffer(trackBinding.Value, out var parameters))
                {
                    var useIndexTable = this.IndexTables.TryGetComponent(trackBinding.Value, out var indexTable) && indexTable.value.IsCreated;

                    foreach (var entry in parameterInitials)
                    {
                        var paramIndex = GetParameterIndex(entry.Hash, useIndexTable, indexTable, parameters);
                        if (paramIndex < 0)
                        {
                            continue;
                        }

                        var parameter = parameters[paramIndex];
                        parameter.value = entry.Value;
                        parameters[paramIndex] = parameter;
                    }
                }

                if (layerInitials.Length > 0 && this.Layers.TryGetBuffer(trackBinding.Value, out var layers))
                {
                    for (var i = 0; i < layerInitials.Length; i++)
                    {
                        var entry = layerInitials[i];
                        if (entry.LayerIndex < 0 || entry.LayerIndex >= layers.Length)
                        {
                            continue;
                        }

                        var layer = layers[entry.LayerIndex];
                        layer.weight = entry.Weight;
                        layers[entry.LayerIndex] = layer;
                    }
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        private partial struct TrackActivateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<AnimatorControllerParameterIndexTableComponent> IndexTables;

            [ReadOnly]
            public BufferLookup<AnimatorControllerParameterComponent> Parameters;

            [ReadOnly]
            public BufferLookup<AnimatorControllerLayerComponent> Layers;

            private void Execute(
                DynamicBuffer<RukhankaAnimatorParameterInitial> parameterInitials, DynamicBuffer<RukhankaAnimatorLayerInitial> layerInitials,
                in DynamicBuffer<RukhankaAnimatorParameterTrackHash> trackHashes, in DynamicBuffer<RukhankaAnimatorLayerIndex> trackLayers,
                in TrackBinding trackBinding)
            {
                parameterInitials.Clear();
                layerInitials.Clear();

                if (trackBinding.Value == Entity.Null)
                {
                    return;
                }

                if (trackHashes.Length > 0 && this.Parameters.TryGetBuffer(trackBinding.Value, out var parameters))
                {
                    var useIndexTable = this.IndexTables.TryGetComponent(trackBinding.Value, out var indexTable) && indexTable.value.IsCreated;

                    for (var i = 0; i < trackHashes.Length; i++)
                    {
                        var hash = trackHashes[i].Hash;
                        if (hash == 0 || ContainsHash(parameterInitials, hash))
                        {
                            continue;
                        }

                        var paramIndex = GetParameterIndex(hash, useIndexTable, indexTable, parameters);
                        if (paramIndex < 0)
                        {
                            continue;
                        }

                        parameterInitials.Add(new RukhankaAnimatorParameterInitial
                        {
                            Hash = hash,
                            Value = parameters[paramIndex].value,
                        });
                    }
                }

                if (trackLayers.Length == 0)
                {
                    return;
                }

                if (!this.Layers.TryGetBuffer(trackBinding.Value, out var layers))
                {
                    return;
                }

                for (var i = 0; i < trackLayers.Length; i++)
                {
                    var layerIndex = trackLayers[i].Value;
                    if (layerIndex < 0 || layerIndex >= layers.Length || ContainsLayer(layerInitials, layerIndex))
                    {
                        continue;
                    }

                    layerInitials.Add(new RukhankaAnimatorLayerInitial
                    {
                        LayerIndex = layerIndex,
                        Weight = layers[layerIndex].weight,
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
            public ComponentLookup<AnimatorControllerParameterIndexTableComponent> IndexTables;

            [NativeDisableParallelForRestriction]
            public BufferLookup<AnimatorControllerParameterComponent> Parameters;

            [NativeDisableParallelForRestriction]
            public BufferLookup<AnimatorControllerLayerComponent> Layers;

            private void Execute(
                Entity entity, in RukhankaAnimatorParameterClipData clipComponent, in TrackBinding trackBinding,
                in DynamicBuffer<RukhankaAnimatorParameterRandomHash> randomHashes)
            {
                if (trackBinding.Value == Entity.Null)
                {
                    return;
                }

                if (!this.Parameters.TryGetBuffer(trackBinding.Value, out var parameters))
                {
                    return;
                }

                var useIndexTable = this.IndexTables.TryGetComponent(trackBinding.Value, out var indexTable) && indexTable.value.IsCreated;

                ref var clipData = ref clipComponent.Value.Value;
                var seed = clipData.Seed != 0 ? clipData.Seed : math.hash(new uint2((uint)entity.Index, (uint)trackBinding.Value.Index));

                if (seed == 0)
                {
                    seed = 1u;
                }

                var random = Random.CreateFromIndex(seed);

                if (clipData.UpdateTrigger)
                {
                    TrySetTrigger(clipData.TriggerHash, clipData.TriggerMode, useIndexTable, indexTable, parameters);
                }

                if (clipData.UpdateRandomTrigger)
                {
                    var hash = SelectRandomHash(randomHashes, ref random);
                    TrySetTrigger(hash, clipData.TriggerMode, useIndexTable, indexTable, parameters);
                }

                if (clipData.UpdateBool)
                {
                    TrySetBool(clipData.BoolHash, clipData.BoolValue, useIndexTable, indexTable, parameters);
                }

                if (clipData.UpdateRandomBool)
                {
                    var hash = SelectRandomHash(randomHashes, ref random);
                    TrySetBool(hash, clipData.BoolValue, useIndexTable, indexTable, parameters);
                }

                if (clipData.IntHash != 0)
                {
                    TrySetInt(clipData, ref random, useIndexTable, indexTable, parameters);
                }

                if (clipData.FloatHash != 0)
                {
                    TrySetFloat(clipData, ref random, useIndexTable, indexTable, parameters);
                }

                if (clipData.SetLayerWeight && this.Layers.TryGetBuffer(trackBinding.Value, out var layers))
                {
                    if (clipData.LayerIndex < 0 || clipData.LayerIndex >= layers.Length)
                    {
                        return;
                    }

                    var layer = layers[clipData.LayerIndex];
                    layer.weight = clipData.LayerWeight;
                    layers[clipData.LayerIndex] = layer;
                }
            }

            private static uint SelectRandomHash(in DynamicBuffer<RukhankaAnimatorParameterRandomHash> randomHashes, ref Random random)
            {
                if (randomHashes.Length == 0)
                {
                    return 0;
                }

                var index = random.NextInt(randomHashes.Length);
                return randomHashes[index].Hash;
            }

            private static void TrySetTrigger(
                uint hash, RukhankaAnimatorParameterTriggerMode mode, bool useIndexTable, in AnimatorControllerParameterIndexTableComponent indexTable,
                DynamicBuffer<AnimatorControllerParameterComponent> parameters)
            {
                var index = GetParameterIndex(hash, useIndexTable, indexTable, parameters);
                if (index < 0)
                {
                    return;
                }

                var parameter = parameters[index];
                if (parameter.type != ControllerParameterType.Trigger)
                {
                    return;
                }

                parameter.value.boolValue = mode == RukhankaAnimatorParameterTriggerMode.Set;
                parameters[index] = parameter;
            }

            private static void TrySetBool(
                uint hash, bool value, bool useIndexTable, in AnimatorControllerParameterIndexTableComponent indexTable,
                DynamicBuffer<AnimatorControllerParameterComponent> parameters)
            {
                var index = GetParameterIndex(hash, useIndexTable, indexTable, parameters);
                if (index < 0)
                {
                    return;
                }

                var parameter = parameters[index];
                if (parameter.type != ControllerParameterType.Bool)
                {
                    return;
                }

                parameter.value.boolValue = value;
                parameters[index] = parameter;
            }

            private static void TrySetInt(
                in RukhankaAnimatorParameterClipBlob clipData, ref Random random, bool useIndexTable,
                in AnimatorControllerParameterIndexTableComponent indexTable, DynamicBuffer<AnimatorControllerParameterComponent> parameters)
            {
                var index = GetParameterIndex(clipData.IntHash, useIndexTable, indexTable, parameters);
                if (index < 0)
                {
                    return;
                }

                var parameter = parameters[index];
                if (parameter.type != ControllerParameterType.Int)
                {
                    return;
                }

                var value = clipData.IntMode switch
                {
                    RukhankaAnimatorParameterValueMode.Random => RandomInt(ref random, clipData.IntMin, clipData.IntMax),
                    RukhankaAnimatorParameterValueMode.Increment => parameter.value.intValue + clipData.IntIncrement,
                    _ => clipData.IntValue,
                };

                parameter.value.intValue = value;
                parameters[index] = parameter;
            }

            private static void TrySetFloat(
                in RukhankaAnimatorParameterClipBlob clipData, ref Random random, bool useIndexTable,
                in AnimatorControllerParameterIndexTableComponent indexTable, DynamicBuffer<AnimatorControllerParameterComponent> parameters)
            {
                var index = GetParameterIndex(clipData.FloatHash, useIndexTable, indexTable, parameters);
                if (index < 0)
                {
                    return;
                }

                var parameter = parameters[index];
                if (parameter.type != ControllerParameterType.Float)
                {
                    return;
                }

                var value = clipData.FloatMode switch
                {
                    RukhankaAnimatorParameterValueMode.Random => RandomFloat(ref random, clipData.FloatMin, clipData.FloatMax),
                    RukhankaAnimatorParameterValueMode.Increment => parameter.value.floatValue + clipData.FloatIncrement,
                    _ => clipData.FloatValue,
                };

                parameter.value.floatValue = value;
                parameters[index] = parameter;
            }

            private static int RandomInt(ref Random random, int minValue, int maxValue)
            {
                var min = math.min(minValue, maxValue);
                var max = math.max(minValue, maxValue);

                if (min == max)
                {
                    return min;
                }

                if (max == int.MaxValue)
                {
                    return random.NextInt(min, max);
                }

                return random.NextInt(min, max + 1);
            }

            private static float RandomFloat(ref Random random, float minValue, float maxValue)
            {
                var min = math.min(minValue, maxValue);
                var max = math.max(minValue, maxValue);
                return random.NextFloat(min, max);
            }
        }
    }
}
#endif
