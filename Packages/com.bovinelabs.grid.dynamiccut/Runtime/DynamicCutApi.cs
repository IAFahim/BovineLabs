using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.GraphCut;

namespace BovineLabs.Grid.DynamicCut
{
    public struct DynamicCutState
    {
        public GraphCutState Cut;
        public NativeList<int> DirtyNodes;
        public NativeArray<int> UnarySource; // source t-link capacity per cell
        public NativeArray<int> UnarySink;   // sink t-link capacity per cell
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
            // Check if already dirty
            for (int i = 0; i < s.DirtyNodes.Length; i++)
                if (s.DirtyNodes[i] == cell) return;
            s.DirtyNodes.Add(cell);
        }

        public static void EditPairwise(ref DynamicCutState s, int a, int b, int capacityDelta)
        {
            // Find the edge between a and b and modify its capacity
            for (int i = 0; i < s.Cut.Edges.Length; i++)
            {
                // Check if this edge connects a to b or b to a
                var e = s.Cut.Edges[i];
                var rev = s.Cut.Edges[e.Reverse];
                if ((rev.To == a && e.To == b) || (rev.To == b && e.To == a))
                {
                    if (s.Cut.Edges[i].Capacity > 0)
                    {
                        int newCap = e.Capacity + capacityDelta;
                        if (newCap < 0) newCap = 0;
                        int newFlow = e.Flow;
                        if (newFlow > newCap) newFlow = newCap;
                        s.Cut.Edges[i] = new GraphCut.CutEdge
                        {
                            To = e.To,
                            Capacity = newCap,
                            Flow = newFlow,
                            Reverse = e.Reverse,
                        };
                    }
                }
            }
        }

        public static bool Repair(ref DynamicCutState s)
        {
            // Rebuild the graph with updated capacities
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
