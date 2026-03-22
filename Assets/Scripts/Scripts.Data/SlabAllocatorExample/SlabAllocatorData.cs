namespace Scripts.Data.SlabAllocatorExample
{
    using Unity.Entities;

    public struct SlabAllocatorConfig : IComponentData
    {
        public int SlabSize;
        public int MaxSlabs;
        public float AllocationInterval;
        public Entity Prefab;
    }

    public struct SlabAllocatorController : IComponentData
    {
        public float Timer;
        public uint TotalAllocations;
        public uint TotalDeallocations;
    }

    public struct SlabItem : IComponentData
    {
        public int SlabIndex;
        public int ItemIndex;
    }
}
