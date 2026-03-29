using Unity.Entities;

namespace Scripts.Data.Stats
{
    [InternalBufferCapacity(0)]
    public struct StatValueElement : IBufferElementData
    {
        public ushort Id;
        public ushort LinkId;
        public StatDisplayStyle Style;
        public StatLinkType LinkType;
        public float Value;
    }
}
