using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.GraphCut;
using BovineLabs.Grid;

namespace BovineLabs.Grid.DynamicCut
{
    public struct DynamicCutState
    {
        public GraphCutState Cut;
        public NativeList<int> DirtyNodes;
        public NativeArray<int> UnarySource;
        public NativeArray<int> UnarySink;
    }

    public static class DynamicCutApi
    {
        public static DynamicCutState Create(int width, int height, int maxEdges, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new DynamicCutState
            {
                Cut = GraphCutApi.Create(width, height, maxEdges, a),
                DirtyNodes = new NativeList<int>(width * height, a),
                UnarySource = new NativeArray<int>(g.Length, a),
                UnarySink = new NativeArray<int>(g.Length, a),
            };
        }

        public static void EditUnary(ref DynamicCutState s, int cell, int sourceCapDelta, int sinkCapDelta)
        {
            s.UnarySource[cell] += sourceCapDelta;
            s.UnarySink[cell] += sinkCapDelta;
            if (!s.DirtyNodes.IsCreated) return;
            for (int i = 0; i < s.DirtyNodes.Length; i++)
                if (s.DirtyNodes[i] == cell) return;
            s.DirtyNodes.Add(cell);
        }

        public static unsafe void EditPairwise(ref DynamicCutState s, int a, int b, int capacityDelta)
        {
            // Walk adjacency list of node 'a' to find edge to 'b'
            int* head = (int*)s.Cut.EdgeHead.GetUnsafePtr();
            int* next = s.Cut.EdgeNext.Ptr;
            int* to = s.Cut.EdgeTo.Ptr;
            int* cap = s.Cut.EdgeCap.Ptr;

            int e = head[a];
            while (e >= 0)
            {
                if (to[e] == b && cap[e] > 0)
                {
                    int newCap = cap[e] + capacityDelta;
                    if (newCap < 0) newCap = 0;
                    cap[e] = newCap;
                    return;
                }
                e = next[e];
            }

            // Also try reverse direction (b -> a)
            e = head[b];
            while (e >= 0)
            {
                if (to[e] == a && cap[e] > 0)
                {
                    int newCap = cap[e] + capacityDelta;
                    if (newCap < 0) newCap = 0;
                    cap[e] = newCap;
                    return;
                }
                e = next[e];
            }
        }

        public static bool Repair(ref DynamicCutState s)
        {
            var u0 = new NativeArray<int>(s.UnarySource.Length, Allocator.Temp);
            var u1 = new NativeArray<int>(s.UnarySink.Length, Allocator.Temp);
            var pw = new NativeArray<int>(s.UnarySource.Length, Allocator.Temp);

            NativeArray<int>.Copy(s.UnarySource, u0);
            NativeArray<int>.Copy(s.UnarySink, u1);
            for (int i = 0; i < pw.Length; i++) pw[i] = 1;

            GraphCutApi.BuildBinaryEnergy(ref s.Cut, u0, u1, pw);
            bool result = GraphCutApi.MinCut(ref s.Cut);
            s.DirtyNodes.Clear();

            u0.Dispose(); u1.Dispose(); pw.Dispose();
            return result;
        }

        public static void Dispose(ref DynamicCutState s)
        {
            GraphCutApi.Dispose(ref s.Cut);
            if (s.DirtyNodes.IsCreated) s.DirtyNodes.Dispose();
            if (s.UnarySource.IsCreated) s.UnarySource.Dispose();
            if (s.UnarySink.IsCreated) s.UnarySink.Dispose();
        }
    }
}
