using System;
using BovineLabs.Core.Keys;
using BovineLabs.Reaction.Data.Core;
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
            public Target assignTo = Target.Target;
            
            public EntityLookupRequestBuffer ToEntityLookupStoreBuffer()
            {
                return new EntityLookupRequestBuffer
                {
                    Key = this.key,
                    ResolveRule = this.resolveRule,
                    AssignTo = this.assignTo,
                };
            }
        }
        
        public class LinkComponentBaker : Baker<LinkRequestAuthoring>
        {
            public override void Bake(LinkRequestAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var requests = AddBuffer<EntityLookupRequestBuffer>(entity);
    
                foreach (var b in authoring.entityLinkLookupBufferBakeData)
                {
                    requests.Add(b.ToEntityLookupStoreBuffer());
                }

                AddComponent<EntityLookupRequestedThisFrame>(entity);
                SetComponentEnabled<EntityLookupRequestedThisFrame>(entity, authoring.resolveAtStart);
                
                AddBuffer<EntityLookupResolveResult>(entity);
            }
        }
    }
}