// <copyright file="VolumeLiftGammaGainTrackSystem.cs" company="BovineLabs">
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
    /// Blends lift/gamma/gain clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeLiftGammaGainTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeLiftGammaGain, VolumeLiftGammaGainInitial> lifeImpl;
        private TrackBlendImpl<VolumeLiftGammaGainBlend, VolumeLiftGammaGainAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeLiftGammaGainClipData>();
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
                .WithAllRW<VolumeLiftGammaGainAnimated>()
                .WithAll<VolumeLiftGammaGainClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeLiftGammaGainAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeLiftGammaGainClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeLiftGammaGainInitial>(true),
                Gains = SystemAPI.GetComponentLookup<VolumeLiftGammaGain>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Gains = SystemAPI.GetComponentLookup<VolumeLiftGammaGain>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeLiftGammaGainBlend CreateBlend(in VolumeLiftGammaGain data, bool useOverrides)
        {
            return new VolumeLiftGammaGainBlend
            {
                Lift = new Unity.Mathematics.float4(data.Lift.x, data.Lift.y, data.Lift.z, data.Lift.w),
                Gamma = new Unity.Mathematics.float4(data.Gamma.x, data.Gamma.y, data.Gamma.z, data.Gamma.w),
                Gain = new Unity.Mathematics.float4(data.Gain.x, data.Gain.y, data.Gain.z, data.Gain.w),
                LiftOverride = useOverrides && data.LiftOverride,
                GammaOverride = useOverrides && data.GammaOverride,
                GainOverride = useOverrides && data.GainOverride,
            };
        }

        private static VolumeLiftGammaGainBlend CreateBlend(in VolumeLiftGammaGainConstantData data)
        {
            return new VolumeLiftGammaGainBlend
            {
                Lift = data.Lift,
                Gamma = data.Gamma,
                Gain = data.Gain,
                LiftOverride = data.LiftOverride,
                GammaOverride = data.GammaOverride,
                GainOverride = data.GainOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeLiftGammaGainAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeLiftGammaGainClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeLiftGammaGainInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeLiftGammaGain> Gains;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeLiftGammaGainAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeLiftGammaGainClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeLiftGammaGainClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Gains);
                            break;
                        case VolumeLiftGammaGainClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyActive(binding.Value, clipBlob.Constant.Active, ref this.Gains);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyActive(Entity entity, bool active, ref ComponentLookup<VolumeLiftGammaGain> gains)
            {
                if (!gains.TryGetRefRW(entity, out var gain))
                {
                    return;
                }

                gain.ValueRW.Active = active;
            }

            private static void ApplyInitial(Entity entity, in VolumeLiftGammaGain initial, ref ComponentLookup<VolumeLiftGammaGain> gains)
            {
                if (!gains.TryGetRefRW(entity, out var gain))
                {
                    return;
                }

                ref var value = ref gain.ValueRW;
                value.Lift = initial.Lift;
                value.Gamma = initial.Gamma;
                value.Gain = initial.Gain;
                value.Active = initial.Active;
                value.LiftOverride = initial.LiftOverride;
                value.GammaOverride = initial.GammaOverride;
                value.GainOverride = initial.GainOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeLiftGammaGainBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeLiftGammaGain> Gains;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Gains.TryGetRefRW(entity, out var gain))
                {
                    return;
                }

                ref var value = ref gain.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeLiftGammaGainMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeLiftGammaGain gain, in VolumeLiftGammaGainBlend blend)
            {
                if (blend.LiftOverride)
                {
                    gain.LiftOverride = true;
                    gain.Lift = new Vector4(blend.Lift.x, blend.Lift.y, blend.Lift.z, blend.Lift.w);
                }

                if (blend.GammaOverride)
                {
                    gain.GammaOverride = true;
                    gain.Gamma = new Vector4(blend.Gamma.x, blend.Gamma.y, blend.Gamma.z, blend.Gamma.w);
                }

                if (blend.GainOverride)
                {
                    gain.GainOverride = true;
                    gain.Gain = new Vector4(blend.Gain.x, blend.Gain.y, blend.Gain.z, blend.Gain.w);
                }
            }
        }

        private struct VolumeLiftGammaGainMixer : IMixer<VolumeLiftGammaGainBlend>
        {
            public VolumeLiftGammaGainBlend Lerp(in VolumeLiftGammaGainBlend a, in VolumeLiftGammaGainBlend b, in float s)
            {
                return new VolumeLiftGammaGainBlend
                {
                    Lift = MixUtil.LerpFloat4(a.Lift, b.Lift, s, a.LiftOverride, b.LiftOverride),
                    Gamma = MixUtil.LerpFloat4(a.Gamma, b.Gamma, s, a.GammaOverride, b.GammaOverride),
                    Gain = MixUtil.LerpFloat4(a.Gain, b.Gain, s, a.GainOverride, b.GainOverride),
                    LiftOverride = a.LiftOverride || b.LiftOverride,
                    GammaOverride = a.GammaOverride || b.GammaOverride,
                    GainOverride = a.GainOverride || b.GainOverride,
                };
            }

            public VolumeLiftGammaGainBlend Add(in VolumeLiftGammaGainBlend a, in VolumeLiftGammaGainBlend b)
            {
                return new VolumeLiftGammaGainBlend
                {
                    Lift = MixUtil.AddFloat4(a.Lift, b.Lift, a.LiftOverride, b.LiftOverride),
                    Gamma = MixUtil.AddFloat4(a.Gamma, b.Gamma, a.GammaOverride, b.GammaOverride),
                    Gain = MixUtil.AddFloat4(a.Gain, b.Gain, a.GainOverride, b.GainOverride),
                    LiftOverride = a.LiftOverride || b.LiftOverride,
                    GammaOverride = a.GammaOverride || b.GammaOverride,
                    GainOverride = a.GainOverride || b.GainOverride,
                };
            }
        }
    }
}
#endif
