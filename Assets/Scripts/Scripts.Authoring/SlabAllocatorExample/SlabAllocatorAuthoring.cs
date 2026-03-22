namespace Scripts.Authoring.SlabAllocatorExample
{
    using Scripts.Data.SlabAllocatorExample;
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEngine;

    public class SlabAllocatorAuthoring : MonoBehaviour
    {
        public int SlabSize = 8;
        public int MaxSlabs = 50;
        public float AllocationInterval = 0.15f;
        public GameObject BoxPrefab;

        private class Baker : Baker<SlabAllocatorAuthoring>
        {
            public override void Bake(SlabAllocatorAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);

                this.AddComponent(entity, new SlabAllocatorConfig
                {
                    SlabSize = authoring.SlabSize,
                    MaxSlabs = authoring.MaxSlabs,
                    AllocationInterval = authoring.AllocationInterval,
                    Prefab = authoring.BoxPrefab != null ? this.GetEntity(authoring.BoxPrefab, TransformUsageFlags.Dynamic) : Entity.Null
                });

                this.AddComponent(entity, new SlabAllocatorController
                {
                    Timer = 0f,
                    TotalAllocations = 0,
                    TotalDeallocations = 0
                });
            }
        }
    }
}
