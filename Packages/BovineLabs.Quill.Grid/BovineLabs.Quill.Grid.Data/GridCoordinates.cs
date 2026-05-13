using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Data
{
    public readonly struct GridCoordinateConverter
    {
        public readonly float3 Origin;
        public readonly float CellSize;
        public readonly float CellHalfSize;
        public readonly int Width;
        public readonly int Height;

        public GridCoordinateConverter(float3 origin, float cellSize, int width, int height)
        {
            Origin = origin;
            CellSize = cellSize;
            CellHalfSize = cellSize * 0.5f;
            Width = width;
            Height = height;
        }

        public float3 CellCenter(int x, int y)
        {
            return new float3(
                Origin.x + x * CellSize + CellHalfSize,
                Origin.y,
                Origin.z + y * CellSize + CellHalfSize);
        }

        public float3 CellCenter(int index)
        {
            return CellCenter(index % Width, index / Width);
        }

        public float3 CellMin(int x, int y)
        {
            return new float3(
                Origin.x + x * CellSize,
                Origin.y,
                Origin.z + y * CellSize);
        }

        public float3 CellMax(int x, int y)
        {
            return new float3(
                Origin.x + (x + 1) * CellSize,
                Origin.y,
                Origin.z + (y + 1) * CellSize);
        }

        public float3 GridPoint(float x, float y, float height = 0f)
        {
            return new float3(
                Origin.x + x * CellSize,
                Origin.y + height,
                Origin.z + y * CellSize);
        }

        public float3 GridMin => Origin;
        public float3 GridMax => new(Origin.x + Width * CellSize, Origin.y, Origin.z + Height * CellSize);
    }
}