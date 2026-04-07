using System;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.EntityLinks.Authoring
{
    public class EntityLinkLookupResolverAuthoring : MonoBehaviour
    {
        public EntityTagsMonoBehavior[] links = Array.Empty<EntityTagsMonoBehavior>();

        private void OnValidate() => links = GetComponentsInChildren<EntityTagsMonoBehavior>(true);

        public class EntityLinkLookupResolverBaker : Baker<EntityLinkLookupResolverAuthoring>
        {
            public override void Bake(EntityLinkLookupResolverAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var entityLookupStoreBuffers = AddBuffer<EntityLookupStoreBuffer>(entity);
                foreach (var entitySelfIdMonoBehavior in authoring.links)
                {
                    foreach (var entitySelfIdData in entitySelfIdMonoBehavior.ids)
                    {
                        entityLookupStoreBuffers.Add(new EntityLookupStoreBuffer
                            {
                                Key = entitySelfIdData.key,
                                Value = GetEntity(entitySelfIdMonoBehavior.gameObject, TransformUsageFlags.None)
                            }
                        );
                    }
                }
            }
        }
    }
}