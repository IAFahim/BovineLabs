using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Subgoal
{
    public struct SubgoalEdge { public int To; public float Cost; }

    public struct SubgoalState
    {
        public Grid2D Grid;
        public UnsafeList<int> Subgoals;
        public NativeArray<int> SubgoalOfCell;
        public UnsafeList<SubgoalEdge> Edges;
        public UnsafeList<RangeI> EdgeRanges;
    }

    [BurstCompile]
    public unsafe static class SubgoalApi
    {
        private static readonly int2* DiagOffsets;

        static SubgoalApi()
        {
            // Initialized in a way that's Burst-compatible if needed, 
            // but for simplicity we'll just use the logic in the methods.
        }

        public static SubgoalState Create(int width, int height, int maxSubgoals, int maxEdges, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new SubgoalState
            {
                Grid = g,
                Subgoals = new UnsafeList<int>(maxSubgoals, a),
                SubgoalOfCell = new NativeArray<int>(g.Length, a),
                Edges = new UnsafeList<SubgoalEdge>(maxEdges, a),
                EdgeRanges = new UnsafeList<RangeI>(maxSubgoals, a),
            };
        }

        [BurstCompile]
        public static void Build(ref SubgoalState s, in NativeArray<byte> blocked)
        {
            s.Subgoals.Clear();
            s.SubgoalOfCell.Fill(-1);
            s.Edges.Clear();
            s.EdgeRanges.Clear();

            int width = s.Grid.Width;
            int height = s.Grid.Height;
            byte* blockedPtr = (byte*)blocked.GetUnsafeReadOnlyPtr();
            int* subgoalOfCellPtr = (int*)s.SubgoalOfCell.GetUnsafePtr();

            // 1. Identify subgoals (corners)
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (blockedPtr[i] != 0) continue;
                int y = i / width;
                int x = i % width;
                int2 p = new int2(x, y);
                bool isCorner = false;

                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (Hint.Unlikely(np.x < 0 || np.y < 0 || np.x >= width || np.y >= height)) continue;
                    
                    if (blockedPtr[np.y * width + np.x] != 0)
                    {
                        // Check diagonals
                        for (int dy = -1; dy <= 1; dy += 2)
                        {
                            for (int dx = -1; dx <= 1; dx += 2)
                            {
                                int2 dp = p + new int2(dx, dy);
                                if (dp.x >= 0 && dp.y >= 0 && dp.x < width && dp.y < height && blockedPtr[dp.y * width + dp.x] == 0)
                                {
                                    // If dp is a diagonal neighbor and the cardinal neighbor np is blocked, 
                                    // we might have a corner. Standard Subgoal logic is more specific,
                                    // but this is a reasonable approximation for "corner".
                                    isCorner = true;
                                    break;
                                }
                            }
                            if (isCorner) break;
                        }
                    }
                    if (isCorner) break;
                }

                if (isCorner)
                {
                    int id = s.Subgoals.Length;
                    s.Subgoals.Add(i);
                    subgoalOfCellPtr[i] = id;
                }
            }

            // 2. Build edges (line-of-sight between subgoals)
            for (int i = 0; i < s.Subgoals.Length; i++)
            {
                int edgeStart = s.Edges.Length;
                int2 pi = s.Grid.ToCoord(s.Subgoals[i]);

                for (int j = 0; j < s.Subgoals.Length; j++)
                {
                    if (i == j) continue;
                    int2 pj = s.Grid.ToCoord(s.Subgoals[j]);
                    if (LineOfSight(in s.Grid, blockedPtr, pi, pj))
                    {
                        s.Edges.Add(new SubgoalEdge { To = j, Cost = math.distance(pi, pj) });
                    }
                }
                s.EdgeRanges.Add(new RangeI(edgeStart, s.Edges.Length - edgeStart));
            }
        }

        [BurstCompile]
        public static bool Search(
            ref SubgoalState s,
            in NativeArray<byte> blocked,
            int start, int goal,
            ref NativeList<int> path)
        {
            path.Clear();
            int2 startCoord = s.Grid.ToCoord(start);
            int2 goalCoord = s.Grid.ToCoord(goal);
            byte* blockedPtr = (byte*)blocked.GetUnsafeReadOnlyPtr();

            if (LineOfSight(in s.Grid, blockedPtr, startCoord, goalCoord))
            {
                path.Add(start);
                path.Add(goal);
                return true;
            }

            int n = s.Subgoals.Length;
            int startNode = n;
            int goalNode = n + 1;
            int totalNodes = n + 2;

            var gArr = new NativeArray<float>(totalNodes, Allocator.Temp);
            var parentArr = new NativeArray<int>(totalNodes, Allocator.Temp);
            gArr.Fill(float.PositiveInfinity);
            parentArr.Fill(-1);

            var heap = MinHeap.Create(totalNodes, Allocator.Temp);
            gArr[startNode] = 0f;
            heap.InsertOrDecrease(new HeapNode(startNode, math.distance(startCoord, goalCoord)));

            while (!heap.IsEmpty)
            {
                int u = heap.Pop().Id;
                if (u == goalNode)
                {
                    ExtractPath(in s, start, goal, parentArr, goalNode, ref path);
                    gArr.Dispose(); parentArr.Dispose(); heap.Dispose();
                    return true;
                }

                int2 pu = (u < n) ? s.Grid.ToCoord(s.Subgoals[u]) : (u == startNode ? startCoord : goalCoord);

                // Successors:
                if (u == startNode)
                {
                    // Start can reach any visible subgoal or the goal
                    for (int v = 0; v < n; v++)
                    {
                        int2 pv = s.Grid.ToCoord(s.Subgoals[v]);
                        if (LineOfSight(in s.Grid, blockedPtr, pu, pv))
                            TryRelax(v, pu, pv, u, ref gArr, ref parentArr, ref heap, goalCoord);
                    }
                    if (LineOfSight(in s.Grid, blockedPtr, pu, goalCoord))
                        TryRelax(goalNode, pu, goalCoord, u, ref gArr, ref parentArr, ref heap, goalCoord);
                }
                else if (u < n)
                {
                    // Subgoal can reach precomputed neighbors or the goal if visible
                    RangeI range = s.EdgeRanges[u];
                    for (int i = range.Offset; i < range.Offset + range.Count; i++)
                    {
                        SubgoalEdge edge = s.Edges[i];
                        int2 pv = s.Grid.ToCoord(s.Subgoals[edge.To]);
                        TryRelax(edge.To, pu, pv, u, ref gArr, ref parentArr, ref heap, goalCoord);
                    }
                    if (LineOfSight(in s.Grid, blockedPtr, pu, goalCoord))
                        TryRelax(goalNode, pu, goalCoord, u, ref gArr, ref parentArr, ref heap, goalCoord);
                }
            }

            gArr.Dispose(); parentArr.Dispose(); heap.Dispose();
            return false;
        }

        private static void TryRelax(int v, int2 pu, int2 pv, int u, ref NativeArray<float> gArr, ref NativeArray<int> parentArr, ref MinHeap heap, int2 goalCoord)
        {
            float d = math.distance(pu, pv);
            float newG = gArr[u] + d;
            if (newG < gArr[v])
            {
                gArr[v] = newG;
                parentArr[v] = u;
                heap.InsertOrDecrease(new HeapNode(v, newG + math.distance(pv, goalCoord)));
            }
        }

        private static void ExtractPath(in SubgoalState s, int start, int goal, in NativeArray<int> parentArr, int goalNode, ref NativeList<int> path)
        {
            var temp = new NativeList<int>(Allocator.Temp);
            int cur = goalNode;
            while (cur != -1)
            {
                if (cur < s.Subgoals.Length) temp.Add(s.Subgoals[cur]);
                else if (cur == s.Subgoals.Length) temp.Add(start);
                else temp.Add(goal);
                cur = parentArr[cur];
            }
            for (int i = temp.Length - 1; i >= 0; i--) path.Add(temp[i]);
            temp.Dispose();
        }

        [BurstCompile]
        public static bool LineOfSight(in Grid2D grid, byte* blocked, int2 from, int2 to)
        {
            int dx = math.abs(to.x - from.x);
            int dy = math.abs(to.y - from.y);
            int sx = from.x < to.x ? 1 : -1;
            int sy = from.y < to.y ? 1 : -1;
            int err = dx - dy;
            int x = from.x, y = from.y;
            int width = grid.Width;
            int height = grid.Height;

            while (true)
            {
                if (Hint.Unlikely(x < 0 || y < 0 || x >= width || y >= height)) return false;
                if (blocked[y * width + x] != 0) return false;
                if (x == to.x && y == to.y) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
            return true;
        }

        public static void Dispose(ref SubgoalState s)
        {
            s.Subgoals.Dispose();
            if (s.SubgoalOfCell.IsCreated) s.SubgoalOfCell.Dispose();
            s.Edges.Dispose();
            s.EdgeRanges.Dispose();
        }
    }
}
