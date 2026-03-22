namespace Scripts.BitArray256Example
{
    using BovineLabs.Core;
    using BovineLabs.Core.Collections;
    using Scripts.Data.BitArray256Example;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BitArray256System : ISystem
    {
        private BitArray256 bitArray;
        private Random random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BitArray256Config>();
            this.bitArray = new BitArray256();
            this.random = Random.CreateFromIndex(1234);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<BitArray256Controller>())
            {
                return;
            }

            ref var controller = ref SystemAPI.GetSingletonRW<BitArray256Controller>().ValueRW;
            var config = SystemAPI.GetSingleton<BitArray256Config>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var cellCount = SystemAPI.QueryBuilder().WithAll<BitCell>().Build().CalculateEntityCount();
            if (cellCount == 0 && config.CubePrefab != Entity.Null)
            {
                var gridSize = 16;
                var totalSize = gridSize * config.GridSpacing;
                var centerOffset = totalSize / 2f - config.GridSpacing / 2f;

                for (int i = 0; i < 256; i++)
                {
                    var col = i % gridSize;
                    var row = i / gridSize;
                    var x = col * config.GridSpacing - centerOffset;
                    var y = row * config.GridSpacing - centerOffset;

                    var cellEntity = ecb.Instantiate(config.CubePrefab);
                    ecb.SetComponent(cellEntity, LocalTransform.FromPosition(new float3(x, y, 0)));
                    ecb.AddComponent(cellEntity, new BitCell { Index = i });
                    ecb.AddComponent(cellEntity, new URPMaterialPropertyBaseColor { Value = new float4(0.2f, 0.2f, 0.2f, 1) });
                }
            }

            var deltaTime = SystemAPI.Time.DeltaTime;
            controller.Timer += deltaTime;

            if (controller.Timer >= config.ToggleInterval)
            {
                controller.Timer = 0f;
                var index = this.random.NextInt(0, 256);
                var currentValue = this.bitArray[index];
                this.bitArray[index] = !currentValue;
                controller.TotalToggles++;
            }

            foreach (var (cell, transform, color) in SystemAPI.Query<RefRO<BitCell>, RefRW<LocalTransform>, RefRW<URPMaterialPropertyBaseColor>>())
            {
                var isSet = this.bitArray[cell.ValueRO.Index];
                color.ValueRW.Value = isSet ? new float4(0, 1, 0, 1) : new float4(0.2f, 0.2f, 0.2f, 1);
                transform.ValueRW.Scale = isSet ? 1f : 0.5f;
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
