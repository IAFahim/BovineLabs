using BovineLabs.EntityLinks.Authoring;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.EntityLinks.Samples
{
    public class EntityLinksSample : MonoBehaviour
    {
        [K(nameof(EntityLinkKeys))]
        public byte TargetKey;

        public class Baker : Baker<EntityLinksSample>
        {
            public override void Bake(EntityLinksSample authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EntityLinkComponent
                {
                    Key = authoring.TargetKey,
                });
                AddBuffer<AutoEntityLinkBuffer>(entity);
            }
        }
    }
}
