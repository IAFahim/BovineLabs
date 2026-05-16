// <copyright file="VolumeSplitToningTrackSystem.cs" company="BovineLabs">
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
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Blends split toning clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeSplitToningTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeSplitToning, VolumeSplitToningInitial> lifeImpl;
        private TrackBlendImpl<VolumeSplitToningBlend, VolumeSplitToningAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeSplitToningClipData>();
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
                .WithAllRW<VolumeSplitToningAnimated>()
                .WithAll<VolumeSplitToningClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeSplitToningAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeSplitToningClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeSplitToningInitial>(true),
                SplitTonings = SystemAPI.GetComponentLookup<VolumeSplitToning>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                SplitTonings = SystemAPI.GetComponentLookup<VolumeSplitToning>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeSplitToningBlend CreateBlend(in VolumeSplitToning data, bool useOverrides)
        {
            return new VolumeSplitToningBlend
            {
                Shadows = new float4(data.Shadows.r, data.Shadows.g, data.Shadows.b, data.Shadows.a),
                Highlights = new float4(data.Highlights.r, data.Highlights.g, data.Highlights.b, data.Highlights.a),
                Balance = data.Balance,
                ShadowsOverride = useOverrides && data.ShadowsOverride,
                HighlightsOverride = useOverrides && data.HighlightsOverride,
                BalanceOverride = useOverrides && data.BalanceOverride,
            };
        }

        private static VolumeSplitToningBlend CreateBlend(in VolumeSplitToningConstantData data)
        {
            return new VolumeSplitToningBlend
            {
                Shadows = data.Shadows,
                Highlights = data.Highlights,
                Balance = data.Balance,
                ShadowsOverride = data.ShadowsOverride,
                HighlightsOverride = data.HighlightsOverride,
                BalanceOverride = data.BalanceOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeSplitToningAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeSplitToningClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeSplitToningInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeSplitToning> SplitTonings;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeSplitToningAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeSplitToningClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeSplitToningClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.SplitTonings);
                            break;
                        case VolumeSplitToningClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyActive(binding.Value, clipBlob.Constant.Active, ref this.SplitTonings);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyActive(Entity entity, bool active, ref ComponentLookup<VolumeSplitToning> splitTonings)
            {
                if (!splitTonings.TryGetRefRW(entity, out var splitToning))
                {
                    return;
                }

                splitToning.ValueRW.Active = active;
            }

            private static void ApplyInitial(Entity entity, in VolumeSplitToning initial, ref ComponentLookup<VolumeSplitToning> splitTonings)
            {
                if (!splitTonings.TryGetRefRW(entity, out var splitToning))
                {
                    return;
                }

                ref var value = ref splitToning.ValueRW;
                value.Shadows = initial.Shadows;
                value.Highlights = initial.Highlights;
                value.Balance = initial.Balance;
                value.Active = initial.Active;
                value.ShadowsOverride = initial.ShadowsOverride;
                value.HighlightsOverride = initial.HighlightsOverride;
                value.BalanceOverride = initial.BalanceOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeSplitToningBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeSplitToning> SplitTonings;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.SplitTonings.TryGetRefRW(entity, out var splitToning))
                {
                    return;
                }

                ref var value = ref splitToning.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeSplitToningMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeSplitToning splitToning, in VolumeSplitToningBlend blend)
            {
                if (blend.ShadowsOverride)
                {
                    splitToning.ShadowsOverride = true;
                    splitToning.Shadows = new Color(blend.Shadows.x, blend.Shadows.y, blend.Shadows.z, blend.Shadows.w);
                }

                if (blend.HighlightsOverride)
                {
                    splitToning.HighlightsOverride = true;
                    splitToning.Highlights = new Color(blend.Highlights.x, blend.Highlights.y, blend.Highlights.z, blend.Highlights.w);
                }

                if (blend.BalanceOverride)
                {
                    splitToning.BalanceOverride = true;
                    splitToning.Balance = blend.Balance;
                }
            }
        }

        private struct VolumeSplitToningMixer : IMixer<VolumeSplitToningBlend>
        {
            public VolumeSplitToningBlend Lerp(in VolumeSplitToningBlend a, in VolumeSplitToningBlend b, in float s)
            {
                return new VolumeSplitToningBlend
                {
                    Shadows = MixUtil.LerpFloat4(a.Shadows, b.Shadows, s, a.ShadowsOverride, b.ShadowsOverride),
                    Highlights = MixUtil.LerpFloat4(a.Highlights, b.Highlights, s, a.HighlightsOverride, b.HighlightsOverride),
                    Balance = MixUtil.LerpFloat(a.Balance, b.Balance, s, a.BalanceOverride, b.BalanceOverride),
                    ShadowsOverride = a.ShadowsOverride || b.ShadowsOverride,
                    HighlightsOverride = a.HighlightsOverride || b.HighlightsOverride,
                    BalanceOverride = a.BalanceOverride || b.BalanceOverride,
                };
            }

            public VolumeSplitToningBlend Add(in VolumeSplitToningBlend a, in VolumeSplitToningBlend b)
            {
                return new VolumeSplitToningBlend
                {
                    Shadows = MixUtil.AddFloat4(a.Shadows, b.Shadows, a.ShadowsOverride, b.ShadowsOverride),
                    Highlights = MixUtil.AddFloat4(a.Highlights, b.Highlights, a.HighlightsOverride, b.HighlightsOverride),
                    Balance = MixUtil.AddFloat(a.Balance, b.Balance, a.BalanceOverride, b.BalanceOverride),
                    ShadowsOverride = a.ShadowsOverride || b.ShadowsOverride,
                    HighlightsOverride = a.HighlightsOverride || b.HighlightsOverride,
                    BalanceOverride = a.BalanceOverride || b.BalanceOverride,
                };
            }
        }
    }
}
#endif
