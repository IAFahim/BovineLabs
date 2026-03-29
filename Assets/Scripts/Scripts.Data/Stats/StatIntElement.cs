using Unity.Entities;

namespace Scripts.Data.Stats
{
    [InternalBufferCapacity(0)]
    public struct StatIntElement : IBufferElementData
    {
        public ushort Id;
        public ushort LinkId;
        public StatIntView View;
        public int Value;
    }
}
