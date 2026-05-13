using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridLineVisual : IBufferElementData
    {
        public float3 From;
        public float3 To;
        public float4 Color;
        public int Frame;

        public GridLineVisual(float3 from, float3 to, float4 color, int frame = 0)
        {
            From = from;
            To = to;
            Color = color;
            Frame = frame;
        }
    }
}