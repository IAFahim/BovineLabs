namespace Scripts.Data.NativeArrayExample
{
    using Unity.Entities;
    using Unity.Mathematics;

    public struct NativeArrayConfig : IComponentData
    {
        public int ArraySize;
        public float UpdateInterval;
        public int EntitiesPerRow;
        public Entity Prefab;
    }

    public struct NativeArrayController : IComponentData
    {
        public float Timer;
        public uint TotalUpdates;
    }

    public struct ArrayItem : IComponentData
    {
        public int ArrayIndex;
        public float Value;
    }
}
