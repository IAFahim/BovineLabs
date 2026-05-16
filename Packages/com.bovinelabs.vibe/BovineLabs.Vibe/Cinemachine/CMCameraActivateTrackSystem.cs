// <copyright file="CMCameraActivateTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Applies Cinemachine activation clips authored on the timeline.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct CMCameraActivateTrackSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CMCameraActivateClipData>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new TrackDeactivateJob
            {
                Cameras = SystemAPI.GetComponentLookup<CMCamera>(),
                CMCameraEnableds = SystemAPI.GetComponentLookup<CMCameraEnabled>(),
            }.Schedule();

            new TrackActivateJob
            {
                Cameras = SystemAPI.GetComponentLookup<CMCamera>(true),
                CMCameraEnableds = SystemAPI.GetComponentLookup<CMCameraEnabled>(true),
            }.Schedule();

            new ClipActivateJob
            {
                Cameras = SystemAPI.GetComponentLookup<CMCamera>(),
                CMCameraEnableds = SystemAPI.GetComponentLookup<CMCameraEnabled>(),
            }.Schedule();
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        private partial struct TrackActivateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<CMCamera> Cameras;

            [ReadOnly]
            public ComponentLookup<CMCameraEnabled> CMCameraEnableds;

            private void Execute(ref CMCameraActivateInitial initial, in TrackBinding trackBinding)
            {
                var enabled = this.CMCameraEnableds.GetEnabledRefROOptional<CMCameraEnabled>(trackBinding.Value);

                if (!enabled.IsValid || !this.Cameras.TryGetComponent(trackBinding.Value, out var camera))
                {
                    return;
                }

                initial.Enabled = enabled.ValueRO;
                initial.Priority = camera.Priority;
                initial.OutputChannel = camera.OutputChannel;
                initial.BlendHint = camera.BlendHint;
            }
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate))]
        [WithAll(typeof(TimelineActivePrevious))]
        [WithDisabled(typeof(TimelineActive))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            public ComponentLookup<CMCamera> Cameras;

            public ComponentLookup<CMCameraEnabled> CMCameraEnableds;

            private void Execute(ref CMCameraActivateInitial initial, in TrackBinding trackBinding)
            {
                var enabled = this.CMCameraEnableds.GetEnabledRefRWOptional<CMCameraEnabled>(trackBinding.Value);

                if (!enabled.IsValid || !this.Cameras.TryGetRefRW(trackBinding.Value, out var camera))
                {
                    return;
                }

                ref var cameraValue = ref camera.ValueRW;
                enabled.ValueRW = initial.Enabled;
                cameraValue.Priority = initial.Priority;
                cameraValue.OutputChannel = initial.OutputChannel;
                cameraValue.BlendHint = initial.BlendHint;
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct ClipActivateJob : IJobEntity
        {
            public ComponentLookup<CMCamera> Cameras;

            public ComponentLookup<CMCameraEnabled> CMCameraEnableds;

            private void Execute(in CMCameraActivateClipData clipData, in TrackBinding trackBinding)
            {
                ref var data = ref clipData.Value.Value;
                var enabled = this.CMCameraEnableds.GetEnabledRefRWOptional<CMCameraEnabled>(trackBinding.Value);

                if (!enabled.IsValid || !this.Cameras.TryGetRefRW(trackBinding.Value, out var camera))
                {
                    return;
                }

                ref var cameraValue = ref camera.ValueRW;

                if (data.SetEnabled)
                {
                    enabled.ValueRW = data.Enabled;
                }

                if (data.SetPriority)
                {
                    cameraValue.Priority = data.Priority;
                }

                if (data.SetOutputChannel)
                {
                    cameraValue.OutputChannel = data.OutputChannel;
                }

                if (data.SetBlendHint)
                {
                    cameraValue.BlendHint = data.BlendHint;
                }
            }
        }
    }
}
#endif
