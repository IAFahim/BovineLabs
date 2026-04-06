using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.EntityLinks
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EntityLinkResolveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AutoEntityLinkBuffer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bufferLookup = SystemAPI.GetBufferLookup<AutoEntityLinkBuffer>(false);
            var ltwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);

            foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<AutoEntityLinkBuffer>>().WithEntityAccess())
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var link = buffer[i];
                    if (link.Value != Entity.Null && ltwLookup.HasComponent(link.Value))
                    {
                        var targetPos = ltwLookup[link.Value].Position;
                    }
                }
            }
        }
    }
}
