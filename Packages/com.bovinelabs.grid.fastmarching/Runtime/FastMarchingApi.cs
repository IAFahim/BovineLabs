using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.FastMarching
{
    public struct FastMarchingState
    {
        public Grid2D Grid;
        public NativeArray<float> T;
        public NativeArray<byte> State; // 0 far, 1 trial, 2 accepted
        public MinHeap Heap;
    }

    public static class FastMarchingApi
    {
        public static FastMarchingState Create(int width, int height, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new FastMarchingState
            {
                Grid = g,
                T = new NativeArray<float>(g.Length, a),
                State = new NativeArray<byte>(g.Length, a),
                Heap = MinHeap.Create(g.Length, a),
            };
        }

        public static void InitializeSources(ref FastMarchingState s, NativeArray<int> sources)
        {
            s.T.Fill(float.PositiveInfinity);
            s.State.Fill((byte)0);
            s.Heap.Clear();

            for (int i = 0; i < sources.Length; i++)
            {
                int src = sources[i];
                s.T[src] = 0f;
                s.State[src] = 1;
                s.Heap.InsertOrDecrease(new HeapNode(src, 0f));
            }
        }

        public static bool PropagateStep(ref FastMarchingState s, NativeArray<float> speed)
        {
            if (s.Heap.IsEmpty) return false;

            int u = s.Heap.Pop().Id;
            s.State[u] = 2; // accepted

            int2 up = s.Grid.ToCoord(u);
            for (int d = 0; d < 4; d++)
            {
                int2 np = up + Grid2D.Directions4[d];
                if (!s.Grid.InBounds(np)) continue;
                int ni = s.Grid.ToIndex(np);
                if (s.State[ni] == 2) continue; // already accepted

                float tNew = SolveEikonal(ref s, speed, np);
                if (tNew < s.T[ni])
                {
                    s.T[ni] = tNew;
                    s.State[ni] = 1;
                    s.Heap.InsertOrDecrease(new HeapNode(ni, tNew));
                }
            }

            return !s.Heap.IsEmpty;
        }

        public static void PropagateAll(ref FastMarchingState s, NativeArray<float> speed)
        {
            while (PropagateStep(ref s, speed)) { }
        }

        private static float SolveEikonal(ref FastMarchingState s, NativeArray<float> speed, int2 p)
        {
            int idx = s.Grid.ToIndex(p);
            float sp = speed[idx];
            if (sp <= 0f) return float.PositiveInfinity;

            float tx = float.PositiveInfinity;
            float ty = float.PositiveInfinity;

            // Check x neighbors
            int2 px1 = new int2(p.x - 1, p.y);
            int2 px2 = new int2(p.x + 1, p.y);
            if (s.Grid.InBounds(px1) && s.State[s.Grid.ToIndex(px1)] == 2)
                tx = math.min(tx, s.T[s.Grid.ToIndex(px1)]);
            if (s.Grid.InBounds(px2) && s.State[s.Grid.ToIndex(px2)] == 2)
                tx = math.min(tx, s.T[s.Grid.ToIndex(px2)]);

            // Check y neighbors
            int2 py1 = new int2(p.x, p.y - 1);
            int2 py2 = new int2(p.x, p.y + 1);
            if (s.Grid.InBounds(py1) && s.State[s.Grid.ToIndex(py1)] == 2)
                ty = math.min(ty, s.T[s.Grid.ToIndex(py1)]);
            if (s.Grid.InBounds(py2) && s.State[s.Grid.ToIndex(py2)] == 2)
                ty = math.min(ty, s.T[s.Grid.ToIndex(py2)]);

            float invSpeed = 1f / sp;

            // Quadratic solve: (t - tx)^2 + (t - ty)^2 = invSpeed^2
            if (float.IsPositiveInfinity(tx)) return ty + invSpeed;
            if (float.IsPositiveInfinity(ty)) return tx + invSpeed;

            float diff = math.abs(tx - ty);
            if (diff < invSpeed)
            {
                float t = (tx + ty + math.sqrt(2f * invSpeed * invSpeed - diff * diff)) * 0.5f;
                return t;
            }

            return math.min(tx, ty) + invSpeed;
        }

        public static void BuildGradientFlow(ref FastMarchingState s, NativeArray<float2> flow)
        {
            for (int i = 0; i < s.Grid.Length; i++)
            {
                int2 p = s.Grid.ToCoord(i);
                float2 grad = float2.zero;

                // Central differences
                if (p.x > 0 && p.x < s.Grid.Width - 1)
                    grad.x = (s.T[s.Grid.ToIndex(p.x + 1, p.y)] - s.T[s.Grid.ToIndex(p.x - 1, p.y)]) * 0.5f;
                else if (p.x > 0)
                    grad.x = s.T[i] - s.T[s.Grid.ToIndex(p.x - 1, p.y)];
                else if (p.x < s.Grid.Width - 1)
                    grad.x = s.T[s.Grid.ToIndex(p.x + 1, p.y)] - s.T[i];

                if (p.y > 0 && p.y < s.Grid.Height - 1)
                    grad.y = (s.T[s.Grid.ToIndex(p.x, p.y + 1)] - s.T[s.Grid.ToIndex(p.x, p.y - 1)]) * 0.5f;
                else if (p.y > 0)
                    grad.y = s.T[i] - s.T[s.Grid.ToIndex(p.x, p.y - 1)];
                else if (p.y < s.Grid.Height - 1)
                    grad.y = s.T[s.Grid.ToIndex(p.x, p.y + 1)] - s.T[i];

                float len = math.length(grad);
                flow[i] = len > 0f ? -grad / len : float2.zero;
            }
        }

        public static void Dispose(ref FastMarchingState s)
        {
            if (s.T.IsCreated) s.T.Dispose();
            if (s.State.IsCreated) s.State.Dispose();
            if (s.Heap.IsCreated) s.Heap.Dispose();
        }
    }
}
