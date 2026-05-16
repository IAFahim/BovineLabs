// <copyright file="CMGroupFramingTrackSystem.cs" company="BovineLabs">
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
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Blends Cinemachine group framing timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMGroupFramingTrackSystem : ISystem
    {
        private TrackLifeImpl<CMGroupFraming, CMGroupFramingInitial> lifeImpl;
        private TrackBlendImpl<CMGroupFramingBlend, CMGroupFramingAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMGroupFramingClipData>();
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
                .WithAllRW<CMGroupFramingAnimated>()
                .WithAll<CMGroupFramingClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMGroupFramingAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMGroupFramingClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                GroupFramings = SystemAPI.GetComponentLookup<CMGroupFraming>(),
                Initials = SystemAPI.GetComponentLookup<CMGroupFramingInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                GroupFramings = SystemAPI.GetComponentLookup<CMGroupFraming>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static CMGroupFramingBlend CreateBlend(in CMGroupFraming groupFraming)
        {
            return new CMGroupFramingBlend
            {
                FramingSize = groupFraming.FramingSize,
                CenterOffset = groupFraming.CenterOffset,
                Damping = groupFraming.Damping,
                FovRange = groupFraming.FovRange,
                DollyRange = groupFraming.DollyRange,
                OrthoSizeRange = groupFraming.OrthoSizeRange,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMGroupFramingAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMGroupFramingClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMGroupFraming> GroupFramings;

            [ReadOnly]
            public ComponentLookup<CMGroupFramingInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMGroupFramingAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMGroupFramingClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        CMGroupFramingClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMGroupFramingClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, clipBlob),
                        _ => animated.Value,
                    };
                }
            }

            private CMGroupFramingBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return CreateBlend(initial.Value);
            }

            private CMGroupFramingBlend SelectAnimatedValue(Entity boundEntity, in CMGroupFramingClipBlob clipData)
            {
                if (this.GroupFramings.TryGetRefRW(boundEntity, out var groupFraming))
                {
                    ref var value = ref groupFraming.ValueRW;
                    value.FramingMode = clipData.FramingMode;
                    value.SizeAdjustment = clipData.SizeAdjustment;
                    value.LateralAdjustment = clipData.LateralAdjustment;
                }

                return new CMGroupFramingBlend
                {
                    FramingSize = clipData.FramingSize,
                    CenterOffset = clipData.CenterOffset,
                    Damping = clipData.Damping,
                    FovRange = clipData.FovRange,
                    DollyRange = clipData.DollyRange,
                    OrthoSizeRange = clipData.OrthoSizeRange,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMGroupFramingBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMGroupFraming> GroupFramings;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.GroupFramings.TryGetRefRW(entity, out var groupFraming))
                {
                    return;
                }

                ref var value = ref groupFraming.ValueRW;
                var current = CreateBlend(value);
                var blended = JobHelpers.Blend(ref mixData, current, default(CMGroupFramingMixer));
                ApplyBlend(ref value, in blended);
                SanitizeGroupFraming(ref value);
            }
        }

        private static void ApplyBlend(ref CMGroupFraming groupFraming, in CMGroupFramingBlend blend)
        {
            groupFraming.FramingSize = blend.FramingSize;
            groupFraming.CenterOffset = blend.CenterOffset;
            groupFraming.Damping = blend.Damping;
            groupFraming.FovRange = blend.FovRange;
            groupFraming.DollyRange = blend.DollyRange;
            groupFraming.OrthoSizeRange = blend.OrthoSizeRange;
        }

        private static void SanitizeGroupFraming(ref CMGroupFraming groupFraming)
        {
            groupFraming.FramingSize = math.max(0f, groupFraming.FramingSize);
            groupFraming.CenterOffset = math.clamp(groupFraming.CenterOffset, new float2(-1f, -1f), new float2(1f, 1f));
            groupFraming.Damping = math.max(0f, groupFraming.Damping);
            groupFraming.FovRange = ClampRange(groupFraming.FovRange, 1f, 179f);
            groupFraming.DollyRange = ClampRange(groupFraming.DollyRange, 0f, float.PositiveInfinity);
            groupFraming.OrthoSizeRange = ClampRange(groupFraming.OrthoSizeRange, 0f, float.PositiveInfinity);
        }

        private static float2 ClampRange(in float2 range, float min, float max)
        {
            var minValue = math.min(range.x, range.y);
            var maxValue = math.max(range.x, range.y);

            minValue = math.max(min, minValue);
            maxValue = math.max(minValue, maxValue);

            if (!float.IsPositiveInfinity(max))
            {
                maxValue = math.min(max, maxValue);
                minValue = math.min(minValue, maxValue);
            }

            return math.float2(minValue, maxValue);
        }

        private struct CMGroupFramingMixer : IMixer<CMGroupFramingBlend>
        {
            public CMGroupFramingBlend Lerp(in CMGroupFramingBlend a, in CMGroupFramingBlend b, in float s)
            {
                return new CMGroupFramingBlend
                {
                    FramingSize = math.lerp(a.FramingSize, b.FramingSize, s),
                    CenterOffset = math.lerp(a.CenterOffset, b.CenterOffset, s),
                    Damping = math.lerp(a.Damping, b.Damping, s),
                    FovRange = math.lerp(a.FovRange, b.FovRange, s),
                    DollyRange = math.lerp(a.DollyRange, b.DollyRange, s),
                    OrthoSizeRange = math.lerp(a.OrthoSizeRange, b.OrthoSizeRange, s),
                };
            }

            public CMGroupFramingBlend Add(in CMGroupFramingBlend a, in CMGroupFramingBlend b)
            {
                return new CMGroupFramingBlend
                {
                    FramingSize = a.FramingSize + b.FramingSize,
                    CenterOffset = a.CenterOffset + b.CenterOffset,
                    Damping = a.Damping + b.Damping,
                    FovRange = a.FovRange + b.FovRange,
                    DollyRange = a.DollyRange + b.DollyRange,
                    OrthoSizeRange = a.OrthoSizeRange + b.OrthoSizeRange,
                };
            }
        }
    }
}
#endif
