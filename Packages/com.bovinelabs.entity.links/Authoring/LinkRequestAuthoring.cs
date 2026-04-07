using System;
using BovineLabs.Core.Keys;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.EntityLinks.Authoring
{
    public class LinkRequestAuthoring : MonoBehaviour
    {
        public EntityLinkLookupBufferBakeData[] entityLinkLookupBufferBakeData;
        public bool resolveAtStart;
        
        [Serializable]
        public class EntityLinkLookupBufferBakeData
        {
            [K(nameof(EntityLinkKeys))]
            public byte key;
            public ResolveRule resolveRule = ResolveRule.Parent | ResolveRule.Owner;
            
            public EntityLookupRequestBuffer ToEntityLookupStoreBuffer()
            {
                return new EntityLookupRequestBuffer
                {
                    Key = key,
                    ResolveRule = resolveRule,
                };
            }
        }
        
        public class LinkComponentBaker : Baker<LinkRequestAuthoring>
        {
            public override void Bake(LinkRequestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var entityLookupStoreBuffers = AddBuffer<EntityLookupRequestBuffer>(entity);
                foreach (var linkLookupBufferBakeData in authoring.entityLinkLookupBufferBakeData)
                {
                    entityLookupStoreBuffers.Add(linkLookupBufferBakeData.ToEntityLookupStoreBuffer());
                }

                AddComponent<EntityLookupResolvedThisFrame>(entity);
                SetComponentEnabled<EntityLookupResolvedThisFrame>(entity, authoring.resolveAtStart);
                AddBuffer<EntityLookupResolveResult>(entity);
            }
        }
    }
}
