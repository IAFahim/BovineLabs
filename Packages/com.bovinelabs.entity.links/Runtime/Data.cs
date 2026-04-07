using System;
using Unity.Entities;

namespace BovineLabs.EntityLinks
{
    [InternalBufferCapacity(0)]
    public struct EntityLookupStoreBuffer : IBufferElementData
    {
        public byte Key;
        public ResolveRule ResolveRule;
        public Entity Value;
    }


    [InternalBufferCapacity(0)]
    public struct EntityLookupRequestBuffer : IBufferElementData
    {
        public byte Key;
        public ResolveRule ResolveRule;
    }

    public struct EntityLookupResolvedThisFrame : IComponentData, IEnableableComponent
    {
    }

    [InternalBufferCapacity(0)]
    public struct EntityLookupResolveResult : IBufferElementData
    {
        public Entity Value;
    }

    [Flags]
    public enum ResolveRule : byte
    {
        None = 0,
        Parent = 1 << 0,
        ParentsTarget = 1 << 1,
        SelfTarget = 1 << 2,
        Owner = 1 << 3,
        Source = 1 << 5,
        Target = 1 << 6
    }
}