namespace Scripts.ThreadRandomExample
{
    using Scripts.Data.ThreadRandomExample;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ThreadRandomSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ThreadRandomConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<ThreadRandomController>())
            {
                return;
            }

            ref var controller = ref SystemAPI.GetSingletonRW<ThreadRandomController>().ValueRW;
            var config = SystemAPI.GetSingleton<ThreadRandomConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var parallelEcb = ecb.AsParallelWriter();

            controller.Timer += SystemAPI.Time.DeltaTime;

            if (controller.Timer >= config.SpawnInterval)
            {
                controller.Timer = 0f;

                var currentCount = SystemAPI.QueryBuilder().WithAll<RandomSpawnedEntity>().Build().CalculateEntityCount();

                if (currentCount < config.MaxEntities)
                {
                    var random = Random.CreateFromIndex((uint)controller.TotalSpawned + 1);
                    var position = random.NextFloat3Direction() * random.NextFloat(0, config.SpawnRadius);
                    var color = new float4(random.NextFloat(0.5f, 1f), random.NextFloat(0.5f, 1f), random.NextFloat(0.5f, 1f), 1f);

                    var entity = ecb.Instantiate(config.Prefab);
                    ecb.SetComponent(entity, LocalTransform.FromPosition(position));
                    ecb.SetComponent(entity, new URPMaterialPropertyBaseColor { Value = color });
                    ecb.AddComponent(entity, new RandomSpawnedEntity
                    {
                        RandomPosition = position,
                        RandomColor = color,
                        LifeRemaining = 5f
                    });

                    controller.TotalSpawned++;
                }
            }

            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (spawned, transform, entity) in SystemAPI.Query<RefRW<RandomSpawnedEntity>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                var life = spawned.ValueRO.LifeRemaining - deltaTime;

                if (life <= 0)
                {
                    ecb.DestroyEntity(entity);
                }
                else
                {
                    spawned.ValueRW.LifeRemaining = life;
                    var scale = math.lerp(0.5f, 1.5f, math.sin(life * 3f) * 0.5f + 0.5f);
                    transform.ValueRW.Scale = scale;
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
