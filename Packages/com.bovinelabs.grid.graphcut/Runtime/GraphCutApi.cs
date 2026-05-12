using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.GraphCut
{
    public struct GraphCutState
    {
        public Grid2D Grid;
        
        // Edge data (SoA)
        public UnsafeList<int> EdgeTo;
        public UnsafeList<int> EdgeCap;
        public UnsafeList<int> EdgeFlow;
        public UnsafeList<int> EdgeRev;
        public UnsafeList<int> EdgeNext;
        
        public NativeArray<int> EdgeHead; // nodes 0 to Length-1, Source=Length, Sink=Length+1
        public NativeArray<int> Excess;
        public NativeArray<int> Height;
        public NativeArray<byte> SourceSide;
        public UnsafeList<int> ActiveNodes;
    }

    [BurstCompile]
    public unsafe static class GraphCutApi
    {
        private const int SourceOffset = 0;
        private const int SinkOffset = 1;

        public static GraphCutState Create(int width, int height, int maxEdges, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            int nodeCount = g.Length + 2;
            return new GraphCutState
            {
                Grid = g,
                EdgeTo = new UnsafeList<int>(maxEdges, a),
                EdgeCap = new UnsafeList<int>(maxEdges, a),
                EdgeFlow = new UnsafeList<int>(maxEdges, a),
                EdgeRev = new UnsafeList<int>(maxEdges, a),
                EdgeNext = new UnsafeList<int>(maxEdges, a),
                EdgeHead = new NativeArray<int>(nodeCount, a),
                Excess = new NativeArray<int>(nodeCount, a),
                Height = new NativeArray<int>(nodeCount, a),
                SourceSide = new NativeArray<byte>(g.Length, a),
                ActiveNodes = new UnsafeList<int>(nodeCount, a),
            };
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
            s.EdgeHead.Fill(-1);

            int cellCount = s.Grid.Length;
            int sourceIdx = cellCount;
            int sinkIdx = cellCount + 1;

            int* unary0Ptr = (int*)unary0.GetUnsafeReadOnlyPtr();
            int* unary1Ptr = (int*)unary1.GetUnsafeReadOnlyPtr();
            int* pairwisePtr = (int*)pairwise.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < cellCount; i++)
            {
                if (unary0Ptr[i] > 0) AddEdge(ref s, sourceIdx, i, unary0Ptr[i]);
                if (unary1Ptr[i] > 0) AddEdge(ref s, i, sinkIdx, unary1Ptr[i]);
            }

            int width = s.Grid.Width;
            int height = s.Grid.Height;
            for (int i = 0; i < cellCount; i++)
            {
                int y = i / width;
                int x = i % width;
                int w = pairwisePtr[i];
                if (w <= 0) continue;

                // Only 2 directions to avoid double-counting
                for (int d = 0; d < 2; d++)
                {
                    int2 offset = Grid2D.Directions4[d];
                    int nx = x + offset.x;
                    int ny = y + offset.y;
                    if (Hint.Likely(nx >= 0 && ny >= 0 && nx < width && ny < height))
                    {
                        AddEdge(ref s, i, ny * width + nx, w);
                    }
                }
            }
        }

        private static void AddEdge(ref GraphCutState s, int u, int v, int cap)
        {
            int uvIdx = s.EdgeTo.Length;
            int vuIdx = uvIdx + 1;

            s.EdgeTo.Add(v); s.EdgeCap.Add(cap); s.EdgeFlow.Add(0); s.EdgeRev.Add(vuIdx); s.EdgeNext.Add(s.EdgeHead[u]);
            s.EdgeHead[u] = uvIdx;

            s.EdgeTo.Add(u); s.EdgeCap.Add(0); s.EdgeFlow.Add(0); s.EdgeRev.Add(uvIdx); s.EdgeNext.Add(s.EdgeHead[v]);
            s.EdgeHead[v] = vuIdx;
        }

        [BurstCompile]
        public static bool MinCut(ref GraphCutState s)
        {
            int cellCount = s.Grid.Length;
            int nodeCount = cellCount + 2;
            int source = cellCount;
            int sink = cellCount + 1;

            s.Excess.Fill(0);
            s.Height.Fill(0);
            s.ActiveNodes.Clear();

            int* excessPtr = (int*)s.Excess.GetUnsafePtr();
            int* heightPtr = (int*)s.Height.GetUnsafePtr();
            int* headPtr = (int*)s.EdgeHead.GetUnsafePtr();
            int* toPtr = s.EdgeTo.Ptr;
            int* capPtr = s.EdgeCap.Ptr;
            int* flowPtr = s.EdgeFlow.Ptr;
            int* revPtr = s.EdgeRev.Ptr;
            int* nextPtr = s.EdgeNext.Ptr;

            heightPtr[source] = nodeCount;

            // Initial pushes from source
            for (int e = headPtr[source]; e != -1; e = nextPtr[e])
            {
                int v = toPtr[e];
                int cap = capPtr[e];
                flowPtr[e] = cap;
                flowPtr[revPtr[e]] = -cap;
                excessPtr[v] = cap;
                excessPtr[source] -= cap;
                if (v != sink && v != source) s.ActiveNodes.Add(v);
            }

            while (s.ActiveNodes.Length > 0)
            {
                int u = s.ActiveNodes[s.ActiveNodes.Length - 1];
                s.ActiveNodes.RemoveAtSwapBack(s.ActiveNodes.Length - 1);

                while (excessPtr[u] > 0)
                {
                    int minHeight = int.MaxValue;
                    bool pushed = false;

                    for (int e = headPtr[u]; e != -1; e = nextPtr[e])
                    {
                        int v = toPtr[e];
                        int resid = capPtr[e] - flowPtr[e];
                        if (resid > 0)
                        {
                            if (heightPtr[u] == heightPtr[v] + 1)
                            {
                                int push = math.min(excessPtr[u], resid);
                                flowPtr[e] += push;
                                flowPtr[revPtr[e]] -= push;
                                excessPtr[u] -= push;
                                excessPtr[v] += push;
                                if (v != source && v != sink && !s.ActiveNodes.Contains(v))
                                    s.ActiveNodes.Add(v);
                                pushed = true;
                                if (excessPtr[u] == 0) break;
                            }
                            minHeight = math.min(minHeight, heightPtr[v]);
                        }
                    }

                    if (!pushed)
                    {
                        heightPtr[u] = (minHeight == int.MaxValue) ? heightPtr[u] : minHeight + 1;
                    }
                }
            }

            // Extract min-cut via BFS reachability from source in residual graph
            s.SourceSide.Fill((byte)0);
            byte* sidePtr = (byte*)s.SourceSide.GetUnsafePtr();
            var queue = new NativeQueue<int>(Allocator.Temp);
            queue.Enqueue(source);
            
            // Mark source as "visited" conceptually (virtual node)
            // But we only store SourceSide for cells
            
            var visited = new NativeArray<byte>(nodeCount, Allocator.Temp);
            visited[source] = 1;

            while (queue.TryDequeue(out int u))
            {
                for (int e = headPtr[u]; e != -1; e = nextPtr[e])
                {
                    int v = toPtr[e];
                    if (visited[v] == 0 && capPtr[e] - flowPtr[e] > 0)
                    {
                        visited[v] = 1;
                        if (v < cellCount) sidePtr[v] = 1;
                        queue.Enqueue(v);
                    }
                }
            }
            
            visited.Dispose();
            queue.Dispose();
            return true;
        }

        [BurstCompile]
        public static void ApplyCutLabels(ref GraphCutState s, ref NativeArray<int> labels, int label0, int label1)
        {
            int* labelsPtr = (int*)labels.GetUnsafePtr();
            byte* sidePtr = (byte*)s.SourceSide.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++)
                labelsPtr[i] = sidePtr[i] == 1 ? label0 : label1;
        }

        public static void Dispose(ref GraphCutState s)
        {
            s.EdgeTo.Dispose();
            s.EdgeCap.Dispose();
            s.EdgeFlow.Dispose();
            s.EdgeRev.Dispose();
            s.EdgeNext.Dispose();
            if (s.EdgeHead.IsCreated) s.EdgeHead.Dispose();
            if (s.Excess.IsCreated) s.Excess.Dispose();
            if (s.Height.IsCreated) s.Height.Dispose();
            if (s.SourceSide.IsCreated) s.SourceSide.Dispose();
            s.ActiveNodes.Dispose();
        }
    }
}
