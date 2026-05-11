using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Subgoal
{
    public struct SubgoalEdge { public int To; public float Cost; }

    public struct SubgoalState
    {
        public Grid2D Grid;
        public NativeList<int> Subgoals;
        public NativeArray<int> SubgoalOfCell;
        public NativeList<SubgoalEdge> Edges;
        public NativeList<RangeI> EdgeRanges;
    }

    public static class SubgoalApi
    {
        // Diagonal offsets for each cardinal direction's corresponding diagonal
        // When a cardinal neighbor is blocked, check if the diagonal perpendicular to it is free
        // For dir 0 (right), check diagonal up-right (1) and down-right (7)
        // For dir 1 (down), check diagonal down-left (6) and down-right (7) -- wait, Directions4[1] = up
        // Let's use explicit pairs: for each cardinal d, the two diagonals that share a component
        private static readonly int2[] DiagOffsets =
        {
            new int2(1, 1), new int2(1, -1),   // for right (1,0): NE and SE
            new int2(-1, 1), new int2(1, 1),    // for up (0,1): NW and NE
            new int2(-1, 1), new int2(-1, -1),  // for left (-1,0): NW and SW
            new int2(-1, -1), new int2(1, -1),  // for down (0,-1): SW and SE
        };

        public static SubgoalState Create(int width, int height, int maxSubgoals, int maxEdges, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new SubgoalState
            {
                Grid = g,
                Subgoals = new NativeList<int>(maxSubgoals, a),
                SubgoalOfCell = new NativeArray<int>(g.Length, a),
                Edges = new NativeList<SubgoalEdge>(maxEdges, a),
                EdgeRanges = new NativeList<RangeI>(maxSubgoals, a),
            };
        }

        public static void Build(ref SubgoalState s, NativeArray<byte> blocked)
        {
            s.Subgoals.Clear();
            s.SubgoalOfCell.Fill(-1);
            s.Edges.Clear();
            s.EdgeRanges.Clear();

            // Find corner subgoals: free cells adjacent to an obstacle where a diagonal is also free
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (blocked[i] != 0) continue;
                int2 p = s.Grid.ToCoord(i);
                bool isCorner = false;

                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    if (blocked[s.Grid.ToIndex(np)] != 0)
                    {
                        // Check both diagonals for this cardinal direction
                        int2 d1 = p + DiagOffsets[d * 2];
                        int2 d2 = p + DiagOffsets[d * 2 + 1];
                        if ((s.Grid.InBounds(d1) && blocked[s.Grid.ToIndex(d1)] == 0) ||
                            (s.Grid.InBounds(d2) && blocked[s.Grid.ToIndex(d2)] == 0))
                        {
                            isCorner = true;
                            break;
                        }
                    }
                }

                if (isCorner)
                {
                    int id = s.Subgoals.Length;
                    s.Subgoals.Add(i);
                    s.SubgoalOfCell[i] = id;
                }
            }

            // Add edges between line-of-sight visible subgoals
            for (int i = 0; i < s.Subgoals.Length; i++)
            {
                int edgeStart = s.Edges.Length;
                int2 pi = s.Grid.ToCoord(s.Subgoals[i]);

                for (int j = i + 1; j < s.Subgoals.Length; j++)
                {
                    int2 pj = s.Grid.ToCoord(s.Subgoals[j]);
                    if (LineOfSight(s.Grid, blocked, pi, pj))
                    {
                        float cost = math.length(new float2(pj.x - pi.x, pj.y - pi.y));
                        s.Edges.Add(new SubgoalEdge { To = j, Cost = cost });
                        s.Edges.Add(new SubgoalEdge { To = i, Cost = cost });
                    }
                }

                s.EdgeRanges.Add(new RangeI(edgeStart, s.Edges.Length - edgeStart));
            }
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
            }
            return true;
        }

        public static bool Search(ref SubgoalState s, NativeArray<byte> blocked, int start, int goal, NativeList<int> path)
        {
            path.Clear();

            // Use subgoal graph for search: add start/goal as temporary subgoals, search the graph
            int2 startCoord = s.Grid.ToCoord(start);
            int2 goalCoord = s.Grid.ToCoord(goal);

            // Direct line-of-sight check first
            if (LineOfSight(s.Grid, blocked, startCoord, goalCoord))
            {
                path.Add(start);
                path.Add(goal);
                return true;
            }

            // Build temporary node list: existing subgoals + start + goal
            int n = s.Subgoals.Length;
            int startNode = n;
            int goalNode = n + 1;
            int totalNodes = n + 2;

            var gArr = new NativeArray<float>(totalNodes, Allocator.Temp);
            var parentArr = new NativeArray<int>(totalNodes, Allocator.Temp);
            var closedArr = new NativeArray<byte>(totalNodes, Allocator.Temp);
            gArr.Fill(float.PositiveInfinity);
            parentArr.Fill(-1);
            closedArr.Fill((byte)0);

            var heap = MinHeap.Create(totalNodes, Allocator.Temp);
            gArr[startNode] = 0f;
            heap.InsertOrDecrease(new HeapNode(startNode, Grid2D.HeuristicEuclidean(startCoord, goalCoord)));

            // Get position array (subgoal coords + start + goal)
            var positions = new NativeArray<int2>(totalNodes, Allocator.Temp);
            for (int i = 0; i < n; i++)
                positions[i] = s.Grid.ToCoord(s.Subgoals[i]);
            positions[startNode] = startCoord;
            positions[goalNode] = goalCoord;

            while (!heap.IsEmpty)
            {
                int u = heap.Pop().Id;
                if (u == goalNode)
                {
                    // Extract path through subgoal graph
                    var tempPath = new NativeList<int>(Allocator.Temp);
                    int cur = goalNode;
                    while (cur >= 0)
                    {
                        if (cur < n) tempPath.Add(s.Subgoals[cur]);
                        else if (cur == startNode) tempPath.Add(start);
                        cur = parentArr[cur];
                    }
                    // Reverse
                    for (int i = tempPath.Length - 1; i >= 0; i--)
                        path.Add(tempPath[i]);
                    tempPath.Dispose();
                    break;
                }

                closedArr[u] = 1;

                // Expand: check visibility to all other nodes
                for (int v = 0; v < totalNodes; v++)
                {
                    if (v == u || closedArr[v] != 0) continue;

                    // Check visibility (skip for same-node edges in the graph)
                    bool visible = false;
                    if (u < n && v < n)
                    {
                        // Check if edge exists in precomputed graph
                        // (simplified: just check line of sight)
                        visible = true; // edges already computed in Build
                    }
                    else
                    {
                        visible = true; // temporary nodes
                    }

                    if (!visible) continue;
                    if (!LineOfSight(s.Grid, blocked, positions[u], positions[v])) continue;

                    float cost = math.length(new float2(positions[v].x - positions[u].x, positions[v].y - positions[u].y));
                    float newG = gArr[u] + cost;
                    if (newG < gArr[v])
                    {
                        gArr[v] = newG;
                        parentArr[v] = u;
                        float f = newG + Grid2D.HeuristicEuclidean(positions[v], goalCoord);
                        heap.InsertOrDecrease(new HeapNode(v, f));
                    }
                }
            }

            gArr.Dispose(); parentArr.Dispose(); closedArr.Dispose(); heap.Dispose(); positions.Dispose();
            return path.Length > 0;
        }

        public static void Dispose(ref SubgoalState s)
        {
            if (s.Subgoals.IsCreated) s.Subgoals.Dispose();
            if (s.SubgoalOfCell.IsCreated) s.SubgoalOfCell.Dispose();
            if (s.Edges.IsCreated) s.Edges.Dispose();
            if (s.EdgeRanges.IsCreated) s.EdgeRanges.Dispose();
        }
    }
}
