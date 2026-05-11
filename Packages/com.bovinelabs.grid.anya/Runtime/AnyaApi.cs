using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Anya
{
    /// <summary>
    /// Any-angle pathfinding using Theta* algorithm with line-of-sight optimization.
    /// Uses A* on grid with lazy line-of-sight checks to parent's parent for path shortening.
    /// </summary>
    public struct AnyaState
    {
        public Grid2D Grid;
        public MinHeap Heap;
    }

    public static class AnyaApi
    {
        public static AnyaState Create(int width, int height, int maxNodes, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new AnyaState
            {
                Grid = g,
                Heap = MinHeap.Create(maxNodes, a),
            };
        }

        public static bool Search(
            ref AnyaState s,
            NativeArray<byte> blocked,
            int2 start,
            int2 goal,
            NativeList<int2> path)
        {
            path.Clear();
            s.Heap.Clear();

            if (!s.Grid.InBounds(start) || !s.Grid.InBounds(goal)) return false;
            int si = s.Grid.ToIndex(start);
            int gi = s.Grid.ToIndex(goal);
            if (blocked[si] != 0 || blocked[gi] != 0) return false;

            // Direct line-of-sight check first
            if (LineOfSight(s.Grid, blocked, start, goal))
            {
                path.Add(start);
                path.Add(goal);
                return true;
            }

            // Theta*: A* with any-angle optimization
            var g = new NativeArray<float>(s.Grid.Length, Allocator.Temp);
            var parent = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var closed = new NativeArray<byte>(s.Grid.Length, Allocator.Temp);
            g.Fill(float.PositiveInfinity);
            parent.Fill(-1);
            closed.Fill((byte)0);

            g[si] = 0f;
            s.Heap.InsertOrDecrease(new HeapNode(si, Grid2D.HeuristicEuclidean(start, goal)));

            while (!s.Heap.IsEmpty)
            {
                int u = s.Heap.Pop().Id;
                if (u == gi)
                {
                    // Extract path
                    int cur = gi;
                    while (cur >= 0) { path.Add(s.Grid.ToCoord(cur)); cur = parent[cur]; }
                    for (int i = 0; i < path.Length / 2; i++)
                    {
                        var tmp = path[i]; path[i] = path[path.Length - 1 - i]; path[path.Length - 1 - i] = tmp;
                    }
                    break;
                }

                closed[u] = 1;
                int2 uCoord = s.Grid.ToCoord(u);

                int2 up = s.Grid.ToCoord(u);
                for (int d = 0; d < 8; d++)
                {
                    int2 np = up + Grid2D.Directions8[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (blocked[ni] != 0 || closed[ni] != 0) continue;

                    float cost = (d < 4) ? 1f : 1.414f;

                    // Theta* optimization: try line-of-sight from parent[u] to neighbor
                    if (parent[u] >= 0)
                    {
                        int2 parentCoord = s.Grid.ToCoord(parent[u]);
                        float parentG = g[parent[u]];
                        float losCost = math.length(new float2(np.x - parentCoord.x, np.y - parentCoord.y));

                        if (LineOfSight(s.Grid, blocked, parentCoord, np) && parentG + losCost < g[ni])
                        {
                            g[ni] = parentG + losCost;
                            parent[ni] = parent[u];
                            float f = g[ni] + Grid2D.HeuristicEuclidean(np, goal);
                            s.Heap.InsertOrDecrease(new HeapNode(ni, f));
                            continue;
                        }
                    }

                    float newG = g[u] + cost;
                    if (newG < g[ni])
                    {
                        g[ni] = newG;
                        parent[ni] = u;
                        float f = newG + Grid2D.HeuristicEuclidean(np, goal);
                        s.Heap.InsertOrDecrease(new HeapNode(ni, f));
                    }
                }
            }

            g.Dispose(); parent.Dispose(); closed.Dispose();
            return path.Length > 0;
        }

        public static bool LineOfSight(Grid2D grid, NativeArray<byte> blocked, int2 from, int2 to)
        {
            int dx = math.abs(to.x - from.x);
            int dy = math.abs(to.y - from.y);
            int sx = from.x < to.x ? 1 : -1;
            int sy = from.y < to.y ? 1 : -1;
            int err = dx - dy;
            int x = from.x, y = from.y;

            while (true)
            {
                if (blocked[grid.ToIndex(x, y)] != 0) return false;
                if (x == to.x && y == to.y) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
                if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) return false;
            }
            return true;
        }

        public static void Dispose(ref AnyaState s)
        {
            if (s.Heap.IsCreated) s.Heap.Dispose();
        }
    }
}
