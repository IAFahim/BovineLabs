namespace Scripts.SpinLockExample
{
    using Scripts.Data.SpinLockExample;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SpinLockSystem : ISystem
    {
        private NativeArray<int> lockValue;
        private uint lockHolder;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpinLockConfig>();
            this.lockValue = new NativeArray<int>(1, Allocator.Persistent);
            this.lockValue[0] = 0;
            this.lockHolder = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<SpinLockController>())
            {
                return;
            }

            ref var controller = ref SystemAPI.GetSingletonRW<SpinLockController>().ValueRW;
            var config = SystemAPI.GetSingleton<SpinLockConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            controller.Timer += SystemAPI.Time.DeltaTime;

            if (controller.Timer >= config.LockInterval)
            {
                controller.Timer = 0f;

                for (int i = 0; i < config.WriterCount; i++)
                {
                    var writerEntity = ecb.Instantiate(config.WriterPrefab);
                    var writerId = (uint)(controller.TotalLockAttempts + i + 1);
                    ecb.SetComponent(writerEntity, LocalTransform.FromPosition(new float3(i * 2f, 2f, 0)));
                    ecb.SetComponent(writerEntity, new URPMaterialPropertyBaseColor { Value = new float4(1f, 0.2f, 0.2f, 1f) });
                    ecb.AddComponent(writerEntity, new SpinLockWriter
                    {
                        WriterId = (int)writerId,
                        LockAttemptTime = (float)SystemAPI.Time.ElapsedTime
                    });
                    controller.TotalLockAttempts++;
                }

                for (int i = 0; i < config.ReaderCount; i++)
                {
                    var readerEntity = ecb.Instantiate(config.ReaderPrefab);
                    var readerId = (uint)(controller.TotalLockAttempts + config.WriterCount + i + 1);
                    ecb.SetComponent(readerEntity, LocalTransform.FromPosition(new float3(i * 2f, -2f, 0)));
                    ecb.SetComponent(readerEntity, new URPMaterialPropertyBaseColor { Value = new float4(0.2f, 0.2f, 1f, 1f) });
                    ecb.AddComponent(readerEntity, new SpinLockReader
                    {
                        ReaderId = (int)readerId,
                        LockAttemptTime = (float)SystemAPI.Time.ElapsedTime
                    });
                    controller.TotalLockAttempts++;
                }
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (writer, entity) in SystemAPI.Query<RefRW<SpinLockWriter>>().WithEntityAccess())
            {
                var elapsed = currentTime - writer.ValueRO.LockAttemptTime;

                if (elapsed > 2f)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                if (this.lockHolder == 0)
                {
                    this.lockHolder = (uint)writer.ValueRO.WriterId;
                    this.lockValue[0] = writer.ValueRO.WriterId;
                    writer.ValueRW.WriterId = -writer.ValueRO.WriterId;
                    controller.SuccessfulLocks++;
                }
                else
                {
                    controller.FailedLocks++;
                }
            }

            foreach (var (reader, entity) in SystemAPI.Query<RefRW<SpinLockReader>>().WithEntityAccess())
            {
                var elapsed = currentTime - reader.ValueRO.LockAttemptTime;

                if (elapsed > 2f)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var readValue = this.lockValue[0];
                if (readValue != 0)
                {
                    reader.ValueRW.ReaderId = -reader.ValueRO.ReaderId;
                }
            }

            if (currentTime % 1f < 0.1f)
            {
                this.lockHolder = 0;
                this.lockValue[0] = 0;
            }

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (this.lockValue.IsCreated)
            {
                this.lockValue.Dispose();
            }
        }
    }
}
