using Unity.Entities;

namespace Counter.Data.Components
{
    public struct CounterComponent : IComponentData
    {
        public int Value;
    }
}