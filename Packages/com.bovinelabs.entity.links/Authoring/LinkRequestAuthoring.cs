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
                var entity = this.GetEntity(TransformUsageFlags.None);
                var requests = this.AddBuffer<EntityLookupRequestBuffer>(entity);
    
                foreach (var b in authoring.entityLinkLookupBufferBakeData)
                {
                    requests.Add(b.ToEntityLookupStoreBuffer());
                }

                this.AddComponent<EntityLookupRequestedThisFrame>(entity);
                this.SetComponentEnabled<EntityLookupRequestedThisFrame>(entity, authoring.resolveAtStart);
                
                this.AddBuffer<EntityLookupResolveResult>(entity);
            }
        }
    }
}