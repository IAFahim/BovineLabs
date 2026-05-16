// <copyright file="TrackDeactivateJob.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Jobs
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    [BurstCompile]
    public unsafe struct TrackDeactivateJob<TC, TI> : IJobChunk
        where TI : unmanaged, IInitial<TC>
        where TC : unmanaged, IComponentData
    {
        [ReadOnly]
        public ComponentTypeHandle<TI> InitialHandle;

        [ReadOnly]
        public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<TC> Targets;

        public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var initials = (TI*)chunk.GetRequiredComponentDataPtrRO(ref this.InitialHandle);
            var trackBindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);

            var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
            while (enumerator.NextEntityIndex(out var entityIndexInChunk))
            {
                ref readonly var initial = ref initials[entityIndexInChunk];
                ref readonly var trackBinding = ref trackBindings[entityIndexInChunk];

                if (!this.Targets.TryGetRefRW(trackBinding.Value, out var target))
                {
                    continue;
                }

                target.ValueRW = initial.Value;
            }
        }
    }
}
