// <copyright file="TrackLifeImpl.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Jobs
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using Unity.Collections;
    using Unity.Entities;

    public struct TrackLifeImpl<TC, TI>
        where TI : unmanaged, IInitial<TC>
        where TC : unmanaged, IComponentData
    {
        private EntityQuery deactivateQuery;
        private EntityQuery activateQuery;
        private ComponentTypeHandle<TI> initialHandle;
        private ComponentTypeHandle<TrackBinding> trackBindingHandle;
        private ComponentLookup<TC> targets;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp);

            this.deactivateQuery = builder
                .WithAll<TrackResetOnDeactivate, TI, TrackBinding, TimelineActivePrevious>()
                .WithDisabled<TimelineActive>()
                .Build(ref state);

            builder.Reset();

            this.activateQuery = builder
                .WithAllRW<TI>()
                .WithAll<TrackBinding, TimelineActive>()
                .WithDisabled<TimelineActivePrevious>()
                .Build(ref state);

            this.initialHandle = state.GetComponentTypeHandle<TI>();
            this.trackBindingHandle = state.GetComponentTypeHandle<TrackBinding>(true);
            this.targets = state.GetComponentLookup<TC>();
        }

        // Trick for burst compatible jobs without having to register
        public void OnUpdate(ref SystemState state, TrackDeactivateJob<TC, TI> deactivateJob = default, TrackActivateJob<TC, TI> activateJob = default)
        {
            this.initialHandle.Update(ref state);
            this.trackBindingHandle.Update(ref state);
            this.targets.Update(ref state);

            deactivateJob.InitialHandle = this.initialHandle;
            deactivateJob.TrackBindingHandle = this.trackBindingHandle;
            deactivateJob.Targets = this.targets;

            activateJob.InitialHandle = this.initialHandle;
            activateJob.TrackBindingHandle = this.trackBindingHandle;
            activateJob.Targets = this.targets;

            state.Dependency = deactivateJob.Schedule(this.deactivateQuery, state.Dependency);
            state.Dependency = activateJob.ScheduleParallel(this.activateQuery, state.Dependency);
        }
    }
}
