using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid
{
    /// <summary>
    /// A cell in a 2D grid. 0 = free, 1 = blocked.
    /// </summary>
    public enum CellState : byte
    {
        Free = 0,
        Blocked = 1,
    }

    /// <summary>
    /// Burst-compatible 2D grid stored as a flat NativeArray.
    /// Row-major: index = y * width + x.
    /// </summary>
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
        public readonly int Index(int x, int y) => y * Width + x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool InBounds(int2 pos) =>
            pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsFree(int2 pos) =>
            InBounds(pos) && Cells[Index(pos.x, pos.y)] == CellState.Free;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int x, int y, CellState state) =>
            Cells[Index(x, y)] = state;

        public void Dispose()
        {
            if (Cells.IsCreated) Cells.Dispose();
        }
    }

    /// <summary>
    /// A single node in a pathfinding search.
    /// Stores position, cost, and parent for path reconstruction.
    /// </summary>
    public struct PathNode : IEquatable<PathNode>, IComparable<PathNode>
    {
        public int2 Position;
        public float GCost;       // Cost from start
        public float FCost;       // G + heuristic
        public int ParentIndex;   // Index in the node array (-1 = none)

        public int CompareTo(PathNode other) => FCost.CompareTo(other.FCost);
        public bool Equals(PathNode other) => Position.Equals(other.Position);
        public override int GetHashCode() => Position.GetHashCode();
    }

    /// <summary>
    /// Result of a pathfinding query.
    /// </summary>
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

        public void Dispose()
        {
            if (Path.IsCreated) Path.Dispose();
        }
    }

    /// <summary>
    /// 8-directional grid neighbor offsets (cardinal + diagonal).
    /// </summary>
    public static class GridNeighbors
    {
        public static readonly int2[] Cardinal = {
            new int2(0, -1),  // N
            new int2(1, 0),   // E
            new int2(0, 1),   // S
            new int2(-1, 0),  // W
        };

        public static readonly int2[] Diagonal = {
            new int2(1, -1),   // NE
            new int2(1, 1),    // SE
            new int2(-1, 1),   // SW
            new int2(-1, -1),  // NW
        };

        public const float CardinalCost = 1f;
        public const float DiagonalCost = 1.4142135f; // sqrt(2)

        /// <summary>Iterate 4-connected neighbors. Returns count of valid neighbors written.</summary>
        public static int GetNeighbors4(in Grid2D grid, int cell, NativeArray<int> neighbors, NativeArray<byte> blocked)
        {
            int count = 0;
            int2 p = grid.ToCoord(cell);

            for (int d = 0; d < 4; d++)
            {
                int2 n = p + Grid2D.Directions4[d];
                if (grid.InBounds(n))
                {
                    int ni = grid.ToIndex(n);
                    if (blocked[ni] == 0)
                        neighbors[count++] = ni;
                }
            }

            return count;
        }

        /// <summary>Iterate 8-connected neighbors. Returns count of valid neighbors written.</summary>
        public static int GetNeighbors8(in Grid2D grid, int cell, NativeArray<int> neighbors, NativeArray<byte> blocked)
        {
            int count = 0;
            int2 p = grid.ToCoord(cell);

            for (int d = 0; d < 8; d++)
            {
                int2 n = p + Grid2D.Directions8[d];
                if (grid.InBounds(n))
                {
                    int ni = grid.ToIndex(n);
                    if (blocked[ni] == 0)
                        neighbors[count++] = ni;
                }
            }

            return count;
        }

        /// <summary>Check diagonal passability (no corner cutting).</summary>
        public static bool IsDiagonalPassable(in Grid2D grid, int2 from, int2 dir, NativeArray<byte> blocked)
        {
            int2 adjA = new int2(from.x + dir.x, from.y);
            int2 adjB = new int2(from.x, from.y + dir.y);

            if (!grid.InBounds(adjA) || !grid.InBounds(adjB)) return false;
            return blocked[grid.ToIndex(adjA)] == 0 && blocked[grid.ToIndex(adjB)] == 0;
        }
    }

    /// <summary>
    /// Common heuristic functions for pathfinding.
    /// </summary>
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
