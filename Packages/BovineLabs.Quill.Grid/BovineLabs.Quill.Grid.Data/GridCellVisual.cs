using Unity.Entities;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridCellVisual : IBufferElementData
    {
        public int Cell;
        public float Value;
        public byte Layer;
        public int Frame;

        public const byte LayerObstacle = 0;
        public const byte LayerFrontier = 1;
        public const byte LayerClosed = 2;
        public const byte LayerPath = 3;
        public const byte LayerHeatmap = 4;
        public const byte LayerConflict = 5;
        public const byte LayerConstraint = 6;

        public GridCellVisual(int cell, float value, byte layer, int frame = 0)
        {
            Cell = cell;
            Value = value;
            Layer = layer;
            Frame = frame;
        }
    }
}