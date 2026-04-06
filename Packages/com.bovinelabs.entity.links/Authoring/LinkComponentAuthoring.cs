using BovineLabs.Core.Keys;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.EntityLinks.Authoring
{
    public class LinkComponentAuthoring : MonoBehaviour
    {
        [K(nameof(EntityLinkKeys))]
        public byte Key;

        public class LinkComponentBaker : Baker<LinkComponentAuthoring>
        {
            public override void Bake(LinkComponentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EntityLinkComponent
                {
                    Key = authoring.Key,
                });
            }
        }
    }
}
