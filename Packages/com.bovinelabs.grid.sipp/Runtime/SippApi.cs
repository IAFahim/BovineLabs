using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Sipp
{
    public struct SafeInterval { public int Cell; public float Start; public float End; }

    public struct SippNode { public int Cell; public int Interval; public float Time; public float F; public int Parent; }

    public struct DynamicObstacle { public int Cell; public float StartTime; public float EndTime; }

    public struct SippState
    {
        public Grid2D Grid;
        public NativeList<SafeInterval> Intervals;
        public NativeArray<RangeI> CellIntervals;
        public NativeList<SippNode> Nodes;
        public MinHeap Heap;
        public NativeArray<float> BestTime;
        public NativeList<DynamicObstacle> Obstacles;
    }

    public static class SippApi
    {
        public static SippState Create(int width, int height, int maxIntervals, int maxNodes, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new SippState
            {
                Grid = g,
                Intervals = new NativeList<SafeInterval>(maxIntervals, a),
                CellIntervals = new NativeArray<RangeI>(g.Length, a),
                Nodes = new NativeList<SippNode>(maxNodes, a),
                Heap = MinHeap.Create(maxNodes, a),
                BestTime = new NativeArray<float>(g.Length, a),
                Obstacles = new NativeList<DynamicObstacle>(maxIntervals, a),
            };
        }

        public static void AddObstacle(ref SippState s, int cell, float startTime, float endTime)
        {
            s.Obstacles.Add(new DynamicObstacle { Cell = cell, StartTime = startTime, EndTime = endTime });
        }

        public static void BuildSafeIntervals(ref SippState s)
        {
            s.Intervals.Clear();

            // Start with [0, inf) for all cells
            // Then split intervals based on dynamic obstacles
            // First, collect obstacles per cell
            var cellObs = new NativeList<float>(s.Obstacles.Length * 2, Allocator.Temp);
            var cellOffsets = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var cellCounts = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            cellOffsets.Fill(0);
            cellCounts.Fill(0);

            // Count obstacles per cell
            for (int i = 0; i < s.Obstacles.Length; i++)
                cellCounts[s.Obstacles[i].Cell]++;

            // Compute offsets
            int offset = 0;
            for (int i = 0; i < s.Grid.Length; i++)
            {
                cellOffsets[i] = offset;
                offset += cellCounts[i] * 2; // start + end
            }

            cellObs.Resize(offset, NativeArrayOptions.ClearMemory);

            // Fill obstacle times (just store start and end pairs)
            var writeIdx = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            NativeArray<int>.Copy(cellOffsets, writeIdx);
            for (int i = 0; i < s.Obstacles.Length; i++)
            {
                int c = s.Obstacles[i].Cell;
                cellObs[writeIdx[c]++] = s.Obstacles[i].StartTime;
                cellObs[writeIdx[c]++] = s.Obstacles[i].EndTime;
            }

            // Build intervals per cell
            for (int i = 0; i < s.Grid.Length; i++)
            {
                int start = s.Intervals.Length;

                if (cellCounts[i] == 0)
                {
                    // No obstacles: single interval [0, inf)
                    s.Intervals.Add(new SafeInterval { Cell = i, Start = 0f, End = float.PositiveInfinity });
                }
                else
                {
                    // Parse obstacle time pairs and create safe intervals between them
                    float t = 0f;
                    for (int j = cellOffsets[i]; j < cellOffsets[i] + cellCounts[i] * 2; j += 2)
                    {
                        float obsStart = cellObs[j];
                        float obsEnd = cellObs[j + 1];

                        // Safe interval before this obstacle
                        if (t < obsStart)
                        {
                            s.Intervals.Add(new SafeInterval { Cell = i, Start = t, End = obsStart });
                        }
                        t = obsEnd;
                    }
                    // Final interval after last obstacle
                    if (t < float.PositiveInfinity)
                    {
                        s.Intervals.Add(new SafeInterval { Cell = i, Start = t, End = float.PositiveInfinity });
                    }

                    // If no safe intervals were created, add empty range
                    if (s.Intervals.Length == start)
                    {
                        // Cell is blocked for all time — don't add any interval
                    }
                }

                s.CellIntervals[i] = new RangeI(start, s.Intervals.Length - start);
            }

            cellObs.Dispose(); cellOffsets.Dispose(); cellCounts.Dispose(); writeIdx.Dispose();
        }

        public static bool Search(
            ref SippState s,
            NativeArray<byte> blocked,
            int start, int goal,
            float startTime,
            NativeList<int> path)
        {
            s.Nodes.Clear();
            s.Heap.Clear();
            path.Clear();
            BuildSafeIntervals(ref s);

            if (blocked[start] != 0 || blocked[goal] != 0) return false;

            for (int i = 0; i < s.BestTime.Length; i++) s.BestTime[i] = float.PositiveInfinity;

            // Find valid starting interval
            var startRange = s.CellIntervals[start];
            int startInterval = -1;
            for (int iv = startRange.Offset; iv < startRange.Offset + startRange.Count; iv++)
            {
                if (s.Intervals[iv].Start <= startTime && startTime < s.Intervals[iv].End)
                {
                    startInterval = iv;
                    break;
                }
            }
            if (startInterval < 0) return false;

            int startNode = 0;
            s.Nodes.Add(new SippNode { Cell = start, Interval = startInterval, Time = startTime, F = startTime, Parent = -1 });
            s.Heap.InsertOrDecrease(new HeapNode(startNode, startTime));
            s.BestTime[start] = startTime;

            while (!s.Heap.IsEmpty)
            {
                int nodeId = s.Heap.Pop().Id;
                if (nodeId >= s.Nodes.Length) continue;
                var node = s.Nodes[nodeId];

                if (node.Time > s.BestTime[node.Cell]) continue;

                if (node.Cell == goal)
                {
                    int cur = nodeId;
                    while (cur >= 0) { path.Add(s.Nodes[cur].Cell); cur = s.Nodes[cur].Parent; }
                    for (int i = 0; i < path.Length / 2; i++)
                    { int tmp = path[i]; path[i] = path[path.Length - 1 - i]; path[path.Length - 1 - i] = tmp; }
                    return true;
                }

                int2 p = s.Grid.ToCoord(node.Cell);
                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (blocked[ni] != 0) continue;

                    float arrivalTime = node.Time + 1f;

                    // Find compatible safe interval
                    var range = s.CellIntervals[ni];
                    for (int iv = range.Offset; iv < range.Offset + range.Count; iv++)
                    {
                        var interval = s.Intervals[iv];
                        if (arrivalTime >= interval.Start && arrivalTime < interval.End)
                        {
                            if (arrivalTime >= s.BestTime[ni]) break;
                            s.BestTime[ni] = arrivalTime;

                            float h = Grid2D.HeuristicManhattan(s.Grid.ToCoord(ni), s.Grid.ToCoord(goal));
                            int newNodeId = s.Nodes.Length;
                            s.Nodes.Add(new SippNode { Cell = ni, Interval = iv, Time = arrivalTime, F = arrivalTime + h, Parent = nodeId });
                            s.Heap.InsertOrDecrease(new HeapNode(newNodeId, arrivalTime + h));
                            break;
                        }
                    }
                }
            }

            return false;
        }

        public static void Dispose(ref SippState s)
        {
            if (s.Intervals.IsCreated) s.Intervals.Dispose();
            if (s.CellIntervals.IsCreated) s.CellIntervals.Dispose();
            if (s.Nodes.IsCreated) s.Nodes.Dispose();
            if (s.Heap.IsCreated) s.Heap.Dispose();
            if (s.BestTime.IsCreated) s.BestTime.Dispose();
            if (s.Obstacles.IsCreated) s.Obstacles.Dispose();
        }
    }
}
