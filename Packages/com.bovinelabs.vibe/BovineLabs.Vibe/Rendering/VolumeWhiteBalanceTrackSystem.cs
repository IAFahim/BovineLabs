// <copyright file="VolumeWhiteBalanceTrackSystem.cs" company="BovineLabs">
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

    /// <summary>
    /// Blends white balance clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeWhiteBalanceTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeWhiteBalance, VolumeWhiteBalanceInitial> lifeImpl;
        private TrackBlendImpl<VolumeWhiteBalanceBlend, VolumeWhiteBalanceAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeWhiteBalanceClipData>();
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
                .WithAllRW<VolumeWhiteBalanceAnimated>()
                .WithAll<VolumeWhiteBalanceClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeWhiteBalanceAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeWhiteBalanceClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeWhiteBalanceInitial>(true),
                WhiteBalances = SystemAPI.GetComponentLookup<VolumeWhiteBalance>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                WhiteBalances = SystemAPI.GetComponentLookup<VolumeWhiteBalance>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeWhiteBalanceBlend CreateBlend(in VolumeWhiteBalance data, bool useOverrides)
        {
            return new VolumeWhiteBalanceBlend
            {
                Temperature = data.Temperature,
                Tint = data.Tint,
                TemperatureOverride = useOverrides && data.TemperatureOverride,
                TintOverride = useOverrides && data.TintOverride,
            };
        }

        private static VolumeWhiteBalanceBlend CreateBlend(in VolumeWhiteBalanceConstantData data)
        {
            return new VolumeWhiteBalanceBlend
            {
                Temperature = data.Temperature,
                Tint = data.Tint,
                TemperatureOverride = data.TemperatureOverride,
                TintOverride = data.TintOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeWhiteBalanceAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeWhiteBalanceClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeWhiteBalanceInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeWhiteBalance> WhiteBalances;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeWhiteBalanceAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeWhiteBalanceClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeWhiteBalanceClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.WhiteBalances);
                            break;
                        case VolumeWhiteBalanceClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyActive(binding.Value, clipBlob.Constant.Active, ref this.WhiteBalances);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyActive(Entity entity, bool active, ref ComponentLookup<VolumeWhiteBalance> whiteBalances)
            {
                if (!whiteBalances.TryGetRefRW(entity, out var whiteBalance))
                {
                    return;
                }

                whiteBalance.ValueRW.Active = active;
            }

            private static void ApplyInitial(Entity entity, in VolumeWhiteBalance initial, ref ComponentLookup<VolumeWhiteBalance> whiteBalances)
            {
                if (!whiteBalances.TryGetRefRW(entity, out var whiteBalance))
                {
                    return;
                }

                ref var value = ref whiteBalance.ValueRW;
                value.Temperature = initial.Temperature;
                value.Tint = initial.Tint;
                value.Active = initial.Active;
                value.TemperatureOverride = initial.TemperatureOverride;
                value.TintOverride = initial.TintOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeWhiteBalanceBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeWhiteBalance> WhiteBalances;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.WhiteBalances.TryGetRefRW(entity, out var whiteBalance))
                {
                    return;
                }

                ref var value = ref whiteBalance.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeWhiteBalanceMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeWhiteBalance whiteBalance, in VolumeWhiteBalanceBlend blend)
            {
                if (blend.TemperatureOverride)
                {
                    whiteBalance.TemperatureOverride = true;
                    whiteBalance.Temperature = blend.Temperature;
                }

                if (blend.TintOverride)
                {
                    whiteBalance.TintOverride = true;
                    whiteBalance.Tint = blend.Tint;
                }
            }
        }

        private struct VolumeWhiteBalanceMixer : IMixer<VolumeWhiteBalanceBlend>
        {
            public VolumeWhiteBalanceBlend Lerp(in VolumeWhiteBalanceBlend a, in VolumeWhiteBalanceBlend b, in float s)
            {
                return new VolumeWhiteBalanceBlend
                {
                    Temperature = MixUtil.LerpFloat(a.Temperature, b.Temperature, s, a.TemperatureOverride, b.TemperatureOverride),
                    Tint = MixUtil.LerpFloat(a.Tint, b.Tint, s, a.TintOverride, b.TintOverride),
                    TemperatureOverride = a.TemperatureOverride || b.TemperatureOverride,
                    TintOverride = a.TintOverride || b.TintOverride,
                };
            }

            public VolumeWhiteBalanceBlend Add(in VolumeWhiteBalanceBlend a, in VolumeWhiteBalanceBlend b)
            {
                return new VolumeWhiteBalanceBlend
                {
                    Temperature = MixUtil.AddFloat(a.Temperature, b.Temperature, a.TemperatureOverride, b.TemperatureOverride),
                    Tint = MixUtil.AddFloat(a.Tint, b.Tint, a.TintOverride, b.TintOverride),
                    TemperatureOverride = a.TemperatureOverride || b.TemperatureOverride,
                    TintOverride = a.TintOverride || b.TintOverride,
                };
            }
        }
    }
}
#endif
