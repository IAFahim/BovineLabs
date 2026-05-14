using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.FastMarching
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FastMarchingState
    {
        public Grid2D Grid;
        public float* T;
        public byte* State;
        public MinHeap Heap;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class FastMarchingApi
    {
        public static bool TryCreate(int width, int height, Allocator a, out FastMarchingState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            if (!MinHeap.TryCreate(g.Length, a, out var heap))
            {
                result = default;
                return false;
            }

            result = new FastMarchingState
            {
                Allocator = a,
                Grid = g,
                T = (float*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(float), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<float>(), g.Length),
                State = (byte*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), g.Length),
                Heap = heap
            };
            return true;
        }

        [BurstCompile]
        public static void InitializeSources(ref FastMarchingState s, in NativeArray<int> sources)
        {
            var t = s.T;
            var st = s.State;
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++)
            {
                t[i] = float.PositiveInfinity;
                st[i] = 0;
            }

            s.Heap.Clear();

            var src = (int*)sources.GetUnsafeReadOnlyPtr();
            for (var i = 0; i < sources.Length; i++)
            {
                t[src[i]] = 0f;
                st[src[i]] = 1;
                s.Heap.TryInsertOrDecrease(new HeapNode(src[i], 0f));
            }
        }

        [BurstCompile]
        public static bool TryPropagateStep(ref FastMarchingState s, in NativeArray<float> speed)
        {
            if (s.Heap.IsEmpty) return false;

            if (!s.Heap.TryPop(out var node)) return false;
            var u = node.Id;
            s.State[u] = 2;

            var t = s.T;
            var st = s.State;
            var spd = (float*)speed.GetUnsafePtr();
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var ux = u % w;
            var uy = u / w;

            if (ux + 1 < w) TryAccept(t, st, spd, w, h, u + 1, ref s);
            if (uy + 1 < h) TryAccept(t, st, spd, w, h, u + w, ref s);
            if (ux > 0) TryAccept(t, st, spd, w, h, u - 1, ref s);
            if (uy > 0) TryAccept(t, st, spd, w, h, u - w, ref s);

            return !s.Heap.IsEmpty;
        }

        private static void TryAccept(float* t, byte* st, float* spd, int w, int h, int ni, ref FastMarchingState s)
        {
            if (st[ni] == 2) return;
            var tNew = SolveEikonal(t, st, spd, w, h, ni);
            if (tNew < t[ni])
            {
                t[ni] = tNew;
                st[ni] = 1;
                s.Heap.TryInsertOrDecrease(new HeapNode(ni, tNew));
            }
        }

        [BurstCompile]
        public static bool TryPropagateAll(ref FastMarchingState s, in NativeArray<float> speed)
        {
            while (TryPropagateStep(ref s, in speed))
            {
            }

            return true;
        }

        private static float SolveEikonal(float* t, byte* st, float* spd, int w, int h, int idx)
        {
            var sp = spd[idx];
            if (Hint.Unlikely(sp <= 0f)) return float.PositiveInfinity;

            var px = idx % w;
            var py = idx / w;

            var tx = float.PositiveInfinity;
            var ty = float.PositiveInfinity;

            if (px > 0 && st[idx - 1] == 2)
            {
                var v = t[idx - 1];
                if (v < tx) tx = v;
            }

            if (px + 1 < w && st[idx + 1] == 2)
            {
                var v = t[idx + 1];
                if (v < tx) tx = v;
            }

            if (py > 0 && st[idx - w] == 2)
            {
                var v = t[idx - w];
                if (v < ty) ty = v;
            }

            if (py + 1 < h && st[idx + w] == 2)
            {
                var v = t[idx + w];
                if (v < ty) ty = v;
            }

            var invSpeed = 1f / sp;
            if (float.IsPositiveInfinity(tx)) return ty + invSpeed;
            if (float.IsPositiveInfinity(ty)) return tx + invSpeed;

            var diff = math.abs(tx - ty);
            return diff < invSpeed
                ? (tx + ty + math.sqrt(2f * invSpeed * invSpeed - diff * diff)) * 0.5f
                : math.min(tx, ty) + invSpeed;
        }

        [BurstCompile]
        public static void BuildGradientFlow(ref FastMarchingState s, ref NativeArray<float2> flow)
        {
            var t = s.T;
            var fl = (float2*)flow.GetUnsafePtr();
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var len = s.Grid.Length;

            for (var i = 0; i < len; i++)
            {
                var x = i % w;
                var y = i / w;
                var grad = float2.zero;

                if (x > 0 && x < w - 1) grad.x = (t[i + 1] - t[i - 1]) * 0.5f;
                else if (x > 0) grad.x = t[i] - t[i - 1];
                else if (x < w - 1) grad.x = t[i + 1] - t[i];

                if (y > 0 && y < h - 1) grad.y = (t[i + w] - t[i - w]) * 0.5f;
                else if (y > 0) grad.y = t[i] - t[i - w];
                else if (y < h - 1) grad.y = t[i + w] - t[i];

                var lenSq = math.lengthsq(grad);
                fl[i] = lenSq > 0f ? -grad * math.rsqrt(lenSq) : float2.zero;
            }
        }

        public static void Dispose(ref FastMarchingState s)
        {
            if (s.T != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.T); s.T = null; }
            if (s.State != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.State); s.State = null; }
            if (s.Heap.IsCreated) s.Heap.Dispose();
        }
    }
}