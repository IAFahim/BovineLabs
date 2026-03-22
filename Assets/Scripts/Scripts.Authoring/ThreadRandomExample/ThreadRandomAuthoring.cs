namespace Scripts.Authoring.ThreadRandomExample
{
    using Scripts.Data.ThreadRandomExample;
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEngine;

    public class ThreadRandomAuthoring : MonoBehaviour
    {
        public float SpawnInterval = 0.2f;
        public float SpawnRadius = 5f;
        public int MaxEntities = 100;
        public GameObject SpherePrefab;

        private class Baker : Baker<ThreadRandomAuthoring>
        {
            public override void Bake(ThreadRandomAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);

                this.AddComponent(entity, new ThreadRandomConfig
                {
                    SpawnInterval = authoring.SpawnInterval,
                    SpawnRadius = authoring.SpawnRadius,
                    MaxEntities = authoring.MaxEntities,
                    Prefab = authoring.SpherePrefab != null ? this.GetEntity(authoring.SpherePrefab, TransformUsageFlags.Dynamic) : Entity.Null
                });

                this.AddComponent(entity, new ThreadRandomController
                {
                    Timer = 0f,
                    TotalSpawned = 0
                });
            }
        }
    }
}
