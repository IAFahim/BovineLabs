// <copyright file="VolumeShadowsMidtonesHighlightsTrackSystem.cs" company="BovineLabs">
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
    /// Blends shadows/midtones/highlights clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeShadowsMidtonesHighlightsTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeShadowsMidtonesHighlights, VolumeShadowsMidtonesHighlightsInitial> lifeImpl;
        private TrackBlendImpl<VolumeShadowsMidtonesHighlightsBlend, VolumeShadowsMidtonesHighlightsAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeShadowsMidtonesHighlightsClipData>();
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
                .WithAllRW<VolumeShadowsMidtonesHighlightsAnimated>()
                .WithAll<VolumeShadowsMidtonesHighlightsClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeShadowsMidtonesHighlightsAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeShadowsMidtonesHighlightsClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeShadowsMidtonesHighlightsInitial>(true),
                Adjustments = SystemAPI.GetComponentLookup<VolumeShadowsMidtonesHighlights>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Adjustments = SystemAPI.GetComponentLookup<VolumeShadowsMidtonesHighlights>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeShadowsMidtonesHighlightsBlend CreateBlend(in VolumeShadowsMidtonesHighlights data, bool useOverrides)
        {
            return new VolumeShadowsMidtonesHighlightsBlend
            {
                Shadows = new float4(data.Shadows.x, data.Shadows.y, data.Shadows.z, data.Shadows.w),
                Midtones = new float4(data.Midtones.x, data.Midtones.y, data.Midtones.z, data.Midtones.w),
                Highlights = new float4(data.Highlights.x, data.Highlights.y, data.Highlights.z, data.Highlights.w),
                ShadowsStart = data.ShadowsStart,
                ShadowsEnd = data.ShadowsEnd,
                HighlightsStart = data.HighlightsStart,
                HighlightsEnd = data.HighlightsEnd,
                ShadowsOverride = useOverrides && data.ShadowsOverride,
                MidtonesOverride = useOverrides && data.MidtonesOverride,
                HighlightsOverride = useOverrides && data.HighlightsOverride,
                ShadowsStartOverride = useOverrides && data.ShadowsStartOverride,
                ShadowsEndOverride = useOverrides && data.ShadowsEndOverride,
                HighlightsStartOverride = useOverrides && data.HighlightsStartOverride,
                HighlightsEndOverride = useOverrides && data.HighlightsEndOverride,
            };
        }

        private static VolumeShadowsMidtonesHighlightsBlend CreateBlend(in VolumeShadowsMidtonesHighlightsConstantData data)
        {
            return new VolumeShadowsMidtonesHighlightsBlend
            {
                Shadows = data.Shadows,
                Midtones = data.Midtones,
                Highlights = data.Highlights,
                ShadowsStart = data.ShadowsStart,
                ShadowsEnd = data.ShadowsEnd,
                HighlightsStart = data.HighlightsStart,
                HighlightsEnd = data.HighlightsEnd,
                ShadowsOverride = data.ShadowsOverride,
                MidtonesOverride = data.MidtonesOverride,
                HighlightsOverride = data.HighlightsOverride,
                ShadowsStartOverride = data.ShadowsStartOverride,
                ShadowsEndOverride = data.ShadowsEndOverride,
                HighlightsStartOverride = data.HighlightsStartOverride,
                HighlightsEndOverride = data.HighlightsEndOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeShadowsMidtonesHighlightsAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeShadowsMidtonesHighlightsClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeShadowsMidtonesHighlightsInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeShadowsMidtonesHighlights> Adjustments;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeShadowsMidtonesHighlightsAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeShadowsMidtonesHighlightsClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeShadowsMidtonesHighlightsClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Adjustments);
                            break;
                        case VolumeShadowsMidtonesHighlightsClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyActive(binding.Value, clipBlob.Constant.Active, ref this.Adjustments);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyActive(Entity entity, bool active, ref ComponentLookup<VolumeShadowsMidtonesHighlights> adjustments)
            {
                if (!adjustments.TryGetRefRW(entity, out var adjustment))
                {
                    return;
                }

                adjustment.ValueRW.Active = active;
            }

            private static void ApplyInitial(Entity entity, in VolumeShadowsMidtonesHighlights initial, ref ComponentLookup<VolumeShadowsMidtonesHighlights> adjustments)
            {
                if (!adjustments.TryGetRefRW(entity, out var adjustment))
                {
                    return;
                }

                ref var value = ref adjustment.ValueRW;
                value.Shadows = initial.Shadows;
                value.Midtones = initial.Midtones;
                value.Highlights = initial.Highlights;
                value.ShadowsStart = initial.ShadowsStart;
                value.ShadowsEnd = initial.ShadowsEnd;
                value.HighlightsStart = initial.HighlightsStart;
                value.HighlightsEnd = initial.HighlightsEnd;
                value.Active = initial.Active;
                value.ShadowsOverride = initial.ShadowsOverride;
                value.MidtonesOverride = initial.MidtonesOverride;
                value.HighlightsOverride = initial.HighlightsOverride;
                value.ShadowsStartOverride = initial.ShadowsStartOverride;
                value.ShadowsEndOverride = initial.ShadowsEndOverride;
                value.HighlightsStartOverride = initial.HighlightsStartOverride;
                value.HighlightsEndOverride = initial.HighlightsEndOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeShadowsMidtonesHighlightsBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeShadowsMidtonesHighlights> Adjustments;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Adjustments.TryGetRefRW(entity, out var adjustment))
                {
                    return;
                }

                ref var value = ref adjustment.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeShadowsMidtonesHighlightsMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeShadowsMidtonesHighlights adjustment, in VolumeShadowsMidtonesHighlightsBlend blend)
            {
                if (blend.ShadowsOverride)
                {
                    adjustment.ShadowsOverride = true;
                    adjustment.Shadows = new Vector4(blend.Shadows.x, blend.Shadows.y, blend.Shadows.z, blend.Shadows.w);
                }

                if (blend.MidtonesOverride)
                {
                    adjustment.MidtonesOverride = true;
                    adjustment.Midtones = new Vector4(blend.Midtones.x, blend.Midtones.y, blend.Midtones.z, blend.Midtones.w);
                }

                if (blend.HighlightsOverride)
                {
                    adjustment.HighlightsOverride = true;
                    adjustment.Highlights = new Vector4(blend.Highlights.x, blend.Highlights.y, blend.Highlights.z, blend.Highlights.w);
                }

                if (blend.ShadowsStartOverride)
                {
                    adjustment.ShadowsStartOverride = true;
                    adjustment.ShadowsStart = blend.ShadowsStart;
                }

                if (blend.ShadowsEndOverride)
                {
                    adjustment.ShadowsEndOverride = true;
                    adjustment.ShadowsEnd = blend.ShadowsEnd;
                }

                if (blend.HighlightsStartOverride)
                {
                    adjustment.HighlightsStartOverride = true;
                    adjustment.HighlightsStart = blend.HighlightsStart;
                }

                if (blend.HighlightsEndOverride)
                {
                    adjustment.HighlightsEndOverride = true;
                    adjustment.HighlightsEnd = blend.HighlightsEnd;
                }
            }
        }

        private struct VolumeShadowsMidtonesHighlightsMixer : IMixer<VolumeShadowsMidtonesHighlightsBlend>
        {
            public VolumeShadowsMidtonesHighlightsBlend Lerp(
                in VolumeShadowsMidtonesHighlightsBlend a, in VolumeShadowsMidtonesHighlightsBlend b, in float s)
            {
                return new VolumeShadowsMidtonesHighlightsBlend
                {
                    Shadows = MixUtil.LerpFloat4(a.Shadows, b.Shadows, s, a.ShadowsOverride, b.ShadowsOverride),
                    Midtones = MixUtil.LerpFloat4(a.Midtones, b.Midtones, s, a.MidtonesOverride, b.MidtonesOverride),
                    Highlights = MixUtil.LerpFloat4(a.Highlights, b.Highlights, s, a.HighlightsOverride, b.HighlightsOverride),
                    ShadowsStart = MixUtil.LerpFloat(a.ShadowsStart, b.ShadowsStart, s, a.ShadowsStartOverride, b.ShadowsStartOverride),
                    ShadowsEnd = MixUtil.LerpFloat(a.ShadowsEnd, b.ShadowsEnd, s, a.ShadowsEndOverride, b.ShadowsEndOverride),
                    HighlightsStart = MixUtil.LerpFloat(a.HighlightsStart, b.HighlightsStart, s, a.HighlightsStartOverride, b.HighlightsStartOverride),
                    HighlightsEnd = MixUtil.LerpFloat(a.HighlightsEnd, b.HighlightsEnd, s, a.HighlightsEndOverride, b.HighlightsEndOverride),
                    ShadowsOverride = a.ShadowsOverride || b.ShadowsOverride,
                    MidtonesOverride = a.MidtonesOverride || b.MidtonesOverride,
                    HighlightsOverride = a.HighlightsOverride || b.HighlightsOverride,
                    ShadowsStartOverride = a.ShadowsStartOverride || b.ShadowsStartOverride,
                    ShadowsEndOverride = a.ShadowsEndOverride || b.ShadowsEndOverride,
                    HighlightsStartOverride = a.HighlightsStartOverride || b.HighlightsStartOverride,
                    HighlightsEndOverride = a.HighlightsEndOverride || b.HighlightsEndOverride,
                };
            }

            public VolumeShadowsMidtonesHighlightsBlend Add(
                in VolumeShadowsMidtonesHighlightsBlend a, in VolumeShadowsMidtonesHighlightsBlend b)
            {
                return new VolumeShadowsMidtonesHighlightsBlend
                {
                    Shadows = MixUtil.AddFloat4(a.Shadows, b.Shadows, a.ShadowsOverride, b.ShadowsOverride),
                    Midtones = MixUtil.AddFloat4(a.Midtones, b.Midtones, a.MidtonesOverride, b.MidtonesOverride),
                    Highlights = MixUtil.AddFloat4(a.Highlights, b.Highlights, a.HighlightsOverride, b.HighlightsOverride),
                    ShadowsStart = MixUtil.AddFloat(a.ShadowsStart, b.ShadowsStart, a.ShadowsStartOverride, b.ShadowsStartOverride),
                    ShadowsEnd = MixUtil.AddFloat(a.ShadowsEnd, b.ShadowsEnd, a.ShadowsEndOverride, b.ShadowsEndOverride),
                    HighlightsStart = MixUtil.AddFloat(a.HighlightsStart, b.HighlightsStart, a.HighlightsStartOverride, b.HighlightsStartOverride),
                    HighlightsEnd = MixUtil.AddFloat(a.HighlightsEnd, b.HighlightsEnd, a.HighlightsEndOverride, b.HighlightsEndOverride),
                    ShadowsOverride = a.ShadowsOverride || b.ShadowsOverride,
                    MidtonesOverride = a.MidtonesOverride || b.MidtonesOverride,
                    HighlightsOverride = a.HighlightsOverride || b.HighlightsOverride,
                    ShadowsStartOverride = a.ShadowsStartOverride || b.ShadowsStartOverride,
                    ShadowsEndOverride = a.ShadowsEndOverride || b.ShadowsEndOverride,
                    HighlightsStartOverride = a.HighlightsStartOverride || b.HighlightsStartOverride,
                    HighlightsEndOverride = a.HighlightsEndOverride || b.HighlightsEndOverride,
                };
            }
        }
    }
}
#endif
