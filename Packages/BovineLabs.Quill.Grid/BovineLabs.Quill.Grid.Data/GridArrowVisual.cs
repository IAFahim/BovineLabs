using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridArrowVisual : IBufferElementData
    {
        public float3 From;
        public float3 To;
        public float4 Color;
        public float Magnitude;
        public int Frame;

        public GridArrowVisual(float3 from, float3 to, float4 color, float magnitude, int frame = 0)
        {
            From = from;
            To = to;
            Color = color;
            Magnitude = magnitude;
            Frame = frame;
        }
    }
}