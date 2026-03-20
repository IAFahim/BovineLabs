using Unity.Entities;

namespace Examples._01_AssemblyArchitecture.Script.Counter.Data
{
    public struct CounterComponent : IComponentData
    {
        public int Value;
    }
}