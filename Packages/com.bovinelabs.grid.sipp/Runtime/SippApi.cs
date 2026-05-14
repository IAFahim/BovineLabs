using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Sipp
{
    public struct SafeInterval
    {
        public int Cell;
        public float Start;
        public float End;
    }

    public struct SippNode
    {
        public int Cell;
        public int IntervalIdx;
        public float Time;
        public float F;
        public int Parent;
    }

    public struct DynamicObstacle
    {
        public int Cell;
        public float StartTime;
        public float EndTime;
    }

    public unsafe struct SippState
    {
        public Grid2D Grid;
        public UnsafeList<SafeInterval> Intervals;
        public RangeI* CellIntervals;
        public UnsafeList<SippNode> Nodes;
        public MinHeap Heap;
        public UnsafeList<float> BestTime;
        public UnsafeList<DynamicObstacle> Obstacles;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class SippApi
    {
        public static bool TryCreate(int width, int height, int maxIntervals, int maxNodes, Allocator a,
            out SippState s)
        {
            s = default;
            if (!Grid2D.TryCreate(width, height, out var g)) return false;
            if (!MinHeap.TryCreate(maxNodes, a, out var heap)) return false;

            s = new SippState
            {
                Grid = g,
                Intervals = new UnsafeList<SafeInterval>(maxIntervals, a),
                CellIntervals = (RangeI*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(RangeI), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<RangeI>(), g.Length),
                Nodes = new UnsafeList<SippNode>(maxNodes, a),
                Heap = heap,
                BestTime = new UnsafeList<float>(maxIntervals, a),
                Obstacles = new UnsafeList<DynamicObstacle>(maxIntervals, a)
            };
            return true;
        }

        public static bool TryAddObstacle(ref SippState s, int cell, float startTime, float endTime)
        {
            s.Obstacles.Add(new DynamicObstacle { Cell = cell, StartTime = startTime, EndTime = endTime });
            return true;
        }

        [BurstCompile]
        public static bool TryBuildSafeIntervals(ref SippState s)
        {
            s.Intervals.Clear();
            var cellCount = s.Grid.Length;

            s.Obstacles.Sort(new ObstacleComparer());

            var obsPtr = s.Obstacles.Ptr;
            var obsCount = s.Obstacles.Length;
            var obsIdx = 0;

            for (var i = 0; i < cellCount; i++)
            {
                var start = s.Intervals.Length;
                var t = 0f;

                while (obsIdx < obsCount && obsPtr[obsIdx].Cell == i)
                {
                    var obsStart = obsPtr[obsIdx].StartTime;
                    var obsEnd = obsPtr[obsIdx].EndTime;

                    if (t < obsStart) s.Intervals.Add(new SafeInterval { Cell = i, Start = t, End = obsStart });
                    t = math.max(t, obsEnd);
                    obsIdx++;
                }

                if (t < float.PositiveInfinity)
                    s.Intervals.Add(new SafeInterval { Cell = i, Start = t, End = float.PositiveInfinity });

                s.CellIntervals[i] = new RangeI(start, s.Intervals.Length - start);
            }

            return true;
        }

        [BurstCompile]
        public static bool TrySearch(
            ref SippState s,
            in NativeArray<byte> blocked,
            int start, int goal,
            float startTime,
            ref NativeList<int> path)
        {
            s.Nodes.Clear();
            s.Heap.Clear();
            path.Clear();
            if (!TryBuildSafeIntervals(ref s)) return false;

            var blockedPtr = (byte*)blocked.GetUnsafeReadOnlyPtr();
            if (Hint.Unlikely(blockedPtr[start] != 0 || blockedPtr[goal] != 0)) return false;

            var intervalCount = s.Intervals.Length;
            if (s.BestTime.Capacity < intervalCount) s.BestTime.SetCapacity(intervalCount);
            s.BestTime.Resize(intervalCount);
            var bestTimePtr = s.BestTime.Ptr;
            for (var i = 0; i < intervalCount; i++) bestTimePtr[i] = float.PositiveInfinity;

            var startRange = s.CellIntervals[start];
            var startIntervalIdx = -1;
            var intervalsPtr = s.Intervals.Ptr;

            for (var iv = startRange.Offset; iv < startRange.Offset + startRange.Count; iv++)
                if (intervalsPtr[iv].Start <= startTime && startTime < intervalsPtr[iv].End)
                {
                    startIntervalIdx = iv;
                    break;
                }

            if (startIntervalIdx < 0) return false;

            s.Nodes.Add(new SippNode
                { Cell = start, IntervalIdx = startIntervalIdx, Time = startTime, F = startTime, Parent = -1 });
            if (!s.Heap.TryInsertOrDecrease(new HeapNode(0, startTime))) return false;
            bestTimePtr[startIntervalIdx] = startTime;

            var goalCoord = s.Grid.ToCoord(goal);
            var width = s.Grid.Width;
            var height = s.Grid.Height;

            while (!s.Heap.IsEmpty)
            {
                if (!s.Heap.TryPop(out var top)) return false;
                var nodeId = top.Id;
                var node = s.Nodes[nodeId];

                if (node.Time > bestTimePtr[node.IntervalIdx]) continue;

                if (node.Cell == goal)
                {
                    ExtractPath(in s.Nodes, nodeId, ref path);
                    return true;
                }

                var p = s.Grid.ToCoord(node.Cell);
                for (var d = 0; d < 4; d++)
                {
                    var np = p + Grid2D.Dir4(d);
                    if (Hint.Unlikely(np.x < 0 || np.y < 0 || np.x >= width || np.y >= height)) continue;
                    var ni = np.y * width + np.x;
                    if (Hint.Unlikely(blockedPtr[ni] != 0)) continue;

                    var moveTime = 1f;
                    var arrivalTime = node.Time + moveTime;

                    var niRange = s.CellIntervals[ni];
                    for (var iv = niRange.Offset; iv < niRange.Offset + niRange.Count; iv++)
                    {
                        var interval = intervalsPtr[iv];
                        if (arrivalTime >= interval.End) continue;

                        var earliestArrival = math.max(arrivalTime, interval.Start);
                        if (earliestArrival < interval.End)
                            if (earliestArrival < bestTimePtr[iv])
                            {
                                bestTimePtr[iv] = earliestArrival;
                                var h = Grid2D.HeuristicManhattan(np, goalCoord);
                                var newNodeId = s.Nodes.Length;
                                s.Nodes.Add(new SippNode
                                {
                                    Cell = ni, IntervalIdx = iv, Time = earliestArrival, F = earliestArrival + h,
                                    Parent = nodeId
                                });
                                if (!s.Heap.TryInsertOrDecrease(new HeapNode(newNodeId, earliestArrival + h)))
                                    return false;
                            }
                    }
                }
            }

            return false;
        }

        private static void ExtractPath(in UnsafeList<SippNode> nodes, int nodeId, ref NativeList<int> path)
        {
            var cur = nodeId;
            while (cur >= 0)
            {
                path.Add(nodes[cur].Cell);
                cur = nodes[cur].Parent;
            }

            for (int i = 0, j = path.Length - 1; i < j; i++, j--)
            {
                var tmp = path[i];
                path[i] = path[j];
                path[j] = tmp;
            }
        }

        public static void Dispose(ref SippState s)
        {
            if (s.Intervals.IsCreated) s.Intervals.Dispose();
            if (s.CellIntervals != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.CellIntervals); s.CellIntervals = null; }
            if (s.Nodes.IsCreated) s.Nodes.Dispose();
            s.Heap.Dispose();
            if (s.BestTime.IsCreated) s.BestTime.Dispose();
            if (s.Obstacles.IsCreated) s.Obstacles.Dispose();
        }

        private struct ObstacleComparer : IComparer<DynamicObstacle>
        {
            public int Compare(DynamicObstacle x, DynamicObstacle y)
            {
                if (x.Cell != y.Cell) return x.Cell.CompareTo(y.Cell);
                return x.StartTime.CompareTo(y.StartTime);
            }
        }
    }
}