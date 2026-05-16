// <copyright file="LocalTransformTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe
{
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Transforms;

    /// <summary>
    /// Shared logic between the separate <see cref="PositionTrackSystem"/>, <see cref="RotationTrackSystem"/> and <see cref="ScaleTrackSystem"/>.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct LocalTransformTrackSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new TrackActivateJob { LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true) }.ScheduleParallel();
            new ClipActivateJob { LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true) }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        private partial struct TrackActivateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransforms;

            private void Execute(ref LocalTransformInitial localTransformInitial, in TrackBinding trackBinding)
            {
                if (!this.LocalTransforms.TryGetComponent(trackBinding.Value, out var bindingTransform))
                {
                    return;
                }

                localTransformInitial.Value = bindingTransform;
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct ClipActivateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransforms;

            private void Execute(ref LocalTransformClipInitial localTransformClipInitial, in TrackBinding trackBinding)
            {
                if (!this.LocalTransforms.TryGetComponent(trackBinding.Value, out var bindingTransform))
                {
                    return;
                }

                localTransformClipInitial.Value = bindingTransform;
            }
        }
    }
}
