using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Continuum
{
    public struct ContinuumCrowdState
    {
        public Grid2D Grid;
        public NativeArray<float> Density;
        public NativeArray<float> Speed;
        public NativeArray<float> Potential;
        public NativeArray<float2> Flow;
        public NativeArray<float> Divergence;
    }

    public static class ContinuumCrowdApi
    {
        public static ContinuumCrowdState Create(int width, int height, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new ContinuumCrowdState
            {
                Grid = g,
                Density = new NativeArray<float>(g.Length, a),
                Speed = new NativeArray<float>(g.Length, a),
                Potential = new NativeArray<float>(g.Length, a),
                Flow = new NativeArray<float2>(g.Length, a),
                Divergence = new NativeArray<float>(g.Length, a),
            };
        }

        public static void ClearDensity(ref ContinuumCrowdState s)
        {
            s.Density.Fill(0f);
            s.Divergence.Fill(0f);
        }

        public static void SplatAgents(ref ContinuumCrowdState s, NativeArray<float2> positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                int2 cell = (int2)math.floor(positions[i]);
                if (s.Grid.InBounds(cell))
                    s.Density[s.Grid.ToIndex(cell)] += 1f;
            }
        }

        public static void SolvePotential(ref ContinuumCrowdState s, NativeArray<byte> blocked, int goal, int iterations)
        {
            s.Potential.Fill(float.PositiveInfinity);

            // Compute speed from density
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (blocked[i] == 1) { s.Speed[i] = 0f; continue; }
                float congestion = 1f + s.Density[i] * 0.1f;
                s.Speed[i] = 1f / congestion;
            }

            s.Potential[goal] = 0f;

            // Gauss-Seidel Fast Sweeping (simplified)
            for (int iter = 0; iter < iterations; iter++)
            {
                // Forward sweep
                for (int y = 0; y < s.Grid.Height; y++)
                    for (int x = 0; x < s.Grid.Width; x++)
                        RelaxCell(ref s, blocked, x, y);

                // Backward sweep
                for (int y = s.Grid.Height - 1; y >= 0; y--)
                    for (int x = s.Grid.Width - 1; x >= 0; x--)
                        RelaxCell(ref s, blocked, x, y);
            }
        }

        private static void RelaxCell(ref ContinuumCrowdState s, NativeArray<byte> blocked, int x, int y)
        {
            int idx = s.Grid.ToIndex(x, y);
            if (blocked[idx] == 1) return;
            if (s.Speed[idx] <= 0f) return;

            float invSpeed = 1f / s.Speed[idx];
            float tx = float.PositiveInfinity;
            float ty = float.PositiveInfinity;

            if (x > 0) tx = math.min(tx, s.Potential[s.Grid.ToIndex(x - 1, y)]);
            if (x < s.Grid.Width - 1) tx = math.min(tx, s.Potential[s.Grid.ToIndex(x + 1, y)]);
            if (y > 0) ty = math.min(ty, s.Potential[s.Grid.ToIndex(x, y - 1)]);
            if (y < s.Grid.Height - 1) ty = math.min(ty, s.Potential[s.Grid.ToIndex(x, y + 1)]);

            float tNew;
            if (float.IsPositiveInfinity(tx)) tNew = ty + invSpeed;
            else if (float.IsPositiveInfinity(ty)) tNew = tx + invSpeed;
            else
            {
                float diff = math.abs(tx - ty);
                if (diff < invSpeed)
                    tNew = (tx + ty + math.sqrt(2f * invSpeed * invSpeed - diff * diff)) * 0.5f;
                else
                    tNew = math.min(tx, ty) + invSpeed;
            }

            if (tNew < s.Potential[idx])
                s.Potential[idx] = tNew;
        }

        public static void BuildFlow(ref ContinuumCrowdState s)
        {
            for (int i = 0; i < s.Grid.Length; i++)
            {
                int2 p = s.Grid.ToCoord(i);
                float2 grad = float2.zero;

                if (p.x > 0 && p.x < s.Grid.Width - 1)
                    grad.x = (s.Potential[s.Grid.ToIndex(p.x + 1, p.y)] - s.Potential[s.Grid.ToIndex(p.x - 1, p.y)]) * 0.5f;
                else if (p.x > 0)
                    grad.x = s.Potential[i] - s.Potential[s.Grid.ToIndex(p.x - 1, p.y)];
                else if (p.x < s.Grid.Width - 1)
                    grad.x = s.Potential[s.Grid.ToIndex(p.x + 1, p.y)] - s.Potential[i];

                if (p.y > 0 && p.y < s.Grid.Height - 1)
                    grad.y = (s.Potential[s.Grid.ToIndex(p.x, p.y + 1)] - s.Potential[s.Grid.ToIndex(p.x, p.y - 1)]) * 0.5f;
                else if (p.y > 0)
                    grad.y = s.Potential[i] - s.Potential[s.Grid.ToIndex(p.x, p.y - 1)];
                else if (p.y < s.Grid.Height - 1)
                    grad.y = s.Potential[s.Grid.ToIndex(p.x, p.y + 1)] - s.Potential[i];

                float len = math.length(grad);
                s.Flow[i] = len > 0f ? -grad / len : float2.zero;
            }
        }

        public static void AdvectAgents(ref ContinuumCrowdState s, NativeArray<float2> positions, float dt)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                int2 cell = (int2)math.floor(positions[i]);
                if (!s.Grid.InBounds(cell)) continue;
                int idx = s.Grid.ToIndex(cell);
                positions[i] += s.Flow[idx] * dt;
            }
        }

        public static void Dispose(ref ContinuumCrowdState s)
        {
            if (s.Density.IsCreated) s.Density.Dispose();
            if (s.Speed.IsCreated) s.Speed.Dispose();
            if (s.Potential.IsCreated) s.Potential.Dispose();
            if (s.Flow.IsCreated) s.Flow.Dispose();
            if (s.Divergence.IsCreated) s.Divergence.Dispose();
        }
    }
}
