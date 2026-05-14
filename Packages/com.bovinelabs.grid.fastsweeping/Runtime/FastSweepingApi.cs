using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.FastSweeping
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FastSweepingState
    {
        public Grid2D Grid;
        public float* T;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class FastSweepingApi
    {
        public static bool TryCreate(int width, int height, Allocator a, out FastSweepingState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new FastSweepingState
            {
                Allocator = a,
                Grid = g,
                T = (float*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(float), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<float>(), g.Length)
            };
            return true;
        }

        [BurstCompile]
        public static bool TryInitialize(ref FastSweepingState s, in NativeArray<int> sources)
        {
            var t = s.T;
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++) t[i] = float.PositiveInfinity;
            var src = (int*)sources.GetUnsafeReadOnlyPtr();
            for (var i = 0; i < sources.Length; i++) t[src[i]] = 0f;
            return true;
        }

        [BurstCompile]
        public static bool TrySweepAllDirections(ref FastSweepingState s, in NativeArray<float> speed, int rounds)
        {
            var t = s.T;
            var sp = (float*)speed.GetUnsafePtr();
            var w = s.Grid.Width;
            var h = s.Grid.Height;

            for (var r = 0; r < rounds; r++)
            {
                Sweep(t, sp, w, h, 1, 1);
                Sweep(t, sp, w, h, -1, 1);
                Sweep(t, sp, w, h, 1, -1);
                Sweep(t, sp, w, h, -1, -1);
            }

            return true;
        }

        private static void Sweep(float* t, float* sp, int w, int h, int dx, int dy)
        {
            var xStart = dx > 0 ? 0 : w - 1;
            var yStart = dy > 0 ? 0 : h - 1;

            for (var yi = 0; yi < h; yi++)
            {
                var y = yStart + yi * dy;
                for (var xi = 0; xi < w; xi++)
                {
                    var x = xStart + xi * dx;
                    var cell = y * w + x;

                    var spd = sp[cell];
                    if (Hint.Unlikely(spd <= 0f)) continue;

                    var invSpeed = 1f / spd;
                    var tx = float.PositiveInfinity;
                    var ty = float.PositiveInfinity;

                    if (x > 0)
                    {
                        var v = t[cell - 1];
                        if (v < tx) tx = v;
                    }

                    if (x < w - 1)
                    {
                        var v = t[cell + 1];
                        if (v < tx) tx = v;
                    }

                    if (y > 0)
                    {
                        var v = t[cell - w];
                        if (v < ty) ty = v;
                    }

                    if (y < h - 1)
                    {
                        var v = t[cell + w];
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

                    if (tNew < t[cell]) t[cell] = tNew;
                }
            }
        }

        [BurstCompile]
        public static bool TryRelaxCell(in FastSweepingState s, in NativeArray<float> speed, int cell)
        {
            var t = s.T;
            var sp = (float*)speed.GetUnsafePtr();
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var spd = sp[cell];
            if (Hint.Unlikely(spd <= 0f)) return false;

            var invSpeed = 1f / spd;
            var cx = cell % w;
            var cy = cell / w;

            var tx = float.PositiveInfinity;
            var ty = float.PositiveInfinity;

            if (cx > 0)
            {
                var v = t[cell - 1];
                if (v < tx) tx = v;
            }

            if (cx < w - 1)
            {
                var v = t[cell + 1];
                if (v < tx) tx = v;
            }

            if (cy > 0)
            {
                var v = t[cell - w];
                if (v < ty) ty = v;
            }

            if (cy < h - 1)
            {
                var v = t[cell + w];
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

            if (tNew < t[cell]) t[cell] = tNew;
            return true;
        }

        public static void Dispose(ref FastSweepingState s)
        {
            if (s.T != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.T); s.T = null; }
        }
    }
}