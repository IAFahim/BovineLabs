using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace BovineLabs.EntityLinks.Samples
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(EntityLinkResolveSystem))]
    public partial struct EntityLinksDemoSystem : ISystem
    {
        private bool logged;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityLinkComponent>();
            state.RequireForUpdate<AutoEntityLinkBuffer>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (logged) return;
            logged = true;

            var playerKey = EntityLinkKeys.NameToKey("Player");
            var weaponKey = EntityLinkKeys.NameToKey("Weapon");

            Debug.Log($"[EntityLinks] Player key={playerKey} Weapon key={weaponKey}");

            foreach (var (link, buffer, entity) in SystemAPI
                         .Query<RefRO<EntityLinkComponent>, DynamicBuffer<AutoEntityLinkBuffer>>()
                         .WithEntityAccess())
            {
                Debug.Log($"[EntityLinks] Entity={entity.Index} LinkKey={link.ValueRO.Key} BufferLen={buffer.Length}");

                for (int i = 0; i < buffer.Length; i++)
                {
                    var entry = buffer[i];
                    var keyName = entry.Key == playerKey ? "Player"
                        : entry.Key == weaponKey ? "Weapon"
                        : $"Unknown({entry.Key})";
                    Debug.Log($"[EntityLinks]   Buffer[{i}] Key={keyName} Value=Entity_{entry.Value.Index}");
                }
            }
        }
    }
}
