namespace Scripts.Authoring.NativeArrayExample
{
    using Scripts.Data.NativeArrayExample;
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEngine;

    public class NativeArrayAuthoring : MonoBehaviour
    {
        public int ArraySize = 100;
        public float UpdateInterval = 0.2f;
        public int EntitiesPerRow = 10;
        public GameObject CapsulePrefab;

        private class Baker : Baker<NativeArrayAuthoring>
        {
            public override void Bake(NativeArrayAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);

                this.AddComponent(entity, new NativeArrayConfig
                {
                    ArraySize = authoring.ArraySize,
                    UpdateInterval = authoring.UpdateInterval,
                    EntitiesPerRow = authoring.EntitiesPerRow,
                    Prefab = authoring.CapsulePrefab != null ? this.GetEntity(authoring.CapsulePrefab, TransformUsageFlags.Dynamic) : Entity.Null
                });

                this.AddComponent(entity, new NativeArrayController
                {
                    Timer = 0f,
                    TotalUpdates = 0
                });
            }
        }
    }
}
