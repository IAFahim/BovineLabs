using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Rsr
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RsrRect
    {
        public int2 Min;
        public int2 Max;
        public int PerimeterOffset;
        public int PerimeterCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RsrState
    {
        public Grid2D Grid;
        public NativeArray<int> RectOfCell;
        public UnsafeList<RsrRect> Rects;
        public UnsafeList<int> PerimeterCells;
    }

    [BurstCompile]
    public unsafe static class RsrApi
    {
        public static RsrState Create(int width, int height, int maxRects, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new RsrState
            {
                Grid = g,
                RectOfCell = new NativeArray<int>(g.Length, a),
                Rects = new UnsafeList<RsrRect>(maxRects, a),
                PerimeterCells = new UnsafeList<int>(g.Length, a),
            };
        }

        [BurstCompile]
        public static void Build(ref RsrState s, NativeArray<byte> blocked)
        {
            byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            int* roc = (int*)s.RectOfCell.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;

            s.Rects.Clear();
            s.PerimeterCells.Clear();
            for (int i = 0; i < len; i++) roc[i] = -1;

            var used = new NativeArray<byte>(len, Allocator.Temp);
            byte* usd = (byte*)used.GetUnsafePtr();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (blk[idx] != 0 || usd[idx] != 0) continue;

                    int maxW = 1;
                    while (x + maxW < w)
                    {
                        if (blk[y * w + (x + maxW)] != 0 || usd[y * w + (x + maxW)] != 0) break;
                        maxW++;
                    }

                    int maxH = 1;
                    while (y + maxH < h)
                    {
                        bool allFree = true;
                        for (int dx = 0; dx < maxW; dx++)
                        {
                            if (blk[(y + maxH) * w + (x + dx)] != 0 || usd[(y + maxH) * w + (x + dx)] != 0)
                            { allFree = false; break; }
                        }
                        if (!allFree) break;
                        maxH++;
                    }

                    for (int dy = 0; dy < maxH; dy++)
                        for (int dx = 0; dx < maxW; dx++)
                            usd[(y + dy) * w + (x + dx)] = 1;

                    int rectId = s.Rects.Length;
                    int perimOffset = s.PerimeterCells.Length;

                    for (int dx = 0; dx < maxW; dx++)
                    {
                        s.PerimeterCells.Add(y * w + (x + dx));
                        if (maxH > 1) s.PerimeterCells.Add((y + maxH - 1) * w + (x + dx));
                    }
                    for (int dy = 1; dy < maxH - 1; dy++)
                    {
                        s.PerimeterCells.Add((y + dy) * w + x);
                        if (maxW > 1) s.PerimeterCells.Add((y + dy) * w + (x + maxW - 1));
                    }

                    for (int dy = 0; dy < maxH; dy++)
                        for (int dx = 0; dx < maxW; dx++)
                            roc[(y + dy) * w + (x + dx)] = rectId;

                    s.Rects.Add(new RsrRect
                    {
                        Min = new int2(x, y),
                        Max = new int2(x + maxW - 1, y + maxH - 1),
                        PerimeterOffset = perimOffset,
                        PerimeterCount = s.PerimeterCells.Length - perimOffset,
                    });
                }
            }

            // Handle remaining unassigned free cells
            for (int i = 0; i < len; i++)
            {
                if (blk[i] != 0 || roc[i] >= 0) continue;
                int rectId = s.Rects.Length;
                roc[i] = rectId;
                int perimOffset = s.PerimeterCells.Length;
                s.PerimeterCells.Add(i);
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

        [BurstCompile]
        public static void GetSuccessors(ref RsrState s, int cell, NativeArray<byte> blocked, NativeList<int> successors)
        {
            successors.Clear();
            int rectId = s.RectOfCell[cell];
            if (rectId < 0 || rectId >= s.Rects.Length) return;

            var rect = s.Rects.Ptr[rectId];
            int cx = cell % s.Grid.Width;
            int cy = cell / s.Grid.Width;
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            bool onPerimeter = cx == rect.Min.x || cx == rect.Max.x || cy == rect.Min.y || cy == rect.Max.y;

            if (!onPerimeter)
            {
                for (int i = 0; i < rect.PerimeterCount; i++)
                    successors.Add(s.PerimeterCells.Ptr[rect.PerimeterOffset + i]);
            }
            else
            {
                for (int i = 0; i < rect.PerimeterCount; i++)
                {
                    int perimCell = s.PerimeterCells.Ptr[rect.PerimeterOffset + i];
                    if (perimCell != cell) successors.Add(perimCell);
                }

                byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
                int2 p = new int2(cx, cy);
                if (cx + 1 < w && blk[cy * w + cx + 1] == 0 && s.RectOfCell[cy * w + cx + 1] != rectId) successors.Add(cy * w + cx + 1);
                if (cx > 0 && blk[cy * w + cx - 1] == 0 && s.RectOfCell[cy * w + cx - 1] != rectId) successors.Add(cy * w + cx - 1);
                if (cy + 1 < h && blk[(cy + 1) * w + cx] == 0 && s.RectOfCell[(cy + 1) * w + cx] != rectId) successors.Add((cy + 1) * w + cx);
                if (cy > 0 && blk[(cy - 1) * w + cx] == 0 && s.RectOfCell[(cy - 1) * w + cx] != rectId) successors.Add((cy - 1) * w + cx);
            }
        }

        public static void Dispose(ref RsrState s)
        {
            if (s.RectOfCell.IsCreated) s.RectOfCell.Dispose();
            s.Rects.Dispose();
            s.PerimeterCells.Dispose();
        }
    }
}
