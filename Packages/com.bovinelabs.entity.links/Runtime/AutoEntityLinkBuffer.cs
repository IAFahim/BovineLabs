using Unity.Entities;

namespace BovineLabs.EntityLinks
{
    public struct AutoEntityLinkBuffer : IBufferElementData
    {
        public byte Key;
        public Entity Value;
    }
}
