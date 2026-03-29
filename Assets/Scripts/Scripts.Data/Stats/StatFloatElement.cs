using Unity.Entities;

namespace Scripts.Data.Stats
{
    [InternalBufferCapacity(0)]
    public struct StatFloatElement : IBufferElementData
    {
        public ushort Id;
        public ushort LinkId;
        public StatFloatView View;
        public float Value;
    }
}
