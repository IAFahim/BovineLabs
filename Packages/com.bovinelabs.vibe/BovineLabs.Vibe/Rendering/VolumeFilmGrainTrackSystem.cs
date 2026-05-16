// <copyright file="VolumeFilmGrainTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Volume;
    using BovineLabs.Core.Jobs;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Volume;
    using BovineLabs.Vibe.Jobs;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Blends film grain clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeFilmGrainTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeFilmGrain, VolumeFilmGrainInitial> lifeImpl;
        private TrackBlendImpl<VolumeFilmGrainBlend, VolumeFilmGrainAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeFilmGrainClipData>();
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
                .WithAllRW<VolumeFilmGrainAnimated>()
                .WithAll<VolumeFilmGrainClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeFilmGrainAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeFilmGrainClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeFilmGrainInitial>(true),
                Grains = SystemAPI.GetComponentLookup<VolumeFilmGrain>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Grains = SystemAPI.GetComponentLookup<VolumeFilmGrain>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeFilmGrainBlend CreateBlend(in VolumeFilmGrain data, bool useOverrides)
        {
            return new VolumeFilmGrainBlend
            {
                Intensity = data.Intensity,
                Response = data.Response,
                IntensityOverride = useOverrides && data.IntensityOverride,
                ResponseOverride = useOverrides && data.ResponseOverride,
            };
        }

        private static VolumeFilmGrainBlend CreateBlend(in VolumeFilmGrainConstantData data)
        {
            return new VolumeFilmGrainBlend
            {
                Intensity = data.Intensity,
                Response = data.Response,
                IntensityOverride = data.IntensityOverride,
                ResponseOverride = data.ResponseOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeFilmGrainAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeFilmGrainClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeFilmGrainInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeFilmGrain> Grains;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeFilmGrainAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeFilmGrainClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var bindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk];
                    ref var clipBlob = ref clipData.Value.Value;
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    ref readonly var binding = ref bindings[entityIndexInChunk];

                    var initial = this.Initials.TryGetComponent(clip.Track, out var initialData)
                        ? initialData.Value
                        : default;

                    switch (clipBlob.Type)
                    {
                        case VolumeFilmGrainClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Grains);
                            break;
                        case VolumeFilmGrainClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyConstantOverrides(binding.Value, in clipBlob.Constant, clipData.Texture, ref this.Grains);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyConstantOverrides(
                Entity entity, in VolumeFilmGrainConstantData data, UnityObjectRef<Texture> texture, ref ComponentLookup<VolumeFilmGrain> grains)
            {
                if (!grains.TryGetRefRW(entity, out var grain))
                {
                    return;
                }

                ref var value = ref grain.ValueRW;
                value.Active = data.Active;

                if (data.TypeOverride)
                {
                    value.TypeOverride = true;
                    value.Type = data.Type;
                }

                if (data.TextureOverride)
                {
                    value.TextureOverride = true;
                    value.Texture = texture;
                }
            }

            private static void ApplyInitial(Entity entity, in VolumeFilmGrain initial, ref ComponentLookup<VolumeFilmGrain> grains)
            {
                if (!grains.TryGetRefRW(entity, out var grain))
                {
                    return;
                }

                ref var value = ref grain.ValueRW;
                value.Type = initial.Type;
                value.Intensity = initial.Intensity;
                value.Response = initial.Response;
                value.Texture = initial.Texture;
                value.Active = initial.Active;
                value.TypeOverride = initial.TypeOverride;
                value.IntensityOverride = initial.IntensityOverride;
                value.ResponseOverride = initial.ResponseOverride;
                value.TextureOverride = initial.TextureOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeFilmGrainBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeFilmGrain> Grains;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Grains.TryGetRefRW(entity, out var grain))
                {
                    return;
                }

                ref var value = ref grain.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeFilmGrainMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeFilmGrain grain, in VolumeFilmGrainBlend blend)
            {
                if (blend.IntensityOverride)
                {
                    grain.IntensityOverride = true;
                    grain.Intensity = blend.Intensity;
                }

                if (blend.ResponseOverride)
                {
                    grain.ResponseOverride = true;
                    grain.Response = blend.Response;
                }
            }
        }

        private struct VolumeFilmGrainMixer : IMixer<VolumeFilmGrainBlend>
        {
            public VolumeFilmGrainBlend Lerp(in VolumeFilmGrainBlend a, in VolumeFilmGrainBlend b, in float s)
            {
                return new VolumeFilmGrainBlend
                {
                    Intensity = MixUtil.LerpFloat(a.Intensity, b.Intensity, s, a.IntensityOverride, b.IntensityOverride),
                    Response = MixUtil.LerpFloat(a.Response, b.Response, s, a.ResponseOverride, b.ResponseOverride),
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                    ResponseOverride = a.ResponseOverride || b.ResponseOverride,
                };
            }

            public VolumeFilmGrainBlend Add(in VolumeFilmGrainBlend a, in VolumeFilmGrainBlend b)
            {
                return new VolumeFilmGrainBlend
                {
                    Intensity = MixUtil.AddFloat(a.Intensity, b.Intensity, a.IntensityOverride, b.IntensityOverride),
                    Response = MixUtil.AddFloat(a.Response, b.Response, a.ResponseOverride, b.ResponseOverride),
                    IntensityOverride = a.IntensityOverride || b.IntensityOverride,
                    ResponseOverride = a.ResponseOverride || b.ResponseOverride,
                };
            }
        }
    }
}
#endif
