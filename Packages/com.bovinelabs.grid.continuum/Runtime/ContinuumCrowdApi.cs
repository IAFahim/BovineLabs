using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Continuum
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ContinuumCrowdState
    {
        public Grid2D Grid;
        public NativeArray<float> Density;
        public NativeArray<float> Speed;
        public NativeArray<float> Potential;
        public NativeArray<float2> Flow;
        public NativeArray<float> Divergence;
    }

    [BurstCompile]
    public unsafe static class ContinuumCrowdApi
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

        [BurstCompile]
        public static void ClearDensity(ref ContinuumCrowdState s)
        {
            float* density = (float*)s.Density.GetUnsafePtr();
            float* div = (float*)s.Divergence.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++) { density[i] = 0f; div[i] = 0f; }
        }

        [BurstCompile]
        public static void SplatAgents(ref ContinuumCrowdState s, NativeArray<float2> positions)
        {
            float* density = (float*)s.Density.GetUnsafePtr();
            float2* pos = (float2*)positions.GetUnsafeReadOnlyPtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            for (int i = 0; i < positions.Length; i++)
            {
                int cx = (int)math.floor(pos[i].x);
                int cy = (int)math.floor(pos[i].y);
                if (cx >= 0 && cx < w && cy >= 0 && cy < h)
                    density[cy * w + cx] += 1f;
            }
        }

        [BurstCompile]
        public static void SolvePotential(ref ContinuumCrowdState s, NativeArray<byte> blocked, int goal, int iterations)
        {
            float* pot = (float*)s.Potential.GetUnsafePtr();
            float* spd = (float*)s.Speed.GetUnsafePtr();
            float* dens = (float*)s.Density.GetUnsafePtr();
            byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;

            for (int i = 0; i < len; i++) pot[i] = float.PositiveInfinity;

            for (int i = 0; i < len; i++)
            {
                if (blk[i] == 1) { spd[i] = 0f; continue; }
                spd[i] = 1f / (1f + dens[i] * 0.1f);
            }

            pot[goal] = 0f;

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        RelaxCellCore(pot, blk, spd, x, y, w, h);

                for (int y = h - 1; y >= 0; y--)
                    for (int x = w - 1; x >= 0; x--)
                        RelaxCellCore(pot, blk, spd, x, y, w, h);
            }
        }

        private static void RelaxCellCore(float* pot, byte* blk, float* spd, int x, int y, int w, int h)
        {
            int idx = y * w + x;
            if (blk[idx] == 1) return;
            float sp = spd[idx];
            if (Hint.Unlikely(sp <= 0f)) return;

            float invSpeed = 1f / sp;
            float tx = float.PositiveInfinity;
            float ty = float.PositiveInfinity;

            if (x > 0) { float v = pot[idx - 1]; if (v < tx) tx = v; }
            if (x < w - 1) { float v = pot[idx + 1]; if (v < tx) tx = v; }
            if (y > 0) { float v = pot[idx - w]; if (v < ty) ty = v; }
            if (y < h - 1) { float v = pot[idx + w]; if (v < ty) ty = v; }

            float tNew;
            if (float.IsPositiveInfinity(tx)) tNew = ty + invSpeed;
            else if (float.IsPositiveInfinity(ty)) tNew = tx + invSpeed;
            else
            {
                float diff = math.abs(tx - ty);
                tNew = diff < invSpeed
                    ? (tx + ty + math.sqrt(2f * invSpeed * invSpeed - diff * diff)) * 0.5f
                    : math.min(tx, ty) + invSpeed;
            }

            if (tNew < pot[idx]) pot[idx] = tNew;
        }

        [BurstCompile]
        public static void BuildFlow(ref ContinuumCrowdState s)
        {
            float* pot = (float*)s.Potential.GetUnsafePtr();
            float2* flow = (float2*)s.Flow.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;

            for (int i = 0; i < len; i++)
            {
                int x = i % w;
                int y = i / w;
                float2 grad = float2.zero;

                if (x > 0 && x < w - 1)
                    grad.x = (pot[i + 1] - pot[i - 1]) * 0.5f;
                else if (x > 0)
                    grad.x = pot[i] - pot[i - 1];
                else if (x < w - 1)
                    grad.x = pot[i + 1] - pot[i];

                if (y > 0 && y < h - 1)
                    grad.y = (pot[i + w] - pot[i - w]) * 0.5f;
                else if (y > 0)
                    grad.y = pot[i] - pot[i - w];
                else if (y < h - 1)
                    grad.y = pot[i + w] - pot[i];

                float lenSq = math.lengthsq(grad);
                flow[i] = lenSq > 0f ? -grad * math.rsqrt(lenSq) : float2.zero;
            }
        }

        [BurstCompile]
        public static void AdvectAgents(ref ContinuumCrowdState s, NativeArray<float2> positions, float dt)
        {
            float2* flow = (float2*)s.Flow.GetUnsafePtr();
            float2* pos = (float2*)positions.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            for (int i = 0; i < positions.Length; i++)
            {
                int cx = (int)math.floor(pos[i].x);
                int cy = (int)math.floor(pos[i].y);
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                pos[i] += flow[cy * w + cx] * dt;
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
