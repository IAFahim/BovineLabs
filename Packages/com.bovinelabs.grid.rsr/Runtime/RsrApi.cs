using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

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
    public unsafe struct RsrState
    {
        public Grid2D Grid;
        public int* RectOfCell;
        public UnsafeList<RsrRect> Rects;
        public UnsafeList<int> PerimeterCells;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class RsrApi
    {
        public static bool TryCreate(int width, int height, int maxRects, Allocator a, out RsrState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new RsrState
            {
                Allocator = a,
                Grid = g,
                RectOfCell = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length),
                Rects = new UnsafeList<RsrRect>(maxRects, a),
                PerimeterCells = new UnsafeList<int>(g.Length, a)
            };
            return true;
        }

        [BurstCompile]
        public static bool TryBuild(ref RsrState s, in NativeArray<byte> blocked)
        {
            var blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            var roc = s.RectOfCell;
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var len = s.Grid.Length;

            s.Rects.Clear();
            s.PerimeterCells.Clear();
            for (var i = 0; i < len; i++) roc[i] = -1;

            var used = new Unity.Collections.NativeArray<byte>(len, Allocator.Temp);
            var usd = (byte*)used.GetUnsafePtr();

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var idx = y * w + x;
                if (blk[idx] != 0 || usd[idx] != 0) continue;

                var maxW = 1;
                while (x + maxW < w)
                {
                    if (blk[y * w + x + maxW] != 0 || usd[y * w + x + maxW] != 0) break;
                    maxW++;
                }

                var maxH = 1;
                while (y + maxH < h)
                {
                    var allFree = true;
                    for (var dx = 0; dx < maxW; dx++)
                        if (blk[(y + maxH) * w + x + dx] != 0 || usd[(y + maxH) * w + x + dx] != 0)
                        {
                            allFree = false;
                            break;
                        }

                    if (!allFree) break;
                    maxH++;
                }

                for (var dy = 0; dy < maxH; dy++)
                for (var dx = 0; dx < maxW; dx++)
                    usd[(y + dy) * w + x + dx] = 1;

                var rectId = s.Rects.Length;
                var perimOffset = s.PerimeterCells.Length;

                for (var dx = 0; dx < maxW; dx++)
                {
                    s.PerimeterCells.Add(y * w + x + dx);
                    if (maxH > 1) s.PerimeterCells.Add((y + maxH - 1) * w + x + dx);
                }

                for (var dy = 1; dy < maxH - 1; dy++)
                {
                    s.PerimeterCells.Add((y + dy) * w + x);
                    if (maxW > 1) s.PerimeterCells.Add((y + dy) * w + (x + maxW - 1));
                }

                for (var dy = 0; dy < maxH; dy++)
                for (var dx = 0; dx < maxW; dx++)
                    roc[(y + dy) * w + x + dx] = rectId;

                s.Rects.Add(new RsrRect
                {
                    Min = new int2(x, y),
                    Max = new int2(x + maxW - 1, y + maxH - 1),
                    PerimeterOffset = perimOffset,
                    PerimeterCount = s.PerimeterCells.Length - perimOffset
                });
            }

            for (var i = 0; i < len; i++)
            {
                if (blk[i] != 0 || roc[i] >= 0) continue;
                var rectId = s.Rects.Length;
                roc[i] = rectId;
                var perimOffset = s.PerimeterCells.Length;
                s.PerimeterCells.Add(i);
                s.Rects.Add(new RsrRect
                {
                    Min = s.Grid.ToCoord(i),
                    Max = s.Grid.ToCoord(i),
                    PerimeterOffset = perimOffset,
                    PerimeterCount = 1
                });
            }

            used.Dispose();
            return true;
        }

        [BurstCompile]
        public static void GetSuccessors(ref RsrState s, int cell, in NativeArray<byte> blocked,
            ref NativeList<int> successors)
        {
            TryGetSuccessors(ref s, cell, in blocked, ref successors);
        }

        [BurstCompile]
        public static bool TryGetSuccessors(ref RsrState s, int cell, in NativeArray<byte> blocked,
            ref NativeList<int> successors)
        {
            successors.Clear();
            var roc = s.RectOfCell;
            var rectId = roc[cell];
            if (rectId < 0 || rectId >= s.Rects.Length) return false;

            var rect = s.Rects.Ptr[rectId];
            var cx = cell % s.Grid.Width;
            var cy = cell / s.Grid.Width;
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var onPerimeter = cx == rect.Min.x || cx == rect.Max.x || cy == rect.Min.y || cy == rect.Max.y;

            if (!onPerimeter)
            {
                for (var i = 0; i < rect.PerimeterCount; i++)
                    successors.Add(s.PerimeterCells.Ptr[rect.PerimeterOffset + i]);
            }
            else
            {
                for (var i = 0; i < rect.PerimeterCount; i++)
                {
                    var perimCell = s.PerimeterCells.Ptr[rect.PerimeterOffset + i];
                    if (perimCell != cell) successors.Add(perimCell);
                }

                var blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
                if (cx + 1 < w && blk[cy * w + cx + 1] == 0 && roc[cy * w + cx + 1] != rectId)
                    successors.Add(cy * w + cx + 1);
                if (cx > 0 && blk[cy * w + cx - 1] == 0 && roc[cy * w + cx - 1] != rectId)
                    successors.Add(cy * w + cx - 1);
                if (cy + 1 < h && blk[(cy + 1) * w + cx] == 0 && roc[(cy + 1) * w + cx] != rectId)
                    successors.Add((cy + 1) * w + cx);
                if (cy > 0 && blk[(cy - 1) * w + cx] == 0 && roc[(cy - 1) * w + cx] != rectId)
                    successors.Add((cy - 1) * w + cx);
            }

            return true;
        }

        public static void Dispose(ref RsrState s)
        {
            if (s.RectOfCell != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.RectOfCell); s.RectOfCell = null; }
            if (s.Rects.IsCreated) s.Rects.Dispose();
            if (s.PerimeterCells.IsCreated) s.PerimeterCells.Dispose();
        }
    }
}