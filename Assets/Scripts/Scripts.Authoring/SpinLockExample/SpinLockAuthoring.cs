namespace Scripts.Authoring.SpinLockExample
{
    using Scripts.Data.SpinLockExample;
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEngine;

    public class SpinLockAuthoring : MonoBehaviour
    {
        public int WriterCount = 3;
        public int ReaderCount = 3;
        public float LockInterval = 0.3f;
        public GameObject WriterPrefab;
        public GameObject ReaderPrefab;
        public GameObject ResourcePrefab;

        private class Baker : Baker<SpinLockAuthoring>
        {
            public override void Bake(SpinLockAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);

                this.AddComponent(entity, new SpinLockConfig
                {
                    WriterCount = authoring.WriterCount,
                    ReaderCount = authoring.ReaderCount,
                    LockInterval = authoring.LockInterval,
                    WriterPrefab = authoring.WriterPrefab != null ? this.GetEntity(authoring.WriterPrefab, TransformUsageFlags.Dynamic) : Entity.Null,
                    ReaderPrefab = authoring.ReaderPrefab != null ? this.GetEntity(authoring.ReaderPrefab, TransformUsageFlags.Dynamic) : Entity.Null,
                    ResourcePrefab = authoring.ResourcePrefab != null ? this.GetEntity(authoring.ResourcePrefab, TransformUsageFlags.Dynamic) : Entity.Null
                });

                this.AddComponent(entity, new SpinLockController
                {
                    Timer = 0f,
                    TotalLockAttempts = 0,
                    SuccessfulLocks = 0,
                    FailedLocks = 0
                });
            }
        }
    }
}
