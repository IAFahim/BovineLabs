using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.FastMarching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FastMarchingState
    {
        public Grid2D Grid;
        public NativeArray<float> T;
        public NativeArray<byte> State;
        public MinHeap Heap;
    }

    [BurstCompile]
    public unsafe static class FastMarchingApi
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

        [BurstCompile]
        public static void InitializeSources(ref FastMarchingState s, NativeArray<int> sources)
        {
            float* t = (float*)s.T.GetUnsafePtr();
            byte* st = (byte*)s.State.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++) { t[i] = float.PositiveInfinity; st[i] = 0; }
            s.Heap.Clear();

            int* src = (int*)sources.GetUnsafeReadOnlyPtr();
            for (int i = 0; i < sources.Length; i++)
            {
                t[src[i]] = 0f;
                st[src[i]] = 1;
                s.Heap.InsertOrDecrease(new HeapNode(src[i], 0f));
            }
        }

        [BurstCompile]
        public static bool PropagateStep(ref FastMarchingState s, NativeArray<float> speed)
        {
            if (s.Heap.IsEmpty) return false;

            int u = s.Heap.Pop().Id;
            s.State[u] = 2;

            float* t = (float*)s.T.GetUnsafePtr();
            byte* st = (byte*)s.State.GetUnsafePtr();
            float* spd = (float*)speed.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int ux = u % w;
            int uy = u / w;

            // Right
            if (ux + 1 < w) TryAccept(t, st, spd, w, h, u + 1, s);
            // Up
            if (uy + 1 < h) TryAccept(t, st, spd, w, h, u + w, s);
            // Left
            if (ux > 0) TryAccept(t, st, spd, w, h, u - 1, s);
            // Down
            if (uy > 0) TryAccept(t, st, spd, w, h, u - w, s);

            return !s.Heap.IsEmpty;
        }

        private static void TryAccept(float* t, byte* st, float* spd, int w, int h, int ni, FastMarchingState s)
        {
            if (st[ni] == 2) return;
            float tNew = SolveEikonal(t, st, spd, w, h, ni);
            if (tNew < t[ni])
            {
                t[ni] = tNew;
                st[ni] = 1;
                s.Heap.InsertOrDecrease(new HeapNode(ni, tNew));
            }
        }

        [BurstCompile]
        public static void PropagateAll(ref FastMarchingState s, NativeArray<float> speed)
        {
            while (PropagateStep(ref s, speed)) { }
        }

        private static float SolveEikonal(float* t, byte* st, float* spd, int w, int h, int idx)
        {
            float sp = spd[idx];
            if (Hint.Unlikely(sp <= 0f)) return float.PositiveInfinity;

            int px = idx % w;
            int py = idx / w;

            float tx = float.PositiveInfinity;
            float ty = float.PositiveInfinity;

            if (px > 0 && st[idx - 1] == 2) { float v = t[idx - 1]; if (v < tx) tx = v; }
            if (px + 1 < w && st[idx + 1] == 2) { float v = t[idx + 1]; if (v < tx) tx = v; }
            if (py > 0 && st[idx - w] == 2) { float v = t[idx - w]; if (v < ty) ty = v; }
            if (py + 1 < h && st[idx + w] == 2) { float v = t[idx + w]; if (v < ty) ty = v; }

            float invSpeed = 1f / sp;
            if (float.IsPositiveInfinity(tx)) return ty + invSpeed;
            if (float.IsPositiveInfinity(ty)) return tx + invSpeed;

            float diff = math.abs(tx - ty);
            return diff < invSpeed
                ? (tx + ty + math.sqrt(2f * invSpeed * invSpeed - diff * diff)) * 0.5f
                : math.min(tx, ty) + invSpeed;
        }

        [BurstCompile]
        public static void BuildGradientFlow(ref FastMarchingState s, NativeArray<float2> flow)
        {
            float* t = (float*)s.T.GetUnsafePtr();
            float2* fl = (float2*)flow.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;

            for (int i = 0; i < len; i++)
            {
                int x = i % w;
                int y = i / w;
                float2 grad = float2.zero;

                if (x > 0 && x < w - 1) grad.x = (t[i + 1] - t[i - 1]) * 0.5f;
                else if (x > 0) grad.x = t[i] - t[i - 1];
                else if (x < w - 1) grad.x = t[i + 1] - t[i];

                if (y > 0 && y < h - 1) grad.y = (t[i + w] - t[i - w]) * 0.5f;
                else if (y > 0) grad.y = t[i] - t[i - w];
                else if (y < h - 1) grad.y = t[i + w] - t[i];

                float lenSq = math.lengthsq(grad);
                fl[i] = lenSq > 0f ? -grad * math.rsqrt(lenSq) : float2.zero;
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
