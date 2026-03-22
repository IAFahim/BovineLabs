namespace Scripts.NativeArrayExample
{
    using Scripts.Data.NativeArrayExample;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct NativeArraySystem : ISystem
    {
        private NativeArray<float> dataArray;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NativeArrayConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<NativeArrayController>())
            {
                return;
            }

            ref var controller = ref SystemAPI.GetSingletonRW<NativeArrayController>().ValueRW;
            var config = SystemAPI.GetSingleton<NativeArrayConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            if (!this.dataArray.IsCreated || this.dataArray.Length != config.ArraySize)
            {
                if (this.dataArray.IsCreated)
                {
                    this.dataArray.Dispose();
                }

                this.dataArray = new NativeArray<float>(config.ArraySize, Allocator.Persistent);

                for (int i = 0; i < config.ArraySize; i++)
                {
                    this.dataArray[i] = 0f;
                }

                var existingCount = SystemAPI.QueryBuilder().WithAll<ArrayItem>().Build().CalculateEntityCount();

                if (existingCount == 0)
                {
                    for (int i = 0; i < config.ArraySize; i++)
                    {
                        var row = i / config.EntitiesPerRow;
                        var col = i % config.EntitiesPerRow;

                        var entity = ecb.Instantiate(config.Prefab);
                        ecb.SetComponent(entity, LocalTransform.FromPosition(new float3(col * 1.2f, row * 1.2f, 0)));
                        ecb.SetComponent(entity, new URPMaterialPropertyBaseColor { Value = new float4(0.2f, 0.2f, 1f, 1) });
                        ecb.AddComponent(entity, new ArrayItem
                        {
                            ArrayIndex = i,
                            Value = 0f
                        });
                    }
                }
            }

            controller.Timer += SystemAPI.Time.DeltaTime;

            if (controller.Timer >= config.UpdateInterval)
            {
                controller.Timer = 0f;
                controller.TotalUpdates++;

                var random = Random.CreateFromIndex((uint)controller.TotalUpdates);

                for (int i = 0; i < this.dataArray.Length; i++)
                {
                    this.dataArray[i] = random.NextFloat(0f, 1f);
                }
            }

            foreach (var (item, color) in SystemAPI.Query<RefRW<ArrayItem>, RefRW<URPMaterialPropertyBaseColor>>())
            {
                if (item.ValueRO.ArrayIndex < this.dataArray.Length)
                {
                    var value = this.dataArray[item.ValueRO.ArrayIndex];
                    item.ValueRW.Value = value;
                    color.ValueRW.Value = new float4(value, value * 0.5f, 1f - value, 1f);
                }
            }

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (this.dataArray.IsCreated)
            {
                this.dataArray.Dispose();
            }
        }
    }
}
