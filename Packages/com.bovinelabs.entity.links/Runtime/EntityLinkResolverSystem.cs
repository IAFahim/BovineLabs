using BovineLabs.Reaction.Data.Core;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace BovineLabs.EntityLinks
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.Editor)]
    public partial struct EntityLinkResolverSystem : ISystem
    {
        private EntityQuery query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.query = SystemAPI.QueryBuilder()
                .WithAll<EntityLookupRequestBuffer, EntityLookupResolveResult>()
                .WithAllRW<EntityLookupResolvedThisFrame>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new ResolveLinksJob
            {
                EntityType = SystemAPI.GetEntityTypeHandle(),
                RequestType = SystemAPI.GetBufferTypeHandle<EntityLookupRequestBuffer>(true),
                ResultType = SystemAPI.GetBufferTypeHandle<EntityLookupResolveResult>(),
                TriggerType = SystemAPI.GetComponentTypeHandle<EntityLookupResolvedThisFrame>(),
                StoreLookup = SystemAPI.GetBufferLookup<EntityLookupStoreBuffer>(true),
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
                TargetsLookup = SystemAPI.GetComponentLookup<Targets>(true),
            };

            state.Dependency = job.ScheduleParallel(this.query, state.Dependency);
        }
    }

    [BurstCompile]
    public struct ResolveLinksJob : IJobChunk
    {
        [ReadOnly] public EntityTypeHandle EntityType;
        [ReadOnly] public BufferTypeHandle<EntityLookupRequestBuffer> RequestType;
        public BufferTypeHandle<EntityLookupResolveResult> ResultType;
        public ComponentTypeHandle<EntityLookupResolvedThisFrame> TriggerType;

        [ReadOnly] public BufferLookup<EntityLookupStoreBuffer> StoreLookup;
        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        [ReadOnly] public ComponentLookup<Targets> TargetsLookup;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(this.EntityType);
            var requestsAccessor = chunk.GetBufferAccessorRO(ref this.RequestType);
            var resultsAccessor = chunk.GetBufferAccessor(ref this.ResultType);

            for (var i = 0; i < chunk.Count; i++)
            {
                var entity = entities[i];
                var requests = requestsAccessor[i];
                var results = resultsAccessor[i];

                results.Clear();

                foreach (var request in requests)
                {
                    this.ResolveRequest(entity, request, ref results);
                }

                chunk.SetComponentEnabled(ref this.TriggerType, i, false);
            }
        }

        private void ResolveRequest(
            Entity entity, 
            EntityLookupRequestBuffer request, 
            ref DynamicBuffer<EntityLookupResolveResult> results)
        {
            var rule = request.ResolveRule;

            if (HasFlag(rule, ResolveRule.SelfTarget) && this.TryResolveFromTarget(entity, request, ref results))
            {
                return;
            }

            if (HasFlag(rule, ResolveRule.Parent) && this.TryResolveFromHierarchy(entity, request, ref results))
            {
                return;
            }

            if (this.TargetsLookup.TryGetComponent(entity, out var selfTargets))
            {
                if (HasFlag(rule, ResolveRule.Owner) && this.TryResolveFromTarget(selfTargets.Owner, request, ref results))
                {
                    return;
                }

                if (HasFlag(rule, ResolveRule.Source) && this.TryResolveFromTarget(selfTargets.Source, request, ref results))
                {
                    return;
                }

                if (HasFlag(rule, ResolveRule.Target) && this.TryResolveFromTarget(selfTargets.Target, request, ref results))
                {
                    return;
                }
            }

            if (HasFlag(rule, ResolveRule.ParentsTarget) && this.TryResolveFromParentsTarget(entity, request, ref results))
            {
                return;
            }

            results.Add(new EntityLookupResolveResult { Key = request.Key, AssignedTo = request.AssignTo, Value = Entity.Null });
        }

        private bool TryResolveFromHierarchy(
            Entity entity, 
            EntityLookupRequestBuffer request, 
            ref DynamicBuffer<EntityLookupResolveResult> results)
        {
            var current = entity;
            var depth = 0;

            while (this.ParentLookup.TryGetComponent(current, out var parent) && depth < 64)
            {
                current = parent.Value;

                if (this.TryResolveFromTarget(current, request, ref results))
                {
                    return true;
                }

                depth++;
            }

            return false;
        }

        private bool TryResolveFromParentsTarget(
            Entity entity, 
            EntityLookupRequestBuffer request, 
            ref DynamicBuffer<EntityLookupResolveResult> results)
        {
            if (!this.ParentLookup.TryGetComponent(entity, out var parent))
            {
                return false;
            }

            if (!this.TargetsLookup.TryGetComponent(parent.Value, out var parentTargets))
            {
                return false;
            }

            return this.TryResolveFromTarget(parentTargets.Target, request, ref results);
        }

        private bool TryResolveFromTarget(
            Entity targetEntity, 
            EntityLookupRequestBuffer request, 
            ref DynamicBuffer<EntityLookupResolveResult> results)
        {
            if (targetEntity == Entity.Null)
            {
                return false;
            }

            if (!this.StoreLookup.TryGetBuffer(targetEntity, out var store))
            {
                return false;
            }

            var found = false;

            foreach (var element in store)
            {
                if (element.Key == request.Key)
                {
                    results.Add(new EntityLookupResolveResult { Key = request.Key, AssignedTo = request.AssignTo, Value = element.Value });
                    found = true;
                }
            }

            return found;
        }

        private static bool HasFlag(ResolveRule rule, ResolveRule flag)
        {
            return (rule & flag) != 0;
        }
    }
}