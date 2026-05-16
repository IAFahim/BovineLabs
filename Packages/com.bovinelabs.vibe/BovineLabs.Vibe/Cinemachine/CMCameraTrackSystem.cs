// <copyright file="CMCameraTrackSystem.cs" company="BovineLabs">
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
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Cinemachine;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Evaluates Cinemachine camera timeline tracks and applies their blended results.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct CMCameraTrackSystem : ISystem
    {
        private TrackBlendImpl<float, CMCameraAnimated> impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMCameraClipData>();
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
            var camerasRw = SystemAPI.GetComponentLookup<CMCamera>();
            var camerasRo = SystemAPI.GetComponentLookup<CMCamera>(true);

            new TrackDeactivateJob { Cameras = camerasRw }.Schedule();
            new TrackActivateJob { Cameras = camerasRo }.ScheduleParallel();

            var clipActivateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<CMCameraAnimated>()
                .WithAll<CMCameraClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<CMCameraAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<CMCameraClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                Cameras = camerasRo,
                Initials = SystemAPI.GetComponentLookup<CMCameraInitial>(true),
            }.ScheduleParallel(clipActivateQuery, state.Dependency);

            var blendData = this.impl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Cameras = camerasRw,
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))]
        [WithDisabled(typeof(TimelineActive))]
        [WithAll(typeof(TimelineActivePrevious))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            public ComponentLookup<CMCamera> Cameras;

            private void Execute(in CMCameraInitial initial, in TrackBinding trackBinding)
            {
                if (!this.Cameras.TryGetRefRW(trackBinding.Value, out var camera))
                {
                    return;
                }

                ref var cameraValue = ref camera.ValueRW;
                cameraValue.FieldOfView = math.clamp(initial.FieldOfView, 1f, 179f);
                cameraValue.OrthographicSize = math.max(initial.OrthographicSize, 0.01f);
            }
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        private partial struct TrackActivateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<CMCamera> Cameras;

            private void Execute(ref CMCameraInitial initial, in TrackBinding trackBinding)
            {
                if (!this.Cameras.TryGetComponent(trackBinding.Value, out var camera))
                {
                    return;
                }

                initial.FieldOfView = camera.FieldOfView;
                initial.OrthographicSize = camera.OrthographicSize;
            }
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<CMCameraAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<CMCameraClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<CMCamera> Cameras;

            [ReadOnly]
            public ComponentLookup<CMCameraInitial> Initials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (CMCameraAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (CMCameraClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
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
                        CMCameraClipType.Initial => this.SelectInitialValue(trackBinding.Value, clip.Track, animated.Value),
                        CMCameraClipType.Animated => this.SelectAnimatedValue(trackBinding.Value, in clipData),
                        _ => animated.Value,
                    };
                }
            }

            private float SelectAnimatedValue(in Entity boundEntity, in CMCameraClipData clipData)
            {
                if (!this.Cameras.TryGetComponent(boundEntity, out var camera))
                {
                    return math.clamp(clipData.FieldOfView, 1f, 179f);
                }

                return camera.ModeOverride == LensSettings.OverrideModes.Orthographic
                    ? math.max(clipData.OrthographicSize, 0.01f)
                    : math.clamp(clipData.FieldOfView, 1f, 179f);
            }

            private float SelectInitialValue(in Entity boundEntity, in Entity trackEntity, float defaultValue)
            {
                if (!this.Initials.TryGetComponent(trackEntity, out var initial))
                {
                    return defaultValue;
                }

                if (!this.Cameras.TryGetComponent(boundEntity, out var camera))
                {
                    return math.clamp(initial.FieldOfView, 1f, 179f);
                }

                return camera.ModeOverride == LensSettings.OverrideModes.Orthographic
                    ? math.max(initial.OrthographicSize, 0.01f)
                    : math.clamp(initial.FieldOfView, 1f, 179f);
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<float>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<CMCamera> Cameras;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Cameras.TryGetRefRW(entity, out var camera))
                {
                    return;
                }

                ref var cameraValue = ref camera.ValueRW;
                var useOrthographic = cameraValue.ModeOverride == LensSettings.OverrideModes.Orthographic;
                var currentValue = useOrthographic ? cameraValue.OrthographicSize : cameraValue.FieldOfView;

                var blended = JobHelpers.Blend<float, CMCameraMixer>(ref mixData, currentValue);

                if (useOrthographic)
                {
                    cameraValue.OrthographicSize = math.max(blended, 0.01f);
                }
                else
                {
                    cameraValue.FieldOfView = math.clamp(blended, 1f, 179f);
                }
            }
        }

        private struct CMCameraMixer : IMixer<float>
        {
            public float Lerp(in float a, in float b, in float t)
            {
                return math.lerp(a, b, t);
            }

            public float Add(in float a, in float b)
            {
                return a + b;
            }
        }
    }
}
#endif
