using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid
{
    public enum CellState : byte
    {
        Free = 0,
        Blocked = 1
    }


    public struct NativeGrid2D : IDisposable
    {
        public NativeArray<CellState> Cells;
        public int Width;
        public int Height;

        public NativeGrid2D(int width, int height, Allocator allocator)
        {
            Width = width;
            Height = height;
            Cells = new NativeArray<CellState>(width * height, allocator);
        }

        public NativeGrid2D(CellState[] data, int width, int height, Allocator allocator)
        {
            Width = width;
            Height = height;
            Cells = new NativeArray<CellState>(data.Length, allocator);
            Cells.CopyFrom(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int Index(int x, int y)
        {
            return y * Width + x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool InBounds(int2 pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsFree(int2 pos)
        {
            return InBounds(pos) && Cells[Index(pos.x, pos.y)] == CellState.Free;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Set(int x, int y, CellState state)
        {
            Cells[Index(x, y)] = state;
        }

        public unsafe void Dispose()
        {
            if (Cells.IsCreated) Cells.Dispose();
        }
    }


    public struct PathNode : IEquatable<PathNode>, IComparable<PathNode>
    {
        public int2 Position;
        public float GCost;
        public float FCost;
        public int ParentIndex;

        public int CompareTo(PathNode other)
        {
            return FCost.CompareTo(other.FCost);
        }

        public bool Equals(PathNode other)
        {
            return Position.Equals(other.Position);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }


    public struct PathResult : IDisposable
    {
        public NativeList<int2> Path;
        public bool Found;
        public float PathCost;
        public int NodesExplored;

        public PathResult(Allocator allocator)
        {
            Path = new NativeList<int2>(256, allocator);
            Found = false;
            PathCost = 0f;
            NodesExplored = 0;
        }

        public unsafe void Dispose()
        {
            if (Path.IsCreated) Path.Dispose();
        }
    }


    public static class GridNeighbors
    {
        public const float CardinalCost = 1f;
        public const float DiagonalCost = 1.4142135f;

        public static readonly int2[] Cardinal =
        {
            new(0, -1),
            new(1, 0),
            new(0, 1),
            new(-1, 0)
        };

        public static readonly int2[] Diagonal =
        {
            new(1, -1),
            new(1, 1),
            new(-1, 1),
            new(-1, -1)
        };


        public static int GetNeighbors4(in Grid2D grid, int cell, NativeArray<int> neighbors, NativeArray<byte> blocked)
        {
            var count = 0;
            var p = grid.ToCoord(cell);

            for (var d = 0; d < 4; d++)
            {
                var n = p + Grid2D.Dir4(d);
                if (grid.InBounds(n))
                {
                    var ni = grid.ToIndex(n);
                    if (blocked[ni] == 0)
                        neighbors[count++] = ni;
                }
            }

            return count;
        }


        public static int GetNeighbors8(in Grid2D grid, int cell, NativeArray<int> neighbors, NativeArray<byte> blocked)
        {
            var count = 0;
            var p = grid.ToCoord(cell);

            for (var d = 0; d < 8; d++)
            {
                var n = p + Grid2D.Dir8(d);
                if (grid.InBounds(n))
                {
                    var ni = grid.ToIndex(n);
                    if (blocked[ni] == 0)
                        neighbors[count++] = ni;
                }
            }

            return count;
        }


        public static bool IsDiagonalPassable(in Grid2D grid, int2 from, int2 dir, NativeArray<byte> blocked)
        {
            var adjA = new int2(from.x + dir.x, from.y);
            var adjB = new int2(from.x, from.y + dir.y);

            if (!grid.InBounds(adjA) || !grid.InBounds(adjB)) return false;
            return blocked[grid.ToIndex(adjA)] == 0 && blocked[grid.ToIndex(adjB)] == 0;
        }
    }


    [BurstCompile]
    public static class GridHeuristics
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Euclidean(int2 a, int2 b)
        {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            return math.sqrt(dx * dx + dy * dy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Octile(int2 a, int2 b)
        {
            var dx = math.abs(a.x - b.x);
            var dy = math.abs(a.y - b.y);
            return math.max(dx, dy) + 0.4142135f * math.min(dx, dy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Manhattan(int2 a, int2 b)
        {
            return math.abs(a.x - b.x) + math.abs(a.y - b.y);
        }
    }
}