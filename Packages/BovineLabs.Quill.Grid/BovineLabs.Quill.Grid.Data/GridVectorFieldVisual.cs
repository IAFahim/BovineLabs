using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridVectorFieldVisual : IBufferElementData
    {
        public int Cell;
        public float2 Direction;
        public float Magnitude;
        public int Frame;

        public GridVectorFieldVisual(int cell, float2 direction, float magnitude, int frame = 0)
        {
            Cell = cell;
            Direction = direction;
            Magnitude = magnitude;
            Frame = frame;
        }
    }
}