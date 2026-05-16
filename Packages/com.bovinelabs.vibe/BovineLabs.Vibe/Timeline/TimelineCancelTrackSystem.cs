// <copyright file="TimelineCancelTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Input;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Timeline;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Stops timeline playback when input is detected while a cancel clip is active.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct TimelineCancelTrackSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimelineCancelClipData>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var inputTriggered = SystemAPI.TryGetSingleton<InputCommon>(out var inputCommon) && inputCommon.AnyButtonPress;

            state.Dependency = new CancelTimelineJob
            {
                InputTriggered = inputTriggered,
                Actives = SystemAPI.GetComponentLookup<TimelineActive>(),
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(TimelineCancelClipData), typeof(ClipActive))]
        private partial struct CancelTimelineJob : IJobEntity
        {
            public bool InputTriggered;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<TimelineActive> Actives;

            private void Execute(in DirectorRoot director)
            {
                if (!this.InputTriggered)
                {
                    return;
                }

                if (this.Actives.HasComponent(director.Director))
                {
                    this.Actives.SetComponentEnabled(director.Director, false);
                }
            }
        }
    }
}
#endif
