using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.FieldDStar
{
    public struct FieldDStarState
    {
        public Grid2D Grid;
        public int Goal;
        public NativeArray<float> G;
        public NativeArray<float> RHS;
        public NativeArray<float2> Flow;
        public MinHeap Heap;
    }

    public static class FieldDStarApi
    {
        public static FieldDStarState Create(int width, int height, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new FieldDStarState
            {
                Grid = g,
                G = new NativeArray<float>(g.Length, a),
                RHS = new NativeArray<float>(g.Length, a),
                Flow = new NativeArray<float2>(g.Length, a),
                Heap = MinHeap.Create(g.Length, a),
            };
        }

        public static void Reset(ref FieldDStarState s)
        {
            s.Heap.Clear();
            s.G.Fill(float.PositiveInfinity);
            s.RHS.Fill(float.PositiveInfinity);
            s.Flow.Fill(float2.zero);
        }

        public static void SetGoal(ref FieldDStarState s, int goal)
        {
            s.Goal = goal;
            s.RHS[goal] = 0f;
            s.Heap.InsertOrDecrease(new HeapNode(goal, 0f));
        }

        public static bool Step(ref FieldDStarState s, NativeArray<float> cost)
        {
            if (s.Heap.IsEmpty) return false;

            int u = s.Heap.Pop().Id;

            if (s.G[u] > s.RHS[u])
            {
                s.G[u] = s.RHS[u];
            }
            else
            {
                s.G[u] = float.PositiveInfinity;
                UpdateRHS(ref s, cost, u);
            }

            // Update neighbors
            int2 p = s.Grid.ToCoord(u);
            for (int d = 0; d < 8; d++)
            {
                int2 np = p + Grid2D.Directions8[d];
                if (s.Grid.InBounds(np))
                    UpdateRHS(ref s, cost, s.Grid.ToIndex(np));
            }

            return !s.Heap.IsEmpty;
        }

        private static void UpdateRHS(ref FieldDStarState s, NativeArray<float> cost, int cell)
        {
            // Goal's RHS is always 0 — never overwrite
            if (cell == s.Goal) return;

            float bestRHS = float.PositiveInfinity;
            int2 p = s.Grid.ToCoord(cell);

            for (int d = 0; d < 8; d++)
            {
                int2 np = p + Grid2D.Directions8[d];
                if (!s.Grid.InBounds(np)) continue;
                int ni = s.Grid.ToIndex(np);
                float c = (d < 4) ? 1f : 1.414f;
                if (cost.Length > 0) c *= (cost[cell] + cost[ni]) * 0.5f;
                float val = s.G[ni] + c;
                if (val < bestRHS) bestRHS = val;
            }

            s.RHS[cell] = bestRHS;

            if (s.G[cell] != s.RHS[cell])
            {
                float key = math.min(s.G[cell], s.RHS[cell]);
                s.Heap.InsertOrDecrease(new HeapNode(cell, key));
            }
        }

        public static void ExtractFlow(ref FieldDStarState s, NativeArray<float> cost)
        {
            for (int i = 0; i < s.Grid.Length; i++)
            {
                int2 p = s.Grid.ToCoord(i);
                float2 grad = float2.zero;

                for (int d = 0; d < 8; d++)
                {
                    int2 np = p + Grid2D.Directions8[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);

                    // Finite-difference gradient of G: direction from high G to low G
                    float2 dir = math.normalize(new float2(Grid2D.Directions8[d].x, Grid2D.Directions8[d].y));
                    float diff = s.G[i] - s.G[ni]; // positive means ni is better (lower G)
                    grad += dir * diff;
                }

                float len = math.length(grad);
                // Flow points toward lower G (toward goal)
                s.Flow[i] = len > 0f ? grad / len : float2.zero;
            }
        }

        public static void Dispose(ref FieldDStarState s)
        {
            if (s.G.IsCreated) s.G.Dispose();
            if (s.RHS.IsCreated) s.RHS.Dispose();
            if (s.Flow.IsCreated) s.Flow.Dispose();
            if (s.Heap.IsCreated) s.Heap.Dispose();
        }
    }
}
