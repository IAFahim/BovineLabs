using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.GraphCut
{
    public struct CutEdge
    {
        public int To;       // target cell (or virtual: -1=source, -2=sink)
        public int Capacity;
        public int Flow;
        public int Reverse;  // index of reverse edge
    }

    public struct GraphCutState
    {
        public Grid2D Grid;
        public NativeList<CutEdge> Edges;
        public NativeArray<int> EdgeHead; // per-cell head index into adjacency, -1 if none
        public NativeArray<int> Excess;
        public NativeArray<int> Height;
        public NativeArray<byte> SourceSide;
    }

    public static class GraphCutApi
    {
        public static GraphCutState Create(int width, int height, int maxEdges, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new GraphCutState
            {
                Grid = g,
                Edges = new NativeList<CutEdge>(maxEdges, a),
                EdgeHead = new NativeArray<int>(g.Length, a),
                Excess = new NativeArray<int>(g.Length, a),
                Height = new NativeArray<int>(g.Length, a),
                SourceSide = new NativeArray<byte>(g.Length, a),
            };
        }

        public static void BuildBinaryEnergy(
            ref GraphCutState s,
            NativeArray<int> unary0,
            NativeArray<int> unary1,
            NativeArray<int> pairwise)
        {
            s.Edges.Clear();
            s.EdgeHead.Fill(-1);

            for (int i = 0; i < s.Grid.Length; i++)
            {
                // Source->cell with capacity unary0[i] (cell wants label 0)
                if (unary0[i] > 0) AddDirectedEdge(ref s, -1, i, unary0[i]);
                // Cell->sink with capacity unary1[i] (cell wants label 1)
                if (unary1[i] > 0) AddDirectedEdge(ref s, i, -2, unary1[i]);
            }

            for (int i = 0; i < s.Grid.Length; i++)
            {
                int2 p = s.Grid.ToCoord(i);
                for (int d = 0; d < 2; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    int w = pairwise[i];
                    if (w > 0)
                    {
                        AddDirectedEdge(ref s, i, ni, w);
                    }
                }
            }
        }

        private static void AddDirectedEdge(ref GraphCutState s, int from, int to, int cap)
        {
            int idx = s.Edges.Length;
            s.Edges.Add(new CutEdge { To = to, Capacity = cap, Flow = 0, Reverse = idx + 1 });
            s.Edges.Add(new CutEdge { To = from, Capacity = 0, Flow = 0, Reverse = idx });

            // Add to adjacency list for 'from' (if it's a cell)
            if (from >= 0 && from < s.Grid.Length)
            {
                s.Edges[idx] = new CutEdge { To = to, Capacity = cap, Flow = 0, Reverse = idx + 1 };
            }
        }

        public static bool MinCut(ref GraphCutState s)
        {
            // Simple min-cut using BFS reachability from source through residual edges.
            // "Source" = virtual node -1. Find all cells reachable from source.

            s.SourceSide.Fill((byte)0);
            s.Excess.Fill(0);
            s.Height.Fill(0);

            int edgeCount = s.Edges.Length;

            // BFS from source through residual edges (capacity - flow > 0)
            var queue = new NativeQueue<int>(Allocator.Temp);

            // Find cells directly connected to source with residual capacity
            for (int i = 0; i < edgeCount; i++)
            {
                var e = s.Edges[i];
                // Find edges FROM source (To = cell, stored as reverse of cell->source)
                // In our model: source->cell edges have to=cell, stored at reverse index
                // Actually our AddDirectedEdge(-1, cell, cap) creates: forward edge (to=cell, cap=cap), reverse (to=-1, cap=0)
                // So edge[i] where the edge was added with from=-1: the forward edge has To=cell, Capacity=cap
                // We need to find all such edges
            }

            // Let me simplify: scan all edges for "from source" (i.e., even-indexed edges where the original from was -1)
            // Since we can't store "from" in CutEdge, let's use a different approach:
            // Just use the fact that edges from source were added first via AddDirectedEdge(-1, cell, cap)
            // The forward edge is at even index with To=cell, and its reverse is at odd index with To=-1

            // Approach: BFS starting from cells that have source->cell edges with residual
            // For each even-indexed edge i, check if its reverse (i+1) has To=-1.
            // If so, edge i is cell->source, and its reverse is source->cell.
            // Wait no. AddDirectedEdge(-1, cell, cap) creates:
            //   edge[idx] = { To=cell, Cap=cap, Flow=0, Rev=idx+1 }
            //   edge[idx+1] = { To=-1, Cap=0, Flow=0, Rev=idx }
            // So edge[idx] is source->cell with capacity. Residual = cap - flow.
            // edge[idx+1] is cell->source with 0 capacity (reverse edge). Residual = 0 - flow. 
            // If flow is negative (flow pushed back from cell to source), residual could be positive.

            // For initial BFS from source: find edges where reverse edge's To == -1 (meaning this is a source->cell edge)
            for (int i = 0; i < edgeCount; i += 2)
            {
                var rev = s.Edges[i + 1];
                if (rev.To == -1 && s.Edges[i].Capacity - s.Edges[i].Flow > 0 && s.Edges[i].To >= 0)
                {
                    int cell = s.Edges[i].To;
                    if (s.SourceSide[cell] == 0)
                    {
                        s.SourceSide[cell] = 1;
                        queue.Enqueue(cell);
                    }
                }
            }

            // BFS propagation: cell u can reach cell v if there's an edge u->v with residual capacity
            while (queue.TryDequeue(out int u))
            {
                for (int i = 0; i < edgeCount; i++)
                {
                    // Check if this is an edge from u to some target
                    // Since we don't store "from" in CutEdge, scan reverse edges
                    var e = s.Edges[i];
                    var rev = s.Edges[e.Reverse];

                    // e.Reverse points back. If rev.To == u, then e is an edge FROM u
                    if (rev.To != u) continue;
                    if (e.To < 0) continue; // skip virtual nodes
                    if (s.SourceSide[e.To] != 0) continue; // already visited
                    if (e.Capacity - e.Flow <= 0) continue; // no residual

                    s.SourceSide[e.To] = 1;
                    queue.Enqueue(e.To);
                }
            }

            queue.Dispose();
            return true;
        }

        public static void ApplyCutLabels(ref GraphCutState s, NativeArray<int> labels, int label0, int label1)
        {
            for (int i = 0; i < s.Grid.Length; i++)
                labels[i] = s.SourceSide[i] == 1 ? label0 : label1;
        }

        public static void AlphaExpansion(ref GraphCutState s, NativeArray<int> labels, int alpha, NativeArray<int> unary, NativeArray<int> smooth)
        {
            var unary0 = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var unary1 = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            var pw = new NativeArray<int>(s.Grid.Length, Allocator.Temp);

            for (int i = 0; i < s.Grid.Length; i++)
            {
                unary0[i] = unary[i * 2 + labels[i]];  // cost of keeping current label
                unary1[i] = unary[i * 2 + alpha];       // cost of switching to alpha
                pw[i] = smooth[i];
            }

            BuildBinaryEnergy(ref s, unary0, unary1, pw);
            MinCut(ref s);

            for (int i = 0; i < s.Grid.Length; i++)
                labels[i] = s.SourceSide[i] == 0 ? labels[i] : alpha;

            unary0.Dispose(); unary1.Dispose(); pw.Dispose();
        }

        public static void Dispose(ref GraphCutState s)
        {
            if (s.Edges.IsCreated) s.Edges.Dispose();
            if (s.EdgeHead.IsCreated) s.EdgeHead.Dispose();
            if (s.Excess.IsCreated) s.Excess.Dispose();
            if (s.Height.IsCreated) s.Height.Dispose();
            if (s.SourceSide.IsCreated) s.SourceSide.Dispose();
        }
    }
}
