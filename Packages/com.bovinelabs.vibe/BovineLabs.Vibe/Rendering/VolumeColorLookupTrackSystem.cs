// <copyright file="VolumeColorLookupTrackSystem.cs" company="BovineLabs">
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
    /// Blends color lookup clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeColorLookupTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeColorLookup, VolumeColorLookupInitial> lifeImpl;
        private TrackBlendImpl<VolumeColorLookupBlend, VolumeColorLookupAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeColorLookupClipData>();
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
                .WithAllRW<VolumeColorLookupAnimated>()
                .WithAll<VolumeColorLookupClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeColorLookupAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeColorLookupClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeColorLookupInitial>(true),
                Lookups = SystemAPI.GetComponentLookup<VolumeColorLookup>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Lookups = SystemAPI.GetComponentLookup<VolumeColorLookup>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeColorLookupBlend CreateBlend(in VolumeColorLookup data, bool useOverrides)
        {
            return new VolumeColorLookupBlend
            {
                Contribution = data.Contribution,
                ContributionOverride = useOverrides && data.ContributionOverride,
            };
        }

        private static VolumeColorLookupBlend CreateBlend(in VolumeColorLookupConstantData data)
        {
            return new VolumeColorLookupBlend
            {
                Contribution = data.Contribution,
                ContributionOverride = data.ContributionOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeColorLookupAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeColorLookupClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeColorLookupInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeColorLookup> Lookups;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeColorLookupAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeColorLookupClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeColorLookupClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Lookups);
                            break;
                        case VolumeColorLookupClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyConstantOverrides(binding.Value, in clipBlob.Constant, clipData.Texture, ref this.Lookups);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyConstantOverrides(
                Entity entity, in VolumeColorLookupConstantData data, UnityObjectRef<Texture> texture, ref ComponentLookup<VolumeColorLookup> lookups)
            {
                if (!lookups.TryGetRefRW(entity, out var lookup))
                {
                    return;
                }

                ref var value = ref lookup.ValueRW;
                value.Active = data.Active;

                if (data.TextureOverride)
                {
                    value.TextureOverride = true;
                    value.Texture = texture;
                }
            }

            private static void ApplyInitial(Entity entity, in VolumeColorLookup initial, ref ComponentLookup<VolumeColorLookup> lookups)
            {
                if (!lookups.TryGetRefRW(entity, out var lookup))
                {
                    return;
                }

                ref var value = ref lookup.ValueRW;
                value.Contribution = initial.Contribution;
                value.ContributionOverride = initial.ContributionOverride;
                value.Texture = initial.Texture;
                value.TextureOverride = initial.TextureOverride;
                value.Active = initial.Active;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeColorLookupBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeColorLookup> Lookups;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Lookups.TryGetRefRW(entity, out var lookup))
                {
                    return;
                }

                ref var value = ref lookup.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeColorLookupMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeColorLookup lookup, in VolumeColorLookupBlend blend)
            {
                if (blend.ContributionOverride)
                {
                    lookup.ContributionOverride = true;
                    lookup.Contribution = blend.Contribution;
                }
            }
        }

        private struct VolumeColorLookupMixer : IMixer<VolumeColorLookupBlend>
        {
            public VolumeColorLookupBlend Lerp(in VolumeColorLookupBlend a, in VolumeColorLookupBlend b, in float s)
            {
                return new VolumeColorLookupBlend
                {
                    Contribution = MixUtil.LerpFloat(a.Contribution, b.Contribution, s, a.ContributionOverride, b.ContributionOverride),
                    ContributionOverride = a.ContributionOverride || b.ContributionOverride,
                };
            }

            public VolumeColorLookupBlend Add(in VolumeColorLookupBlend a, in VolumeColorLookupBlend b)
            {
                return new VolumeColorLookupBlend
                {
                    Contribution = MixUtil.AddFloat(a.Contribution, b.Contribution, a.ContributionOverride, b.ContributionOverride),
                    ContributionOverride = a.ContributionOverride || b.ContributionOverride,
                };
            }
        }
    }
}
#endif
