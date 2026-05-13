using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridTextVisual : IBufferElementData
    {
        public float3 Position;
        public FixedString64Bytes Text;
        public float4 Color;
        public int Frame;

        public GridTextVisual(float3 position, FixedString64Bytes text, float4 color, int frame = 0)
        {
            Position = position;
            Text = text;
            Color = color;
            Frame = frame;
        }
    }
}