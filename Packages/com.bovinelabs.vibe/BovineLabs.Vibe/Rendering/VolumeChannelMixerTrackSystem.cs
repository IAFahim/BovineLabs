// <copyright file="VolumeChannelMixerTrackSystem.cs" company="BovineLabs">
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
    /// Blends channel mixer clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct VolumeChannelMixerTrackSystem : ISystem
    {
        private TrackLifeImpl<VolumeChannelMixer, VolumeChannelMixerInitial> lifeImpl;
        private TrackBlendImpl<VolumeChannelMixerBlend, VolumeChannelMixerAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VolumeChannelMixerClipData>();
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
                .WithAllRW<VolumeChannelMixerAnimated>()
                .WithAll<VolumeChannelMixerClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<VolumeChannelMixerAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<VolumeChannelMixerClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Initials = SystemAPI.GetComponentLookup<VolumeChannelMixerInitial>(true),
                Mixers = SystemAPI.GetComponentLookup<VolumeChannelMixer>(),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Mixers = SystemAPI.GetComponentLookup<VolumeChannelMixer>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static VolumeChannelMixerBlend CreateBlend(in VolumeChannelMixer data, bool useOverrides)
        {
            return new VolumeChannelMixerBlend
            {
                RedOutRedIn = data.RedOutRedIn,
                RedOutGreenIn = data.RedOutGreenIn,
                RedOutBlueIn = data.RedOutBlueIn,
                GreenOutRedIn = data.GreenOutRedIn,
                GreenOutGreenIn = data.GreenOutGreenIn,
                GreenOutBlueIn = data.GreenOutBlueIn,
                BlueOutRedIn = data.BlueOutRedIn,
                BlueOutGreenIn = data.BlueOutGreenIn,
                BlueOutBlueIn = data.BlueOutBlueIn,
                RedOutRedInOverride = useOverrides && data.RedOutRedInOverride,
                RedOutGreenInOverride = useOverrides && data.RedOutGreenInOverride,
                RedOutBlueInOverride = useOverrides && data.RedOutBlueInOverride,
                GreenOutRedInOverride = useOverrides && data.GreenOutRedInOverride,
                GreenOutGreenInOverride = useOverrides && data.GreenOutGreenInOverride,
                GreenOutBlueInOverride = useOverrides && data.GreenOutBlueInOverride,
                BlueOutRedInOverride = useOverrides && data.BlueOutRedInOverride,
                BlueOutGreenInOverride = useOverrides && data.BlueOutGreenInOverride,
                BlueOutBlueInOverride = useOverrides && data.BlueOutBlueInOverride,
            };
        }

        private static VolumeChannelMixerBlend CreateBlend(in VolumeChannelMixerConstantData data)
        {
            return new VolumeChannelMixerBlend
            {
                RedOutRedIn = data.RedOutRedIn,
                RedOutGreenIn = data.RedOutGreenIn,
                RedOutBlueIn = data.RedOutBlueIn,
                GreenOutRedIn = data.GreenOutRedIn,
                GreenOutGreenIn = data.GreenOutGreenIn,
                GreenOutBlueIn = data.GreenOutBlueIn,
                BlueOutRedIn = data.BlueOutRedIn,
                BlueOutGreenIn = data.BlueOutGreenIn,
                BlueOutBlueIn = data.BlueOutBlueIn,
                RedOutRedInOverride = data.RedOutRedInOverride,
                RedOutGreenInOverride = data.RedOutGreenInOverride,
                RedOutBlueInOverride = data.RedOutBlueInOverride,
                GreenOutRedInOverride = data.GreenOutRedInOverride,
                GreenOutGreenInOverride = data.GreenOutGreenInOverride,
                GreenOutBlueInOverride = data.GreenOutBlueInOverride,
                BlueOutRedInOverride = data.BlueOutRedInOverride,
                BlueOutGreenInOverride = data.BlueOutGreenInOverride,
                BlueOutBlueInOverride = data.BlueOutBlueInOverride,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<VolumeChannelMixerAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<VolumeChannelMixerClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<VolumeChannelMixerInitial> Initials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeChannelMixer> Mixers;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (VolumeChannelMixerAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (VolumeChannelMixerClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        case VolumeChannelMixerClipType.Initial:
                            animated.Value = CreateBlend(in initial, true);
                            ApplyInitial(binding.Value, in initial, ref this.Mixers);
                            break;
                        case VolumeChannelMixerClipType.Constant:
                            animated.Value = CreateBlend(in clipBlob.Constant);
                            ApplyActive(binding.Value, clipBlob.Constant.Active, ref this.Mixers);
                            break;
                        default:
                            animated.Value = CreateBlend(in initial, false);
                            break;
                    }
                }
            }

            private static void ApplyActive(Entity entity, bool active, ref ComponentLookup<VolumeChannelMixer> mixers)
            {
                if (!mixers.TryGetRefRW(entity, out var mixer))
                {
                    return;
                }

                mixer.ValueRW.Active = active;
            }

            private static void ApplyInitial(Entity entity, in VolumeChannelMixer initial, ref ComponentLookup<VolumeChannelMixer> mixers)
            {
                if (!mixers.TryGetRefRW(entity, out var mixer))
                {
                    return;
                }

                ref var value = ref mixer.ValueRW;
                value.RedOutRedIn = initial.RedOutRedIn;
                value.RedOutGreenIn = initial.RedOutGreenIn;
                value.RedOutBlueIn = initial.RedOutBlueIn;
                value.GreenOutRedIn = initial.GreenOutRedIn;
                value.GreenOutGreenIn = initial.GreenOutGreenIn;
                value.GreenOutBlueIn = initial.GreenOutBlueIn;
                value.BlueOutRedIn = initial.BlueOutRedIn;
                value.BlueOutGreenIn = initial.BlueOutGreenIn;
                value.BlueOutBlueIn = initial.BlueOutBlueIn;
                value.Active = initial.Active;
                value.RedOutRedInOverride = initial.RedOutRedInOverride;
                value.RedOutGreenInOverride = initial.RedOutGreenInOverride;
                value.RedOutBlueInOverride = initial.RedOutBlueInOverride;
                value.GreenOutRedInOverride = initial.GreenOutRedInOverride;
                value.GreenOutGreenInOverride = initial.GreenOutGreenInOverride;
                value.GreenOutBlueInOverride = initial.GreenOutBlueInOverride;
                value.BlueOutRedInOverride = initial.BlueOutRedInOverride;
                value.BlueOutGreenInOverride = initial.BlueOutGreenInOverride;
                value.BlueOutBlueInOverride = initial.BlueOutBlueInOverride;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<VolumeChannelMixerBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<VolumeChannelMixer> Mixers;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Mixers.TryGetRefRW(entity, out var mixer))
                {
                    return;
                }

                ref var value = ref mixer.ValueRW;
                var current = CreateBlend(in value, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(VolumeChannelMixerMixer));
                ApplyBlend(ref value, in blended);
            }

            private static void ApplyBlend(ref VolumeChannelMixer mixer, in VolumeChannelMixerBlend blend)
            {
                if (blend.RedOutRedInOverride)
                {
                    mixer.RedOutRedInOverride = true;
                    mixer.RedOutRedIn = blend.RedOutRedIn;
                }

                if (blend.RedOutGreenInOverride)
                {
                    mixer.RedOutGreenInOverride = true;
                    mixer.RedOutGreenIn = blend.RedOutGreenIn;
                }

                if (blend.RedOutBlueInOverride)
                {
                    mixer.RedOutBlueInOverride = true;
                    mixer.RedOutBlueIn = blend.RedOutBlueIn;
                }

                if (blend.GreenOutRedInOverride)
                {
                    mixer.GreenOutRedInOverride = true;
                    mixer.GreenOutRedIn = blend.GreenOutRedIn;
                }

                if (blend.GreenOutGreenInOverride)
                {
                    mixer.GreenOutGreenInOverride = true;
                    mixer.GreenOutGreenIn = blend.GreenOutGreenIn;
                }

                if (blend.GreenOutBlueInOverride)
                {
                    mixer.GreenOutBlueInOverride = true;
                    mixer.GreenOutBlueIn = blend.GreenOutBlueIn;
                }

                if (blend.BlueOutRedInOverride)
                {
                    mixer.BlueOutRedInOverride = true;
                    mixer.BlueOutRedIn = blend.BlueOutRedIn;
                }

                if (blend.BlueOutGreenInOverride)
                {
                    mixer.BlueOutGreenInOverride = true;
                    mixer.BlueOutGreenIn = blend.BlueOutGreenIn;
                }

                if (blend.BlueOutBlueInOverride)
                {
                    mixer.BlueOutBlueInOverride = true;
                    mixer.BlueOutBlueIn = blend.BlueOutBlueIn;
                }
            }
        }

        private struct VolumeChannelMixerMixer : IMixer<VolumeChannelMixerBlend>
        {
            public VolumeChannelMixerBlend Lerp(in VolumeChannelMixerBlend a, in VolumeChannelMixerBlend b, in float s)
            {
                return new VolumeChannelMixerBlend
                {
                    RedOutRedIn = MixUtil.LerpFloat(a.RedOutRedIn, b.RedOutRedIn, s, a.RedOutRedInOverride, b.RedOutRedInOverride),
                    RedOutGreenIn = MixUtil.LerpFloat(a.RedOutGreenIn, b.RedOutGreenIn, s, a.RedOutGreenInOverride, b.RedOutGreenInOverride),
                    RedOutBlueIn = MixUtil.LerpFloat(a.RedOutBlueIn, b.RedOutBlueIn, s, a.RedOutBlueInOverride, b.RedOutBlueInOverride),
                    GreenOutRedIn = MixUtil.LerpFloat(a.GreenOutRedIn, b.GreenOutRedIn, s, a.GreenOutRedInOverride, b.GreenOutRedInOverride),
                    GreenOutGreenIn = MixUtil.LerpFloat(a.GreenOutGreenIn, b.GreenOutGreenIn, s, a.GreenOutGreenInOverride, b.GreenOutGreenInOverride),
                    GreenOutBlueIn = MixUtil.LerpFloat(a.GreenOutBlueIn, b.GreenOutBlueIn, s, a.GreenOutBlueInOverride, b.GreenOutBlueInOverride),
                    BlueOutRedIn = MixUtil.LerpFloat(a.BlueOutRedIn, b.BlueOutRedIn, s, a.BlueOutRedInOverride, b.BlueOutRedInOverride),
                    BlueOutGreenIn = MixUtil.LerpFloat(a.BlueOutGreenIn, b.BlueOutGreenIn, s, a.BlueOutGreenInOverride, b.BlueOutGreenInOverride),
                    BlueOutBlueIn = MixUtil.LerpFloat(a.BlueOutBlueIn, b.BlueOutBlueIn, s, a.BlueOutBlueInOverride, b.BlueOutBlueInOverride),
                    RedOutRedInOverride = a.RedOutRedInOverride || b.RedOutRedInOverride,
                    RedOutGreenInOverride = a.RedOutGreenInOverride || b.RedOutGreenInOverride,
                    RedOutBlueInOverride = a.RedOutBlueInOverride || b.RedOutBlueInOverride,
                    GreenOutRedInOverride = a.GreenOutRedInOverride || b.GreenOutRedInOverride,
                    GreenOutGreenInOverride = a.GreenOutGreenInOverride || b.GreenOutGreenInOverride,
                    GreenOutBlueInOverride = a.GreenOutBlueInOverride || b.GreenOutBlueInOverride,
                    BlueOutRedInOverride = a.BlueOutRedInOverride || b.BlueOutRedInOverride,
                    BlueOutGreenInOverride = a.BlueOutGreenInOverride || b.BlueOutGreenInOverride,
                    BlueOutBlueInOverride = a.BlueOutBlueInOverride || b.BlueOutBlueInOverride,
                };
            }

            public VolumeChannelMixerBlend Add(in VolumeChannelMixerBlend a, in VolumeChannelMixerBlend b)
            {
                return new VolumeChannelMixerBlend
                {
                    RedOutRedIn = MixUtil.AddFloat(a.RedOutRedIn, b.RedOutRedIn, a.RedOutRedInOverride, b.RedOutRedInOverride),
                    RedOutGreenIn = MixUtil.AddFloat(a.RedOutGreenIn, b.RedOutGreenIn, a.RedOutGreenInOverride, b.RedOutGreenInOverride),
                    RedOutBlueIn = MixUtil.AddFloat(a.RedOutBlueIn, b.RedOutBlueIn, a.RedOutBlueInOverride, b.RedOutBlueInOverride),
                    GreenOutRedIn = MixUtil.AddFloat(a.GreenOutRedIn, b.GreenOutRedIn, a.GreenOutRedInOverride, b.GreenOutRedInOverride),
                    GreenOutGreenIn = MixUtil.AddFloat(a.GreenOutGreenIn, b.GreenOutGreenIn, a.GreenOutGreenInOverride, b.GreenOutGreenInOverride),
                    GreenOutBlueIn = MixUtil.AddFloat(a.GreenOutBlueIn, b.GreenOutBlueIn, a.GreenOutBlueInOverride, b.GreenOutBlueInOverride),
                    BlueOutRedIn = MixUtil.AddFloat(a.BlueOutRedIn, b.BlueOutRedIn, a.BlueOutRedInOverride, b.BlueOutRedInOverride),
                    BlueOutGreenIn = MixUtil.AddFloat(a.BlueOutGreenIn, b.BlueOutGreenIn, a.BlueOutGreenInOverride, b.BlueOutGreenInOverride),
                    BlueOutBlueIn = MixUtil.AddFloat(a.BlueOutBlueIn, b.BlueOutBlueIn, a.BlueOutBlueInOverride, b.BlueOutBlueInOverride),
                    RedOutRedInOverride = a.RedOutRedInOverride || b.RedOutRedInOverride,
                    RedOutGreenInOverride = a.RedOutGreenInOverride || b.RedOutGreenInOverride,
                    RedOutBlueInOverride = a.RedOutBlueInOverride || b.RedOutBlueInOverride,
                    GreenOutRedInOverride = a.GreenOutRedInOverride || b.GreenOutRedInOverride,
                    GreenOutGreenInOverride = a.GreenOutGreenInOverride || b.GreenOutGreenInOverride,
                    GreenOutBlueInOverride = a.GreenOutBlueInOverride || b.GreenOutBlueInOverride,
                    BlueOutRedInOverride = a.BlueOutRedInOverride || b.BlueOutRedInOverride,
                    BlueOutGreenInOverride = a.BlueOutGreenInOverride || b.BlueOutGreenInOverride,
                    BlueOutBlueInOverride = a.BlueOutBlueInOverride || b.BlueOutBlueInOverride,
                };
            }
        }
    }
}
#endif
