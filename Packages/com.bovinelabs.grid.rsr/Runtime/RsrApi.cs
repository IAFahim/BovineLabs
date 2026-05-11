using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Rsr
{
    public struct RsrRect
    {
        public int2 Min;
        public int2 Max;
        public int PerimeterOffset;
        public int PerimeterCount;
    }

    public struct RsrState
    {
        public Grid2D Grid;
        public NativeArray<int> RectOfCell;
        public NativeList<RsrRect> Rects;
        public NativeList<int> PerimeterCells;
    }

    public static class RsrApi
    {
        public static RsrState Create(int width, int height, int maxRects, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new RsrState
            {
                Grid = g,
                RectOfCell = new NativeArray<int>(g.Length, a),
                Rects = new NativeList<RsrRect>(maxRects, a),
                PerimeterCells = new NativeList<int>(g.Length, a),
            };
        }

        public static void Build(ref RsrState s, NativeArray<byte> blocked)
        {
            s.RectOfCell.Fill(-1);
            s.Rects.Clear();
            s.PerimeterCells.Clear();

            var used = new NativeArray<byte>(s.Grid.Length, Allocator.Temp);
            used.Fill((byte)0);

            for (int y = 0; y < s.Grid.Height; y++)
            {
                for (int x = 0; x < s.Grid.Width; x++)
                {
                    int idx = s.Grid.ToIndex(x, y);
                    if (blocked[idx] != 0 || used[idx] != 0) continue;

                    int maxW = 1;
                    while (x + maxW < s.Grid.Width)
                    {
                        bool allFree = true;
                        for (int dy = 0; dy < 1; dy++)
                        {
                            if (blocked[s.Grid.ToIndex(x + maxW, y + dy)] != 0 || used[s.Grid.ToIndex(x + maxW, y + dy)] != 0)
                            { allFree = false; break; }
                        }
                        if (!allFree) break;
                        maxW++;
                    }

                    int maxH = 1;
                    while (y + maxH < s.Grid.Height)
                    {
                        bool allFree = true;
                        for (int dx = 0; dx < maxW; dx++)
                        {
                            if (blocked[s.Grid.ToIndex(x + dx, y + maxH)] != 0 || used[s.Grid.ToIndex(x + dx, y + maxH)] != 0)
                            { allFree = false; break; }
                        }
                        if (!allFree) break;
                        maxH++;
                    }

                    for (int dy = 0; dy < maxH; dy++)
                        for (int dx = 0; dx < maxW; dx++)
                            used[s.Grid.ToIndex(x + dx, y + dy)] = 1;

                    int rectId = s.Rects.Length;
                    int perimOffset = s.PerimeterCells.Length;

                    for (int dx = 0; dx < maxW; dx++)
                    {
                        AddPerimeter(ref s, x + dx, y);
                        if (maxH > 1) AddPerimeter(ref s, x + dx, y + maxH - 1);
                    }
                    for (int dy = 1; dy < maxH - 1; dy++)
                    {
                        AddPerimeter(ref s, x, y + dy);
                        if (maxW > 1) AddPerimeter(ref s, x + maxW - 1, y + dy);
                    }

                    for (int dy = 0; dy < maxH; dy++)
                        for (int dx = 0; dx < maxW; dx++)
                            s.RectOfCell[s.Grid.ToIndex(x + dx, y + dy)] = rectId;

                    s.Rects.Add(new RsrRect
                    {
                        Min = new int2(x, y),
                        Max = new int2(x + maxW - 1, y + maxH - 1),
                        PerimeterOffset = perimOffset,
                        PerimeterCount = s.PerimeterCells.Length - perimOffset,
                    });
                }
            }

            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (blocked[i] != 0) continue;
                if (s.RectOfCell[i] >= 0) continue;
                int rectId = s.Rects.Length;
                s.RectOfCell[i] = rectId;
                int perimOffset = s.PerimeterCells.Length;
                AddPerimeter(ref s, s.Grid.ToCoord(i).x, s.Grid.ToCoord(i).y);
                s.Rects.Add(new RsrRect
                {
                    Min = s.Grid.ToCoord(i),
                    Max = s.Grid.ToCoord(i),
                    PerimeterOffset = perimOffset,
                    PerimeterCount = 1,
                });
            }

            used.Dispose();
        }

        private static void AddPerimeter(ref RsrState s, int x, int y)
        {
            s.PerimeterCells.Add(s.Grid.ToIndex(x, y));
        }

        public static void GetSuccessors(ref RsrState s, int cell, NativeArray<byte> blocked, NativeList<int> successors)
        {
            successors.Clear();
            int rectId = s.RectOfCell[cell];
            if (rectId < 0 || rectId >= s.Rects.Length) return;

            var rect = s.Rects[rectId];
            int2 p = s.Grid.ToCoord(cell);
            bool onPerimeter = (p.x == rect.Min.x || p.x == rect.Max.x || p.y == rect.Min.y || p.y == rect.Max.y);

            if (!onPerimeter)
            {
                for (int i = 0; i < rect.PerimeterCount; i++)
                    successors.Add(s.PerimeterCells[rect.PerimeterOffset + i]);
            }
            else
            {
                for (int i = 0; i < rect.PerimeterCount; i++)
                {
                    int perimCell = s.PerimeterCells[rect.PerimeterOffset + i];
                    if (perimCell == cell) continue;
                    successors.Add(perimCell);
                }
                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (blocked[ni] != 0) continue;
                    if (s.RectOfCell[ni] != rectId)
                        successors.Add(ni);
                }
            }
        }

        public static void Dispose(ref RsrState s)
        {
            if (s.RectOfCell.IsCreated) s.RectOfCell.Dispose();
            if (s.Rects.IsCreated) s.Rects.Dispose();
            if (s.PerimeterCells.IsCreated) s.PerimeterCells.Dispose();
        }
    }
}
