using Examples._01_AssemblyArchitecture.Script.Counter.Data;
using Unity.Burst;
using Unity.Entities;

namespace Examples._01_AssemblyArchitecture.Script.Counter.Systems
{
    [BurstCompile]
    public partial struct IncrementCounterSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new IncrementCounterJob().ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    internal partial struct IncrementCounterJob : IJobEntity
    {
        private void Execute(ref CounterComponent counterComponent)
        {
            counterComponent.Value++;
        }
    }
}