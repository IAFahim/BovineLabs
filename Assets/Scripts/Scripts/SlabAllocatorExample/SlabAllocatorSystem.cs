namespace Scripts.SlabAllocatorExample
{
    using Scripts.Data.SlabAllocatorExample;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SlabAllocatorSystem : ISystem
    {
        private NativeArray<bool> slabMap;
        private int slabCount;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SlabAllocatorConfig>();
            this.slabCount = 0;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<SlabAllocatorController>())
            {
                return;
            }

            ref var controller = ref SystemAPI.GetSingletonRW<SlabAllocatorController>().ValueRW;
            var config = SystemAPI.GetSingleton<SlabAllocatorConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            if (this.slabCount == 0 || this.slabMap.Length != config.SlabSize * config.MaxSlabs)
            {
                if (this.slabMap.IsCreated)
                {
                    this.slabMap.Dispose();
                }

                this.slabMap = new NativeArray<bool>(config.SlabSize * config.MaxSlabs, Allocator.Persistent);
                this.slabCount = config.SlabSize * config.MaxSlabs;
            }

            controller.Timer += SystemAPI.Time.DeltaTime;

            if (controller.Timer >= config.AllocationInterval)
            {
                controller.Timer = 0f;

                for (int i = 0; i < this.slabMap.Length; i++)
                {
                    if (!this.slabMap[i])
                    {
                        this.slabMap[i] = true;

                        var slabIndex = i / config.SlabSize;
                        var itemIndex = i % config.SlabSize;

                        var x = (slabIndex % 10) * 3f;
                        var y = (slabIndex / 10) * 3f;
                        var z = itemIndex * 0.5f;

                        var entity = ecb.Instantiate(config.Prefab);
                        ecb.SetComponent(entity, LocalTransform.FromPosition(new float3(x, y, z)));
                        ecb.SetComponent(entity, new URPMaterialPropertyBaseColor { Value = new float4(0, 0.8f, 0.2f, 1) });
                        ecb.AddComponent(entity, new SlabItem
                        {
                            SlabIndex = slabIndex,
                            ItemIndex = itemIndex
                        });

                        controller.TotalAllocations++;
                        break;
                    }
                }
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            foreach (var (item, entity) in SystemAPI.Query<RefRO<SlabItem>>().WithEntityAccess())
            {
                if (currentTime % 5f < 0.1f)
                {
                    var index = item.ValueRO.SlabIndex * config.SlabSize + item.ValueRO.ItemIndex;
                    if (index < this.slabMap.Length)
                    {
                        this.slabMap[index] = false;
                    }

                    ecb.DestroyEntity(entity);
                    controller.TotalDeallocations++;
                }
            }

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (this.slabMap.IsCreated)
            {
                this.slabMap.Dispose();
            }
        }
    }
}
