// <copyright file="CMBasicMultiChannelPerlinTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Core.Jobs;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Cinemachine;
    using BovineLabs.Vibe.Jobs;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Cinemachine;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Evaluates Cinemachine basic multi channel perlin tracks and applies their blended results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMBasicMultiChannelPerlinTrackSystem : ISystem
    {
        private TrackLifeImpl<CMBasicMultiChannelPerlin, CMBasicMultiChannelPerlinInitial> lifeImpl;
        private TrackBlendImpl<CMBasicMultiChannelPerlinBlend, CMBasicMultiChannelPerlinAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMBasicMultiChannelPerlinClipData>();
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
                .WithAllRW<CMBasicMultiChannelPerlinAnimated>()
                .WithAll<CMBasicMultiChannelPerlinClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMBasicMultiChannelPerlinAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMBasicMultiChannelPerlinClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Perlins = SystemAPI.GetComponentLookup<CMBasicMultiChannelPerlin>(),
                Initials = SystemAPI.GetComponentLookup<CMBasicMultiChannelPerlinInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Perlins = SystemAPI.GetComponentLookup<CMBasicMultiChannelPerlin>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMBasicMultiChannelPerlinAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMBasicMultiChannelPerlinClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMBasicMultiChannelPerlin> Perlins;

            [ReadOnly]
            public ComponentLookup<CMBasicMultiChannelPerlinInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMBasicMultiChannelPerlinAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMBasicMultiChannelPerlinClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var trackBindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk];
                    ref var clipBlob = ref clipData.Value.Value;
                    ref readonly var trackBinding = ref trackBindings[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipBlob.Type switch
                    {
                        CMBasicMultiChannelPerlinClipType.Initial => this.SelectInitialValue(clip.Track, trackBinding.Value),
                        CMBasicMultiChannelPerlinClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, clipBlob, clipData.NoiseProfile),
                        _ => animated.Value,
                    };
                }
            }

            private CMBasicMultiChannelPerlinBlend SelectInitialValue(Entity trackEntity, Entity boundEntity)
            {
                var initial = this.Initials[trackEntity];

                if (this.Perlins.TryGetRefRW(boundEntity, out var perlin))
                {
                    perlin.ValueRW.NoiseProfile = initial.Value.NoiseProfile;
                }

                return new CMBasicMultiChannelPerlinBlend
                {
                    AmplitudeGain = math.max(initial.Value.AmplitudeGain, 0f),
                    FrequencyGain = math.max(initial.Value.FrequencyGain, 0f),
                    PivotOffset = ToFloat3(initial.Value.PivotOffset),
                };
            }

            private CMBasicMultiChannelPerlinBlend SelectAnimatedValue(
                Entity boundEntity, in CMBasicMultiChannelPerlinClipBlob clipData, UnityObjectRef<NoiseSettings> noiseProfile)
            {
                if (clipData.OverrideNoiseProfile && this.Perlins.TryGetRefRW(boundEntity, out var perlin))
                {
                    perlin.ValueRW.NoiseProfile = noiseProfile;
                }

                return new CMBasicMultiChannelPerlinBlend
                {
                    AmplitudeGain = math.max(clipData.AmplitudeGain, 0f),
                    FrequencyGain = math.max(clipData.FrequencyGain, 0f),
                    PivotOffset = clipData.PivotOffset,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMBasicMultiChannelPerlinBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMBasicMultiChannelPerlin> Perlins;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Perlins.TryGetRefRW(entity, out var perlin))
                {
                    return;
                }

                ref var value = ref perlin.ValueRW;
                var current = new CMBasicMultiChannelPerlinBlend
                {
                    AmplitudeGain = math.max(value.AmplitudeGain, 0f),
                    FrequencyGain = math.max(value.FrequencyGain, 0f),
                    PivotOffset = ToFloat3(value.PivotOffset),
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(CMBasicMultiChannelPerlinMixer));

                value.AmplitudeGain = math.max(blended.AmplitudeGain, 0f);
                value.FrequencyGain = math.max(blended.FrequencyGain, 0f);
                value.PivotOffset = ToVector3(blended.PivotOffset);
            }
        }

        private static float3 ToFloat3(in Vector3 value)
        {
            return new float3(value.x, value.y, value.z);
        }

        private static Vector3 ToVector3(in float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }

        private struct CMBasicMultiChannelPerlinMixer : IMixer<CMBasicMultiChannelPerlinBlend>
        {
            public CMBasicMultiChannelPerlinBlend Lerp(in CMBasicMultiChannelPerlinBlend a, in CMBasicMultiChannelPerlinBlend b, in float s)
            {
                return new CMBasicMultiChannelPerlinBlend
                {
                    AmplitudeGain = math.lerp(a.AmplitudeGain, b.AmplitudeGain, s),
                    FrequencyGain = math.lerp(a.FrequencyGain, b.FrequencyGain, s),
                    PivotOffset = math.lerp(a.PivotOffset, b.PivotOffset, s),
                };
            }

            public CMBasicMultiChannelPerlinBlend Add(in CMBasicMultiChannelPerlinBlend a, in CMBasicMultiChannelPerlinBlend b)
            {
                return new CMBasicMultiChannelPerlinBlend
                {
                    AmplitudeGain = a.AmplitudeGain + b.AmplitudeGain,
                    FrequencyGain = a.FrequencyGain + b.FrequencyGain,
                    PivotOffset = a.PivotOffset + b.PivotOffset,
                };
            }
        }
    }
}
#endif
