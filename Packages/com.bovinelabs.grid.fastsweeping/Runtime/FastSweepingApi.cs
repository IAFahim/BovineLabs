using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.FastSweeping
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FastSweepingState
    {
        public Grid2D Grid;
        public NativeArray<float> T;
    }

    [BurstCompile]
    public unsafe static class FastSweepingApi
    {
        public static FastSweepingState Create(int width, int height, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new FastSweepingState
            {
                Grid = g,
                T = new NativeArray<float>(g.Length, a),
            };
        }

        [BurstCompile]
        public static void Initialize(ref FastSweepingState s, NativeArray<int> sources)
        {
            float* t = (float*)s.T.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++) t[i] = float.PositiveInfinity;
            int* src = (int*)sources.GetUnsafeReadOnlyPtr();
            for (int i = 0; i < sources.Length; i++) t[src[i]] = 0f;
        }

        [BurstCompile]
        public static void SweepAllDirections(ref FastSweepingState s, NativeArray<float> speed, int rounds)
        {
            float* t = (float*)s.T.GetUnsafePtr();
            float* sp = (float*)speed.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;

            for (int r = 0; r < rounds; r++)
            {
                Sweep(t, sp, w, h, 1, 1);
                Sweep(t, sp, w, h, -1, 1);
                Sweep(t, sp, w, h, 1, -1);
                Sweep(t, sp, w, h, -1, -1);
            }
        }

        private static void Sweep(float* t, float* sp, int w, int h, int dx, int dy)
        {
            int xStart = dx > 0 ? 0 : w - 1;
            int yStart = dy > 0 ? 0 : h - 1;

            for (int yi = 0; yi < h; yi++)
            {
                int y = yStart + yi * dy;
                for (int xi = 0; xi < w; xi++)
                {
                    int x = xStart + xi * dx;
                    int cell = y * w + x;

                    float spd = sp[cell];
                    if (Hint.Unlikely(spd <= 0f)) continue;

                    float invSpeed = 1f / spd;
                    float tx = float.PositiveInfinity;
                    float ty = float.PositiveInfinity;

                    if (x > 0) { float v = t[cell - 1]; if (v < tx) tx = v; }
                    if (x < w - 1) { float v = t[cell + 1]; if (v < tx) tx = v; }
                    if (y > 0) { float v = t[cell - w]; if (v < ty) ty = v; }
                    if (y < h - 1) { float v = t[cell + w]; if (v < ty) ty = v; }

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

                    if (tNew < t[cell]) t[cell] = tNew;
                }
            }
        }

        [BurstCompile]
        public static void RelaxCell(FastSweepingState s, NativeArray<float> speed, int cell)
        {
            float* t = (float*)s.T.GetUnsafePtr();
            float* sp = (float*)speed.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            float spd = sp[cell];
            if (Hint.Unlikely(spd <= 0f)) return;

            float invSpeed = 1f / spd;
            int cx = cell % w;
            int cy = cell / w;

            float tx = float.PositiveInfinity;
            float ty = float.PositiveInfinity;

            if (cx > 0) { float v = t[cell - 1]; if (v < tx) tx = v; }
            if (cx < w - 1) { float v = t[cell + 1]; if (v < tx) tx = v; }
            if (cy > 0) { float v = t[cell - w]; if (v < ty) ty = v; }
            if (cy < h - 1) { float v = t[cell + w]; if (v < ty) ty = v; }

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

            if (tNew < t[cell]) t[cell] = tNew;
        }

        public static void Dispose(ref FastSweepingState s)
        {
            if (s.T.IsCreated) s.T.Dispose();
        }
    }
}
