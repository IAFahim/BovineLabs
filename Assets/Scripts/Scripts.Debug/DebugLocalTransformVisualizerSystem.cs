namespace Scripts.Debug
{
    using BovineLabs.Quill;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEngine;

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct DebugLocalTransformVisualizerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var drawer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer();

            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithEntityAccess())
            {
                var position = localTransform.ValueRO.Position;
                var label = CreateLabel(entity, position);
                drawer.Point(position, 0.15f, Color.cyan);
                drawer.Text128(position + new float3(0f, 0.75f, 0f), label, Color.yellow, 14f);
            }
        }

        private static Unity.Collections.FixedString128Bytes CreateLabel(Entity entity, float3 position)
        {
            return $"{entity.ToFixedString()} ({position.x:0.00}, {position.y:0.00}, {position.z:0.00})";
        }
    }
}
