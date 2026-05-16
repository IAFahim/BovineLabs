// <copyright file="CMFollowZoomTrackSystem.cs" company="BovineLabs">
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
    /// Blends Cinemachine follow zoom timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMFollowZoomTrackSystem : ISystem
    {
        private TrackLifeImpl<CMFollowZoom, CMFollowZoomInitial> lifeImpl;
        private TrackBlendImpl<CMFollowZoomBlend, CMFollowZoomAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMFollowZoomClipData>();
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
                .WithAllRW<CMFollowZoomAnimated>()
                .WithAll<CMFollowZoomClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMFollowZoomAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMFollowZoomClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                Initials = SystemAPI.GetComponentLookup<CMFollowZoomInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                FollowZooms = SystemAPI.GetComponentLookup<CMFollowZoom>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        private static CMFollowZoomBlend CreateBlend(in CMFollowZoom zoom)
        {
            return new CMFollowZoomBlend
            {
                Width = zoom.Width,
                Damping = zoom.Damping,
                FovRange = zoom.FovRange,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMFollowZoomAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMFollowZoomClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentLookup<CMFollowZoomInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMFollowZoomAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMFollowZoomClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clipData = ref clipDatas[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        CMFollowZoomClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMFollowZoomClipType.Animated => CreateBlendFromClipData(clipData),
                        _ => animated.Value,
                    };
                }
            }

            private CMFollowZoomBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];
                return CreateBlend(initial.Value);
            }

            private static CMFollowZoomBlend CreateBlendFromClipData(in CMFollowZoomClipData clipData)
            {
                return new CMFollowZoomBlend
                {
                    Width = clipData.Width,
                    Damping = clipData.Damping,
                    FovRange = clipData.FovRange,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMFollowZoomBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMFollowZoom> FollowZooms;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.FollowZooms.TryGetRefRW(entity, out var zoom))
                {
                    return;
                }

                ref var value = ref zoom.ValueRW;
                var current = CreateBlend(value);
                var blended = JobHelpers.Blend(ref mixData, current, default(CMFollowZoomMixer));
                ApplyBlend(ref value, in blended);
                Sanitize(ref value);
            }

            private static void ApplyBlend(ref CMFollowZoom zoom, in CMFollowZoomBlend blend)
            {
                zoom.Width = blend.Width;
                zoom.Damping = blend.Damping;
                zoom.FovRange = blend.FovRange;
            }

            private static void Sanitize(ref CMFollowZoom zoom)
            {
                zoom.Width = math.max(0f, zoom.Width);
                zoom.Damping = math.max(0f, zoom.Damping);

                var range = zoom.FovRange;
                range.y = math.clamp(range.y, 1f, 179f);
                range.x = math.clamp(range.x, 1f, range.y);
                zoom.FovRange = range;
            }
        }

        private struct CMFollowZoomMixer : IMixer<CMFollowZoomBlend>
        {
            public CMFollowZoomBlend Lerp(in CMFollowZoomBlend a, in CMFollowZoomBlend b, in float s)
            {
                return new CMFollowZoomBlend
                {
                    Width = math.lerp(a.Width, b.Width, s),
                    Damping = math.lerp(a.Damping, b.Damping, s),
                    FovRange = math.lerp(a.FovRange, b.FovRange, s),
                };
            }

            public CMFollowZoomBlend Add(in CMFollowZoomBlend a, in CMFollowZoomBlend b)
            {
                return new CMFollowZoomBlend
                {
                    Width = a.Width + b.Width,
                    Damping = a.Damping + b.Damping,
                    FovRange = a.FovRange + b.FovRange,
                };
            }
        }
    }
}
#endif
