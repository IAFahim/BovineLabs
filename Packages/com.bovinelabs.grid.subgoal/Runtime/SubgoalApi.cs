using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Subgoal
{
    public struct SubgoalEdge
    {
        public int To;
        public float Cost;
    }

    public unsafe struct SubgoalState
    {
        public Grid2D Grid;
        public UnsafeList<int> Subgoals;
        public int* SubgoalOfCell;
        public UnsafeList<SubgoalEdge> Edges;
        public UnsafeList<RangeI> EdgeRanges;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class SubgoalApi
    {
        public static bool TryCreate(int width, int height, int maxSubgoals, int maxEdges, Allocator a,
            out SubgoalState s)
        {
            s = default;
            if (!Grid2D.TryCreate(width, height, out var g)) return false;
            s = new SubgoalState
            {
                Grid = g,
                Subgoals = new UnsafeList<int>(maxSubgoals, a),
                SubgoalOfCell = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length),
                Edges = new UnsafeList<SubgoalEdge>(maxEdges, a),
                EdgeRanges = new UnsafeList<RangeI>(maxSubgoals, a)
            };
            return true;
        }

        [BurstCompile]
        public static bool TryBuild(ref SubgoalState s, in NativeArray<byte> blocked)
        {
            s.Subgoals.Clear();
            for (var i = 0; i < s.Grid.Length; i++) s.SubgoalOfCell[i] = -1;
            s.Edges.Clear();
            s.EdgeRanges.Clear();

            var width = s.Grid.Width;
            var height = s.Grid.Height;
            var blockedPtr = (byte*)blocked.GetUnsafeReadOnlyPtr();
            var subgoalOfCellPtr = s.SubgoalOfCell;

            for (var i = 0; i < s.Grid.Length; i++)
            {
                if (blockedPtr[i] != 0) continue;
                var y = i / width;
                var x = i % width;
                var p = new int2(x, y);
                var isCorner = false;

                for (var d = 0; d < 4; d++)
                {
                    var np = p + Grid2D.Dir4(d);
                    if (Hint.Unlikely(np.x < 0 || np.y < 0 || np.x >= width || np.y >= height)) continue;

                    if (blockedPtr[np.y * width + np.x] != 0)
                        for (var dy = -1; dy <= 1; dy += 2)
                        {
                            for (var dx = -1; dx <= 1; dx += 2)
                            {
                                var dp = p + new int2(dx, dy);
                                if (dp.x >= 0 && dp.y >= 0 && dp.x < width && dp.y < height &&
                                    blockedPtr[dp.y * width + dp.x] == 0)
                                {
                                    isCorner = true;
                                    break;
                                }
                            }

                            if (isCorner) break;
                        }

                    if (isCorner) break;
                }

                if (isCorner)
                {
                    var id = s.Subgoals.Length;
                    s.Subgoals.Add(i);
                    subgoalOfCellPtr[i] = id;
                }
            }

            for (var i = 0; i < s.Subgoals.Length; i++)
            {
                var edgeStart = s.Edges.Length;
                var pi = s.Grid.ToCoord(s.Subgoals[i]);

                for (var j = 0; j < s.Subgoals.Length; j++)
                {
                    if (i == j) continue;
                    var pj = s.Grid.ToCoord(s.Subgoals[j]);
                    if (LineOfSight(in s.Grid, blockedPtr, ref pi, ref pj))
                        s.Edges.Add(new SubgoalEdge { To = j, Cost = math.distance(pi, pj) });
                }

                s.EdgeRanges.Add(new RangeI(edgeStart, s.Edges.Length - edgeStart));
            }

            return true;
        }

        [BurstCompile]
        public static bool TrySearch(
            ref SubgoalState s,
            in NativeArray<byte> blocked,
            int start, int goal,
            ref NativeList<int> path)
        {
            path.Clear();
            var startCoord = s.Grid.ToCoord(start);
            var goalCoord = s.Grid.ToCoord(goal);
            var blockedPtr = (byte*)blocked.GetUnsafeReadOnlyPtr();

            if (LineOfSight(in s.Grid, blockedPtr, ref startCoord, ref goalCoord))
            {
                path.Add(start);
                path.Add(goal);
                return true;
            }

            var n = s.Subgoals.Length;
            var startNode = n;
            var goalNode = n + 1;
            var totalNodes = n + 2;

            var gArr = new Unity.Collections.NativeArray<float>(totalNodes, Allocator.Temp);
            var parentArr = new Unity.Collections.NativeArray<int>(totalNodes, Allocator.Temp);
            gArr.Fill(float.PositiveInfinity);
            parentArr.Fill(-1);

            var gPtr = (float*)gArr.GetUnsafePtr();
            var parentPtr = (int*)parentArr.GetUnsafePtr();

            if (!MinHeap.TryCreate(totalNodes, Allocator.Temp, out var heap))
            {
                gArr.Dispose();
                parentArr.Dispose();
                return false;
            }

            gPtr[startNode] = 0f;
            if (!heap.TryInsertOrDecrease(new HeapNode(startNode, math.distance(startCoord, goalCoord))))
            {
                gArr.Dispose();
                parentArr.Dispose();
                heap.Dispose();
                return false;
            }

            while (!heap.IsEmpty)
            {
                if (!heap.TryPop(out var top))
                {
                    gArr.Dispose();
                    parentArr.Dispose();
                    heap.Dispose();
                    return false;
                }

                var u = top.Id;
                if (u == goalNode)
                {
                    ExtractPath(in s, start, goal, parentPtr, goalNode, ref path);
                    gArr.Dispose();
                    parentArr.Dispose();
                    heap.Dispose();
                    return true;
                }

                var pu = u < n ? s.Grid.ToCoord(s.Subgoals[u]) : u == startNode ? startCoord : goalCoord;

                if (u == startNode)
                {
                    for (var v = 0; v < n; v++)
                    {
                        var pv = s.Grid.ToCoord(s.Subgoals[v]);
                        if (LineOfSight(in s.Grid, blockedPtr, ref pu, ref pv))
                            if (!TryRelax(v, pu, pv, u, gPtr, parentPtr, ref heap, goalCoord))
                            {
                                gArr.Dispose();
                                parentArr.Dispose();
                                heap.Dispose();
                                return false;
                            }
                    }

                    if (LineOfSight(in s.Grid, blockedPtr, ref pu, ref goalCoord))
                        if (!TryRelax(goalNode, pu, goalCoord, u, gPtr, parentPtr, ref heap, goalCoord))
                        {
                            gArr.Dispose();
                            parentArr.Dispose();
                            heap.Dispose();
                            return false;
                        }
                }
                else if (u < n)
                {
                    var range = s.EdgeRanges[u];
                    for (var i = range.Offset; i < range.Offset + range.Count; i++)
                    {
                        var edge = s.Edges[i];
                        var pv = s.Grid.ToCoord(s.Subgoals[edge.To]);
                        if (!TryRelax(edge.To, pu, pv, u, gPtr, parentPtr, ref heap, goalCoord))
                        {
                            gArr.Dispose();
                            parentArr.Dispose();
                            heap.Dispose();
                            return false;
                        }
                    }

                    if (LineOfSight(in s.Grid, blockedPtr, ref pu, ref goalCoord))
                        if (!TryRelax(goalNode, pu, goalCoord, u, gPtr, parentPtr, ref heap, goalCoord))
                        {
                            gArr.Dispose();
                            parentArr.Dispose();
                            heap.Dispose();
                            return false;
                        }
                }
            }

            gArr.Dispose();
            parentArr.Dispose();
            heap.Dispose();
            return false;
        }

        private static bool TryRelax(int v, int2 pu, int2 pv, int u, float* gArr, int* parentArr, ref MinHeap heap,
            int2 goalCoord)
        {
            var d = math.distance(pu, pv);
            var newG = gArr[u] + d;
            if (newG < gArr[v])
            {
                gArr[v] = newG;
                parentArr[v] = u;
                return heap.TryInsertOrDecrease(new HeapNode(v, newG + math.distance(pv, goalCoord)));
            }

            return true;
        }

        private static void ExtractPath(in SubgoalState s, int start, int goal, int* parentArr, int goalNode,
            ref NativeList<int> path)
        {
            var temp = new NativeList<int>(Allocator.Temp);
            var cur = goalNode;
            while (cur != -1)
            {
                if (cur < s.Subgoals.Length) temp.Add(s.Subgoals[cur]);
                else if (cur == s.Subgoals.Length) temp.Add(start);
                else temp.Add(goal);
                cur = parentArr[cur];
            }

            for (var i = temp.Length - 1; i >= 0; i--) path.Add(temp[i]);
            temp.Dispose();
        }

        [BurstCompile]
        public static bool LineOfSight(in Grid2D grid, byte* blocked, ref int2 from, ref int2 to)
        {
            var dx = math.abs(to.x - from.x);
            var dy = math.abs(to.y - from.y);
            var sx = from.x < to.x ? 1 : -1;
            var sy = from.y < to.y ? 1 : -1;
            var err = dx - dy;
            int x = from.x, y = from.y;
            var width = grid.Width;
            var height = grid.Height;

            while (true)
            {
                if (Hint.Unlikely(x < 0 || y < 0 || x >= width || y >= height)) return false;
                if (blocked[y * width + x] != 0) return false;
                if (x == to.x && y == to.y) break;
                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            return true;
        }

        public static void Dispose(ref SubgoalState s)
        {
            s.Subgoals.Dispose();
            if (s.SubgoalOfCell != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.SubgoalOfCell); s.SubgoalOfCell = null; }
            s.Edges.Dispose();
            s.EdgeRanges.Dispose();
        }
    }
}