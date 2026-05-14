using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.GraphCut
{
    public unsafe struct GraphCutState
    {
        public Grid2D Grid;

        public UnsafeList<int> EdgeTo;
        public UnsafeList<int> EdgeCap;
        public UnsafeList<int> EdgeFlow;
        public UnsafeList<int> EdgeRev;
        public UnsafeList<int> EdgeNext;

        public int* EdgeHead;
        public int* Excess;
        public int* Height;
        public byte* SourceSide;
        public UnsafeList<int> ActiveNodes;
        public byte* InActiveNodes;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class GraphCutApi
    {
        private const int SourceOffset = 0;
        private const int SinkOffset = 1;

        public static bool TryCreate(int width, int height, int maxEdges, Allocator a, out GraphCutState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            var nodeCount = g.Length + 2;
            result = new GraphCutState
            {
                Allocator = a,
                Grid = g,
                EdgeTo = new UnsafeList<int>(maxEdges, a),
                EdgeCap = new UnsafeList<int>(maxEdges, a),
                EdgeFlow = new UnsafeList<int>(maxEdges, a),
                EdgeRev = new UnsafeList<int>(maxEdges, a),
                EdgeNext = new UnsafeList<int>(maxEdges, a),
                EdgeHead = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), nodeCount),
                Excess = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), nodeCount),
                Height = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), nodeCount),
                SourceSide = (byte*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), g.Length),
                ActiveNodes = new UnsafeList<int>(nodeCount, a),
                InActiveNodes = (byte*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), nodeCount)
            };
            return true;
        }

        [BurstCompile]
        public static void BuildBinaryEnergy(
            ref GraphCutState s,
            in NativeArray<int> unary0,
            in NativeArray<int> unary1,
            in NativeArray<int> pairwise)
        {
            s.EdgeTo.Clear();
            s.EdgeCap.Clear();
            s.EdgeFlow.Clear();
            s.EdgeRev.Clear();
            s.EdgeNext.Clear();
            var nodeCount = s.Grid.Length + 2;
            for (int i = 0; i < nodeCount; i++) s.EdgeHead[i] = -1;

            var cellCount = s.Grid.Length;
            var sourceIdx = cellCount;
            var sinkIdx = cellCount + 1;

            var unary0Ptr = (int*)unary0.GetUnsafeReadOnlyPtr();
            var unary1Ptr = (int*)unary1.GetUnsafeReadOnlyPtr();
            var pairwisePtr = (int*)pairwise.GetUnsafeReadOnlyPtr();

            for (var i = 0; i < cellCount; i++)
            {
                if (unary0Ptr[i] > 0) AddEdgeInternal(ref s, sourceIdx, i, unary0Ptr[i]);
                if (unary1Ptr[i] > 0) AddEdgeInternal(ref s, i, sinkIdx, unary1Ptr[i]);
            }

            var width = s.Grid.Width;
            var height = s.Grid.Height;
            for (var i = 0; i < cellCount; i++)
            {
                var y = i / width;
                var x = i % width;
                var w = pairwisePtr[i];
                if (w <= 0) continue;

                for (var d = 0; d < 2; d++)
                {
                    var offset = Grid2D.Dir4(d);
                    var nx = x + offset.x;
                    var ny = y + offset.y;
                    if (Hint.Likely(nx >= 0 && ny >= 0 && nx < width && ny < height))
                    {
                        var neighbor = ny * width + nx;
                        AddEdgeInternal(ref s, i, neighbor, w);
                        AddEdgeInternal(ref s, neighbor, i, w);
                    }
                }
            }
        }

        public static void AddEdgeInternal(ref GraphCutState s, int u, int v, int cap)
        {
            var uvIdx = s.EdgeTo.Length;
            var vuIdx = uvIdx + 1;

            s.EdgeTo.Add(v);
            s.EdgeCap.Add(cap);
            s.EdgeFlow.Add(0);
            s.EdgeRev.Add(vuIdx);
            s.EdgeNext.Add(s.EdgeHead[u]);
            s.EdgeHead[u] = uvIdx;

            s.EdgeTo.Add(u);
            s.EdgeCap.Add(0);
            s.EdgeFlow.Add(0);
            s.EdgeRev.Add(uvIdx);
            s.EdgeNext.Add(s.EdgeHead[v]);
            s.EdgeHead[v] = vuIdx;
        }

        [BurstCompile]
        public static bool TrySolve(ref GraphCutState s, int sourceCell, int sinkCell)
        {
            var u0 = new Unity.Collections.NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var u1 = new Unity.Collections.NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var pw = new Unity.Collections.NativeArray<int>(s.Grid.Length, Allocator.Temp);
            if (sourceCell >= 0 && sourceCell < u0.Length) u0[sourceCell] = 1000000;
            if (sinkCell >= 0 && sinkCell < u1.Length) u1[sinkCell] = 1000000;
            for (var i = 0; i < pw.Length; i++) pw[i] = 1;
            BuildBinaryEnergy(ref s, u0, u1, pw);
            var result = TryMinCut(ref s);
            u0.Dispose();
            u1.Dispose();
            pw.Dispose();
            return result;
        }

        [BurstCompile]
        public static bool TryMinCut(ref GraphCutState s)
        {
            var cellCount = s.Grid.Length;
            var nodeCount = cellCount + 2;
            var source = cellCount;
            var sink = cellCount + 1;

            UnsafeUtility.MemSet(s.Excess, 0, nodeCount * sizeof(int));
            UnsafeUtility.MemSet(s.Height, 0, nodeCount * sizeof(int));
            s.ActiveNodes.Clear();
            UnsafeUtility.MemSet(s.InActiveNodes, 0, nodeCount * sizeof(byte));

            var excessPtr = s.Excess;
            var heightPtr = s.Height;
            var headPtr = s.EdgeHead;
            var inActivePtr = s.InActiveNodes;
            var toPtr = s.EdgeTo.Ptr;
            var capPtr = s.EdgeCap.Ptr;
            var flowPtr = s.EdgeFlow.Ptr;
            var revPtr = s.EdgeRev.Ptr;
            var nextPtr = s.EdgeNext.Ptr;

            heightPtr[source] = nodeCount;

            for (var e = headPtr[source]; e != -1; e = nextPtr[e])
            {
                var v = toPtr[e];
                var cap = capPtr[e];
                flowPtr[e] = cap;
                flowPtr[revPtr[e]] = -cap;
                excessPtr[v] = cap;
                excessPtr[source] -= cap;
                if (v != sink && v != source && inActivePtr[v] == 0)
                {
                    s.ActiveNodes.Add(v);
                    inActivePtr[v] = 1;
                }
            }

            while (s.ActiveNodes.Length > 0)
            {
                var u = s.ActiveNodes[s.ActiveNodes.Length - 1];
                s.ActiveNodes.RemoveAtSwapBack(s.ActiveNodes.Length - 1);
                inActivePtr[u] = 0;

                while (excessPtr[u] > 0)
                {
                    var minHeight = int.MaxValue;
                    var pushed = false;

                    for (var e = headPtr[u]; e != -1; e = nextPtr[e])
                    {
                        var v = toPtr[e];
                        var resid = capPtr[e] - flowPtr[e];
                        if (resid > 0)
                        {
                            if (heightPtr[u] == heightPtr[v] + 1)
                            {
                                var push = math.min(excessPtr[u], resid);
                                flowPtr[e] += push;
                                flowPtr[revPtr[e]] -= push;
                                excessPtr[u] -= push;
                                excessPtr[v] += push;
                                if (v != source && v != sink && inActivePtr[v] == 0)
                                {
                                    s.ActiveNodes.Add(v);
                                    inActivePtr[v] = 1;
                                }

                                pushed = true;
                                if (excessPtr[u] == 0) break;
                            }

                            minHeight = math.min(minHeight, heightPtr[v]);
                        }
                    }

                    if (!pushed) heightPtr[u] = minHeight == int.MaxValue ? heightPtr[u] : minHeight + 1;
                }
            }

            UnsafeUtility.MemSet(s.SourceSide, 0, cellCount * sizeof(byte));
            var sidePtr = s.SourceSide;
            var queue = new NativeQueue<int>(Allocator.Temp);
            queue.Enqueue(source);

            var visited = new Unity.Collections.NativeArray<byte>(nodeCount, Allocator.Temp);
            visited[source] = 1;

            while (queue.TryDequeue(out var u))
                for (var e = headPtr[u]; e != -1; e = nextPtr[e])
                {
                    var v = toPtr[e];
                    if (visited[v] == 0 && capPtr[e] - flowPtr[e] > 0)
                    {
                        visited[v] = 1;
                        if (v < cellCount) sidePtr[v] = 1;
                        queue.Enqueue(v);
                    }
                }

            visited.Dispose();
            queue.Dispose();
            return true;
        }

        [BurstCompile]
        public static bool TryExtractCutLabels(ref GraphCutState s, ref NativeArray<int> labels, int label0, int label1)
        {
            var labelsPtr = (int*)labels.GetUnsafePtr();
            var sidePtr = s.SourceSide;
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++)
                labelsPtr[i] = sidePtr[i] == 1 ? label0 : label1;
            return true;
        }

        public static void Dispose(ref GraphCutState s)
        {
            s.EdgeTo.Dispose();
            s.EdgeCap.Dispose();
            s.EdgeFlow.Dispose();
            s.EdgeRev.Dispose();
            s.EdgeNext.Dispose();
            if (s.EdgeHead != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.EdgeHead); s.EdgeHead = null; }
            if (s.Excess != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Excess); s.Excess = null; }
            if (s.Height != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Height); s.Height = null; }
            if (s.SourceSide != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.SourceSide); s.SourceSide = null; }
            s.ActiveNodes.Dispose();
            if (s.InActiveNodes != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.InActiveNodes); s.InActiveNodes = null; }
        }
    }
}