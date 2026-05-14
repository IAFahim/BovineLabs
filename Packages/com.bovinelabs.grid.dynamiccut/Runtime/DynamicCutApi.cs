using BovineLabs.Grid.GraphCut;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BovineLabs.Grid.DynamicCut
{
    public unsafe struct DynamicCutState
    {
        public GraphCutState Cut;
        public NativeList<int> DirtyNodes;
        public int* UnarySource;
        public int* UnarySink;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    public static unsafe class DynamicCutApi
    {
        public static bool TryCreate(int width, int height, int maxEdges, Allocator a, out DynamicCutState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            if (!GraphCutApi.TryCreate(width, height, maxEdges, a, out var cut))
            {
                result = default;
                return false;
            }

            result = new DynamicCutState
            {
                Allocator = a,
                Cut = cut,
                DirtyNodes = new NativeList<int>(width * height, a),
                UnarySource = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length),
                UnarySink = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length)
            };
            return true;
        }

        public static void EditUnary(ref DynamicCutState s, int cell, int sourceCapDelta, int sinkCapDelta)
        {
            s.UnarySource[cell] += sourceCapDelta;
            s.UnarySink[cell] += sinkCapDelta;
            if (!s.DirtyNodes.IsCreated) return;
            for (var i = 0; i < s.Cut.Grid.Length; i++)
                if (s.DirtyNodes[i] == cell)
                    return;
            s.DirtyNodes.Add(cell);
        }

        public static unsafe void EditPairwise(ref DynamicCutState s, int a, int b, int capacityDelta)
        {
            var head = (int*)s.Cut.EdgeHead;
            var next = s.Cut.EdgeNext.Ptr;
            var to = s.Cut.EdgeTo.Ptr;
            var cap = s.Cut.EdgeCap.Ptr;

            var e = head[a];
            while (e >= 0)
            {
                if (to[e] == b && cap[e] > 0)
                {
                    var newCap = cap[e] + capacityDelta;
                    if (newCap < 0) newCap = 0;
                    cap[e] = newCap;
                    return;
                }

                e = next[e];
            }

            e = head[b];
            while (e >= 0)
            {
                if (to[e] == a && cap[e] > 0)
                {
                    var newCap = cap[e] + capacityDelta;
                    if (newCap < 0) newCap = 0;
                    cap[e] = newCap;
                    return;
                }

                e = next[e];
            }
        }

        public static bool TryRepair(ref DynamicCutState s)
        {
            var u0 = new Unity.Collections.NativeArray<int>(s.Cut.Grid.Length, Allocator.Temp);
            var u1 = new Unity.Collections.NativeArray<int>(s.Cut.Grid.Length, Allocator.Temp);
            var pw = new Unity.Collections.NativeArray<int>(s.Cut.Grid.Length, Allocator.Temp);

            Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(u0.GetUnsafePtr(), s.UnarySource, s.Cut.Grid.Length * sizeof(int));
            Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(u1.GetUnsafePtr(), s.UnarySink, s.Cut.Grid.Length * sizeof(int));
            for (var i = 0; i < pw.Length; i++) pw[i] = 1;

            GraphCutApi.BuildBinaryEnergy(ref s.Cut, u0, u1, pw);
            var result = GraphCutApi.TryMinCut(ref s.Cut);
            s.DirtyNodes.Clear();

            u0.Dispose();
            u1.Dispose();
            pw.Dispose();
            return result;
        }

        public static void Dispose(ref DynamicCutState s)
        {
            GraphCutApi.Dispose(ref s.Cut);
            if (s.DirtyNodes.IsCreated) s.DirtyNodes.Dispose();
            if (s.UnarySource != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.UnarySource); s.UnarySource = null; }
            if (s.UnarySink != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.UnarySink); s.UnarySink = null; }
        }
    }
}