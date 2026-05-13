using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Sipp
{
    public struct SafeInterval { public int Cell; public float Start; public float End; }

    public struct SippNode { public int Cell; public int IntervalIdx; public float Time; public float F; public int Parent; }

    public struct DynamicObstacle { public int Cell; public float StartTime; public float EndTime; }

    public struct SippState
    {
        public Grid2D Grid;
        public UnsafeList<SafeInterval> Intervals;
        public NativeArray<RangeI> CellIntervals;
        public UnsafeList<SippNode> Nodes;
        public MinHeap Heap;
        public UnsafeList<float> BestTime; // per interval index
        public UnsafeList<DynamicObstacle> Obstacles;
    }

    [BurstCompile]
    public unsafe static class SippApi
    {
        public static SippState Create(int width, int height, int maxIntervals, int maxNodes, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new SippState
            {
                Grid = g,
                Intervals = new UnsafeList<SafeInterval>(maxIntervals, a),
                CellIntervals = new NativeArray<RangeI>(g.Length, a),
                Nodes = new UnsafeList<SippNode>(maxNodes, a),
                Heap = MinHeap.Create(maxNodes, a),
                BestTime = new UnsafeList<float>(maxIntervals, a),
                Obstacles = new UnsafeList<DynamicObstacle>(maxIntervals, a),
            };
        }

        public static void AddObstacle(ref SippState s, int cell, float startTime, float endTime)
        {
            s.Obstacles.Add(new DynamicObstacle { Cell = cell, StartTime = startTime, EndTime = endTime });
        }

        [BurstCompile]
        public static void BuildSafeIntervals(ref SippState s)
        {
            s.Intervals.Clear();
            int cellCount = s.Grid.Length;

            // Sort obstacles by cell and then by start time
            s.Obstacles.Sort(new ObstacleComparer());

            DynamicObstacle* obsPtr = s.Obstacles.Ptr;
            int obsCount = s.Obstacles.Length;
            int obsIdx = 0;

            for (int i = 0; i < cellCount; i++)
            {
                int start = s.Intervals.Length;
                float t = 0f;

                while (obsIdx < obsCount && obsPtr[obsIdx].Cell == i)
                {
                    float obsStart = obsPtr[obsIdx].StartTime;
                    float obsEnd = obsPtr[obsIdx].EndTime;

                    if (t < obsStart)
                    {
                        s.Intervals.Add(new SafeInterval { Cell = i, Start = t, End = obsStart });
                    }
                    t = math.max(t, obsEnd);
                    obsIdx++;
                }

                if (t < float.PositiveInfinity)
                {
                    s.Intervals.Add(new SafeInterval { Cell = i, Start = t, End = float.PositiveInfinity });
                }

                s.CellIntervals[i] = new RangeI(start, s.Intervals.Length - start);
            }
        }

        [BurstCompile]
        public static bool Search(
            ref SippState s,
            in NativeArray<byte> blocked,
            int start, int goal,
            float startTime,
            ref NativeList<int> path)
        {
            s.Nodes.Clear();
            s.Heap.Clear();
            path.Clear();
            BuildSafeIntervals(ref s);

            byte* blockedPtr = (byte*)blocked.GetUnsafeReadOnlyPtr();
            if (Hint.Unlikely(blockedPtr[start] != 0 || blockedPtr[goal] != 0)) return false;

            // Resize BestTime to match current Intervals count
            int intervalCount = s.Intervals.Length;
            if (s.BestTime.Capacity < intervalCount) s.BestTime.SetCapacity(intervalCount);
            s.BestTime.Resize(intervalCount);
            float* bestTimePtr = s.BestTime.Ptr;
            for (int i = 0; i < intervalCount; i++) bestTimePtr[i] = float.PositiveInfinity;

            // Find valid starting interval
            RangeI startRange = s.CellIntervals[start];
            int startIntervalIdx = -1;
            SafeInterval* intervalsPtr = s.Intervals.Ptr;

            for (int iv = startRange.Offset; iv < startRange.Offset + startRange.Count; iv++)
            {
                if (intervalsPtr[iv].Start <= startTime && startTime < intervalsPtr[iv].End)
                {
                    startIntervalIdx = iv;
                    break;
                }
            }
            if (startIntervalIdx < 0) return false;

            s.Nodes.Add(new SippNode { Cell = start, IntervalIdx = startIntervalIdx, Time = startTime, F = startTime, Parent = -1 });
            s.Heap.InsertOrDecrease(new HeapNode(0, startTime));
            bestTimePtr[startIntervalIdx] = startTime;

            int2 goalCoord = s.Grid.ToCoord(goal);
            int width = s.Grid.Width;
            int height = s.Grid.Height;

            while (!s.Heap.IsEmpty)
            {
                int nodeId = s.Heap.Pop().Id;
                SippNode node = s.Nodes[nodeId];

                if (node.Time > bestTimePtr[node.IntervalIdx]) continue;

                if (node.Cell == goal)
                {
                    ExtractPath(in s.Nodes, nodeId, ref path);
                    return true;
                }

                int2 p = s.Grid.ToCoord(node.Cell);
                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Dir4(d);
                    if (Hint.Unlikely(np.x < 0 || np.y < 0 || np.x >= width || np.y >= height)) continue;
                    int ni = np.y * width + np.x;
                    if (Hint.Unlikely(blockedPtr[ni] != 0)) continue;

                    float moveTime = 1f;
                    float arrivalTime = node.Time + moveTime;

                    RangeI niRange = s.CellIntervals[ni];
                    for (int iv = niRange.Offset; iv < niRange.Offset + niRange.Count; iv++)
                    {
                        SafeInterval interval = intervalsPtr[iv];
                        if (arrivalTime >= interval.End) continue;

                        float earliestArrival = math.max(arrivalTime, interval.Start);
                        if (earliestArrival < interval.End)
                        {
                            // Can wait at current cell before moving
                            if (earliestArrival < bestTimePtr[iv])
                            {
                                bestTimePtr[iv] = earliestArrival;
                                float h = Grid2D.HeuristicManhattan(np, goalCoord);
                                int newNodeId = s.Nodes.Length;
                                s.Nodes.Add(new SippNode { Cell = ni, IntervalIdx = iv, Time = earliestArrival, F = earliestArrival + h, Parent = nodeId });
                                s.Heap.InsertOrDecrease(new HeapNode(newNodeId, earliestArrival + h));
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static void ExtractPath(in UnsafeList<SippNode> nodes, int nodeId, ref NativeList<int> path)
        {
            int cur = nodeId;
            while (cur >= 0) { path.Add(nodes[cur].Cell); cur = nodes[cur].Parent; }
            for (int i = 0, j = path.Length - 1; i < j; i++, j--)
            { int tmp = path[i]; path[i] = path[j]; path[j] = tmp; }
        }

        public static void Dispose(ref SippState s)
        {
            if (s.Intervals.IsCreated) s.Intervals.Dispose();
            if (s.CellIntervals.IsCreated) s.CellIntervals.Dispose();
            if (s.Nodes.IsCreated) s.Nodes.Dispose();
            s.Heap.Dispose();
            if (s.BestTime.IsCreated) s.BestTime.Dispose();
            if (s.Obstacles.IsCreated) s.Obstacles.Dispose();
        }

        private struct ObstacleComparer : System.Collections.Generic.IComparer<DynamicObstacle>
        {
            public int Compare(DynamicObstacle x, DynamicObstacle y)
            {
                if (x.Cell != y.Cell) return x.Cell.CompareTo(y.Cell);
                return x.StartTime.CompareTo(y.StartTime);
            }
        }
    }
}
