// <copyright file="CMPanTiltTrackSystem.cs" company="BovineLabs">
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
    /// Blends Cinemachine pan/tilt timeline clips and applies their results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMPanTiltTrackSystem : ISystem
    {
        private TrackLifeImpl<CMPanTilt, CMPanTiltInitial> lifeImpl;
        private TrackBlendImpl<CMPanTiltBlend, CMPanTiltAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMPanTiltClipData>();
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
                .WithAllRW<CMPanTiltAnimated>()
                .WithAll<CMPanTiltClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMPanTiltAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMPanTiltClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                PanTilts = SystemAPI.GetComponentLookup<CMPanTilt>(),
                Initials = SystemAPI.GetComponentLookup<CMPanTiltInitial>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                PanTilts = SystemAPI.GetComponentLookup<CMPanTilt>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMPanTiltAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMPanTiltClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMPanTilt> PanTilts;

            [ReadOnly]
            public ComponentLookup<CMPanTiltInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMPanTiltAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMPanTiltClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var trackBindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var clipData = ref clipDatas[entityIndexInChunk];
                    ref readonly var trackBinding = ref trackBindings[entityIndexInChunk];
                    ref readonly var clip = ref clips[entityIndexInChunk];

                    animated.Value = clipData.Type switch
                    {
                        CMPanTiltClipType.Initial => this.SelectInitialValue(clip.Track),
                        CMPanTiltClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, clipData),
                        _ => animated.Value,
                    };
                }
            }

            private CMPanTiltBlend SelectInitialValue(Entity trackEntity)
            {
                var initial = this.Initials[trackEntity];

                var panAxis = initial.Value.PanAxis;
                CinemachineAxisUtility.SanitizeAxis(ref panAxis);

                var tiltAxis = initial.Value.TiltAxis;
                CinemachineAxisUtility.ClampTiltRange(ref tiltAxis);
                CinemachineAxisUtility.SanitizeAxis(ref tiltAxis);

                return new CMPanTiltBlend
                {
                    PanAxisValue = panAxis.Value,
                    TiltAxisValue = tiltAxis.Value,
                };
            }

            private CMPanTiltBlend SelectAnimatedValue(Entity boundEntity, in CMPanTiltClipData data)
            {
                var panAxis = data.PanAxis;
                CinemachineAxisUtility.SanitizeAxis(ref panAxis);

                var tiltAxis = data.TiltAxis;
                CinemachineAxisUtility.ClampTiltRange(ref tiltAxis);
                CinemachineAxisUtility.SanitizeAxis(ref tiltAxis);

                if (this.PanTilts.TryGetRefRW(boundEntity, out var panTilt))
                {
                    ref var value = ref panTilt.ValueRW;
                    value.ReferenceFrame = data.ReferenceFrame;
                    value.RecenterTarget = data.RecenterTarget;
                    value.PanAxis = panAxis;
                    value.TiltAxis = tiltAxis;
                }

                return new CMPanTiltBlend
                {
                    PanAxisValue = panAxis.Value,
                    TiltAxisValue = tiltAxis.Value,
                };
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<CMPanTiltBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMPanTilt> PanTilts;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.PanTilts.TryGetRefRW(entity, out var panTilt))
                {
                    return;
                }

                ref var value = ref panTilt.ValueRW;
                var current = new CMPanTiltBlend
                {
                    PanAxisValue = value.PanAxis.Value,
                    TiltAxisValue = value.TiltAxis.Value,
                };

                var blended = JobHelpers.Blend(ref mixData, current, default(CMPanTiltMixer));
                ApplyBlend(ref value, in blended);
                SanitizeAxes(ref value);
            }

            private static void ApplyBlend(ref CMPanTilt panTilt, in CMPanTiltBlend blend)
            {
                var panAxis = panTilt.PanAxis;
                panAxis.Value = blend.PanAxisValue;
                panTilt.PanAxis = panAxis;

                var tiltAxis = panTilt.TiltAxis;
                tiltAxis.Value = blend.TiltAxisValue;
                panTilt.TiltAxis = tiltAxis;
            }

            private static void SanitizeAxes(ref CMPanTilt panTilt)
            {
                var panAxis = panTilt.PanAxis;
                CinemachineAxisUtility.SanitizeAxis(ref panAxis);
                panTilt.PanAxis = panAxis;

                var tiltAxis = panTilt.TiltAxis;
                CinemachineAxisUtility.ClampTiltRange(ref tiltAxis);
                CinemachineAxisUtility.SanitizeAxis(ref tiltAxis);
                panTilt.TiltAxis = tiltAxis;
            }
        }

        private struct CMPanTiltMixer : IMixer<CMPanTiltBlend>
        {
            public CMPanTiltBlend Lerp(in CMPanTiltBlend a, in CMPanTiltBlend b, in float s)
            {
                return new CMPanTiltBlend
                {
                    PanAxisValue = math.lerp(a.PanAxisValue, b.PanAxisValue, s),
                    TiltAxisValue = math.lerp(a.TiltAxisValue, b.TiltAxisValue, s),
                };
            }

            public CMPanTiltBlend Add(in CMPanTiltBlend a, in CMPanTiltBlend b)
            {
                return new CMPanTiltBlend
                {
                    PanAxisValue = a.PanAxisValue + b.PanAxisValue,
                    TiltAxisValue = a.TiltAxisValue + b.TiltAxisValue,
                };
            }
        }
    }
}
#endif
