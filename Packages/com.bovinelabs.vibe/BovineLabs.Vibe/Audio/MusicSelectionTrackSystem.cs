// <copyright file="MusicSelectionTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Audio;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Applies music track selections when timeline clips activate.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct MusicSelectionTrackSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MusicSelectionClipData>();
            state.RequireForUpdate<MusicSelection>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var musicEntity = SystemAPI.GetSingletonEntity<MusicSelection>();

            state.Dependency = new TrackDeactivateJob
            {
                MusicEntity = musicEntity,
                Selections = SystemAPI.GetComponentLookup<MusicSelection>(),
            }.Schedule(state.Dependency);

            state.Dependency = new TrackActivateJob
            {
                MusicEntity = musicEntity,
                Selections = SystemAPI.GetComponentLookup<MusicSelection>(true),
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new ClipActivateJob
            {
                MusicEntity = musicEntity,
                Selections = SystemAPI.GetComponentLookup<MusicSelection>(),
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(TrackResetOnDeactivate), typeof(TimelineActivePrevious))]
        [WithDisabled(typeof(TimelineActive))]
        private partial struct TrackDeactivateJob : IJobEntity
        {
            public Entity MusicEntity;

            public ComponentLookup<MusicSelection> Selections;

            private void Execute(in MusicSelectionInitial initial)
            {
                this.Selections.GetRefRW(this.MusicEntity).ValueRW.TrackId = initial.TrackId;
            }
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        private partial struct TrackActivateJob : IJobEntity
        {
            public Entity MusicEntity;

            [ReadOnly]
            public ComponentLookup<MusicSelection> Selections;

            private void Execute(ref MusicSelectionInitial initial)
            {
                initial.TrackId = this.Selections[this.MusicEntity].TrackId;
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        [WithDisabled(typeof(ClipActivePrevious))]
        private partial struct ClipActivateJob : IJobEntity
        {
            public Entity MusicEntity;

            public ComponentLookup<MusicSelection> Selections;

            private void Execute(in MusicSelectionClipData clipData)
            {
                this.Selections.GetRefRW(this.MusicEntity).ValueRW.TrackId = clipData.TrackId;
            }
        }
    }
}
#endif
