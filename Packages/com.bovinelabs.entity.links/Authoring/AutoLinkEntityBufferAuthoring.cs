using Unity.Entities;
using UnityEngine;

namespace BovineLabs.EntityLinks.Authoring
{
    public class AutoLinkEntityBufferAuthoring : MonoBehaviour
    {
        public class AutoLinkEntityBufferBaker : Baker<AutoLinkEntityBufferAuthoring>
        {
            public override void Bake(AutoLinkEntityBufferAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddBuffer<AutoEntityLinkBuffer>(entity);
            }
        }
    }
}
