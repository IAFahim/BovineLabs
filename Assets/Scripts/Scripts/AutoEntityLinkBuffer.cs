using Unity.Entities;
using UnityEngine;

public struct AutoEntityLinkBuffer : IBufferElementData
{
    public byte Key;
    public Entity Value;
}

public struct EntityLinkComponent : IComponentData
{
    public byte Key;
}

public class AutoEntityLinkSettings : ScriptableObject
{
    public byte key;
}


public class LinkComponentAuthoring : MonoBehaviour
{
    public AutoEntityLinkSettings autoEntityLinkSettings;

    public class LinkComponentBaker : Baker<LinkComponentAuthoring>
    {
        public override void Bake(LinkComponentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<EntityLinkComponent>(entity, new EntityLinkComponent()
                {
                    Key = authoring.autoEntityLinkSettings.key,
                }
            );
        }
    }
}

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