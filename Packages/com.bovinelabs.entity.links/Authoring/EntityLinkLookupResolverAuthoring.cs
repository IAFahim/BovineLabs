using System;
using Unity.Entities;
using Unity.Transforms;
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
                var entity = GetEntity(TransformUsageFlags.None);
                var entityLookupStoreBuffers = AddBuffer<EntityLookupStoreBuffer>(entity);
                foreach (var entitySelfIdMonoBehavior in authoring.links)
                {
                    foreach (var entitySelfIdData in entitySelfIdMonoBehavior.ids)
                    {
                        entityLookupStoreBuffers.Add(new EntityLookupStoreBuffer
                            {
                                Key = entitySelfIdData.key,
                                Value = GetEntity(entitySelfIdMonoBehavior.gameObject, TransformUsageFlags.None),
                                LocalTransform = entitySelfIdData.linkTransformOffset != null
                                    ? LocalTransform.FromPositionRotationScale(
                                        entitySelfIdData.linkTransformOffset.localPosition,
                                        entitySelfIdData.linkTransformOffset.localRotation,
                                        entitySelfIdData.linkTransformOffset.localScale.x)
                                    : LocalTransform.Identity
                            }
                        );
                        DependsOn(entitySelfIdData.linkTransformOffset);
                    }
                }
            }
        }
    }
}