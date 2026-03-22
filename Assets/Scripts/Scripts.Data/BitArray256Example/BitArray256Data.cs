namespace Scripts.Data.BitArray256Example
{
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;

    public struct BitArray256Config : IComponentData
    {
        public float ToggleInterval;
        public float GridSpacing;
        public Entity CubePrefab;
    }

    public struct BitArray256Controller : IComponentData
    {
        public float Timer;
        public uint TotalToggles;
    }

    public struct BitCell : IComponentData
    {
        public int Index;
    }
}
