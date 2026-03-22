namespace Scripts.Data.ThreadRandomExample
{
    using Unity.Entities;
    using Unity.Mathematics;

    public struct ThreadRandomConfig : IComponentData
    {
        public float SpawnInterval;
        public float SpawnRadius;
        public int MaxEntities;
        public Entity Prefab;
    }

    public struct ThreadRandomController : IComponentData
    {
        public float Timer;
        public uint TotalSpawned;
    }

    public struct RandomSpawnedEntity : IComponentData
    {
        public float3 RandomPosition;
        public float4 RandomColor;
        public float LifeRemaining;
    }
}
