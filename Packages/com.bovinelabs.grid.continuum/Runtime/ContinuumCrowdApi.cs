using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Continuum
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ContinuumCrowdState
    {
        public Grid2D Grid;
        public float* Density;
        public float* Speed;
        public float* Potential;
        public float2* Flow;
        public float* Divergence;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class ContinuumCrowdApi
    {
        public static bool TryCreate(int width, int height, Allocator a, out ContinuumCrowdState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new ContinuumCrowdState
            {
                Allocator = a,
                Grid = g,
                Density = (float*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(float), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<float>(), g.Length),
                Speed = (float*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(float), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<float>(), g.Length),
                Potential = (float*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(float), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<float>(), g.Length),
                Flow = (float2*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(float2), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<float2>(), g.Length),
                Divergence = (float*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(float), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<float>(), g.Length)
            };
            return true;
        }

        [BurstCompile]
        public static void ClearDensity(ref ContinuumCrowdState s)
        {
            var density = s.Density;
            var div = s.Divergence;
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++)
            {
                density[i] = 0f;
                div[i] = 0f;
            }
        }

        [BurstCompile]
        public static void SplatAgents(ref ContinuumCrowdState s, in NativeArray<float2> positions)
        {
            var density = s.Density;
            var pos = (float2*)positions.GetUnsafeReadOnlyPtr();
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            for (var i = 0; i < positions.Length; i++)
            {
                var cx = (int)math.floor(pos[i].x);
                var cy = (int)math.floor(pos[i].y);
                if (cx >= 0 && cx < w && cy >= 0 && cy < h)
                    density[cy * w + cx] += 1f;
            }
        }

        [BurstCompile]
        public static bool TrySolvePotential(ref ContinuumCrowdState s, in NativeArray<byte> blocked, int goal,
            int iterations)
        {
            if (!s.Grid.InBounds(goal) || !blocked.IsCreated) return false;

            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var len = s.Grid.Length;

            var pot = s.Potential;
            var spd = s.Speed;
            var dens = s.Density;
            var blk = (byte*)blocked.GetUnsafeReadOnlyPtr();

            for (var i = 0; i < len; i++)
                pot[i] = float.PositiveInfinity;

            for (var i = 0; i < len; i++)
            {
                if (blk[i] == 1)
                {
                    spd[i] = 0f;
                    continue;
                }

                spd[i] = 1f / (1f + dens[i] * 0.1f);
            }

            pot[goal] = 0f;

            for (var iter = 0; iter < iterations; iter++)
            {
                var idx = 0;
                for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    RelaxCell(pot, blk, spd, x, y, idx, w, h);
                    idx++;
                }

                idx = len - 1;
                for (var y = h - 1; y >= 0; y--)
                for (var x = w - 1; x >= 0; x--)
                {
                    RelaxCell(pot, blk, spd, x, y, idx, w, h);
                    idx--;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RelaxCell(float* pot, byte* blk, float* spd, int x, int y, int idx, int w, int h)
        {
            if (blk[idx] == 1) return;
            var sp = spd[idx];
            if (Hint.Unlikely(sp <= 0f)) return;

            var invSpeed = 1f / sp;

            var tx = float.PositiveInfinity;
            var ty = float.PositiveInfinity;

            if (x > 0)
            {
                var v = pot[idx - 1];
                if (v < tx) tx = v;
            }

            if (x < w - 1)
            {
                var v = pot[idx + 1];
                if (v < tx) tx = v;
            }

            if (y > 0)
            {
                var v = pot[idx - w];
                if (v < ty) ty = v;
            }

            if (y < h - 1)
            {
                var v = pot[idx + w];
                if (v < ty) ty = v;
            }

            float tNew;
            if (float.IsPositiveInfinity(tx))
            {
                tNew = ty + invSpeed;
            }
            else if (float.IsPositiveInfinity(ty))
            {
                tNew = tx + invSpeed;
            }
            else
            {
                var diff = math.abs(tx - ty);
                tNew = diff < invSpeed
                    ? (tx + ty + math.sqrt(2f * invSpeed * invSpeed - diff * diff)) * 0.5f
                    : math.min(tx, ty) + invSpeed;
            }

            if (tNew < pot[idx]) pot[idx] = tNew;
        }

        [BurstCompile]
        public static bool TryBuildFlow(ref ContinuumCrowdState s)
        {
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var len = s.Grid.Length;

            var pot = s.Potential;
            var flow = s.Flow;

            var idx = 0;
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var grad = float2.zero;

                if (x > 0 && x < w - 1)
                    grad.x = (pot[idx + 1] - pot[idx - 1]) * 0.5f;
                else if (x > 0)
                    grad.x = pot[idx] - pot[idx - 1];
                else if (x < w - 1)
                    grad.x = pot[idx + 1] - pot[idx];

                if (y > 0 && y < h - 1)
                    grad.y = (pot[idx + w] - pot[idx - w]) * 0.5f;
                else if (y > 0)
                    grad.y = pot[idx] - pot[idx - w];
                else if (y < h - 1)
                    grad.y = pot[idx + w] - pot[idx];

                var lenSq = math.lengthsq(grad);
                flow[idx] = lenSq > 0f ? -grad * math.rsqrt(lenSq) : float2.zero;

                idx++;
            }

            return true;
        }

        [BurstCompile]
        public static void AdvectAgents(ref ContinuumCrowdState s, ref NativeArray<float2> positions, float dt)
        {
            var flow = s.Flow;
            var pos = (float2*)positions.GetUnsafePtr();
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            for (var i = 0; i < positions.Length; i++)
            {
                var cx = (int)math.floor(pos[i].x);
                var cy = (int)math.floor(pos[i].y);
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                pos[i] += flow[cy * w + cx] * dt;
            }
        }

        public static void Dispose(ref ContinuumCrowdState s)
        {
            if (s.Density != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Density); s.Density = null; }
            if (s.Speed != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Speed); s.Speed = null; }
            if (s.Potential != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Potential); s.Potential = null; }
            if (s.Flow != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Flow); s.Flow = null; }
            if (s.Divergence != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Divergence); s.Divergence = null; }
        }
    }
}