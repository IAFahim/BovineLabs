namespace Scripts.Authoring.BitArray256Example
{
    using Scripts.Data.BitArray256Example;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;
    using UnityEngine;

    public class BitArray256Authoring : MonoBehaviour
    {
        public float ToggleInterval = 0.1f;
        public float GridSpacing = 1.2f;
        public GameObject CubePrefab;

        private class Baker : Baker<BitArray256Authoring>
        {
            public override void Bake(BitArray256Authoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                
                this.AddComponent(entity, new BitArray256Config
                {
                    ToggleInterval = authoring.ToggleInterval,
                    GridSpacing = authoring.GridSpacing,
                    CubePrefab = authoring.CubePrefab != null ? this.GetEntity(authoring.CubePrefab, TransformUsageFlags.Dynamic) : Entity.Null
                });

                this.AddComponent(entity, new BitArray256Controller
                {
                    Timer = 0f,
                    TotalToggles = 0
                });
            }
        }
    }
}
