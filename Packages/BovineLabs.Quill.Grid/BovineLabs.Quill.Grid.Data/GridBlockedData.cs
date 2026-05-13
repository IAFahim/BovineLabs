using Unity.Entities;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridBlockedData : IBufferElementData
    {
        public byte Value;

        public GridBlockedData(byte value)
        {
            Value = value;
        }
    }

    public struct GridVisualizerTag : IComponentData
    {
    }
}