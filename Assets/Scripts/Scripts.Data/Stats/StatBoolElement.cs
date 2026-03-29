using Unity.Entities;

namespace Scripts.Data.Stats
{
    [InternalBufferCapacity(0)]
    public struct StatBoolElement : IBufferElementData
    {
        public ushort Id;
        public StatBoolView View;
        public byte Value;
    }
}
