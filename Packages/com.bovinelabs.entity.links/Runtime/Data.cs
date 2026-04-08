using System;
using BovineLabs.Reaction.Data.Core;
using Unity.Entities;

namespace BovineLabs.EntityLinks
{
    public struct EntityLookupStoreBuffer : IBufferElementData
    {
        public byte Key;
        public Entity Value;
    }

    [InternalBufferCapacity(4)]
    public struct EntityLookupRequestBuffer : IBufferElementData
    {
        public byte Key;
        public ResolveRule ResolveRule;
        public Target AssignTo;
    }

    public struct EntityLookupRequestedThisFrame : IComponentData, IEnableableComponent
    {
    }

    [InternalBufferCapacity(4)]
    public struct EntityLookupResolveResult : IBufferElementData
    {
        public byte Key;
        public Target AssignedTo;
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
        Source = 1 << 4,
        Target = 1 << 5
    }
}