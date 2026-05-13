using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Watershed
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WatershedState
    {
        public Grid2D Grid;
        public NativeArray<int> Label;
        public NativeArray<byte> State;
        public MinHeap Heap;
    }

    [BurstCompile]
    public unsafe static class WatershedApi
    {
        public static WatershedState Create(int width, int height, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new WatershedState
            {
                Grid = g,
                Label = new NativeArray<int>(g.Length, a),
                State = new NativeArray<byte>(g.Length, a),
                Heap = MinHeap.Create(g.Length, a),
            };
        }

        [BurstCompile]
        public static int FindMinima(ref WatershedState s, in NativeArray<float> height)
        {
            int* label = (int*)s.Label.GetUnsafePtr();
            byte* st = (byte*)s.State.GetUnsafePtr();
            float* ht = (float*)height.GetUnsafeReadOnlyPtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;

            for (int i = 0; i < len; i++) { label[i] = -1; st[i] = 0; }
            s.Heap.Clear();

            int labelCount = 0;
            var stack = new UnsafeList<int>(len, Allocator.Temp);

            for (int i = 0; i < len; i++)
            {
                if (st[i] != 0) continue;
                if (ht[i] <= 0f) continue;

                int x = i % w;
                int y = i / w;
                bool isMin = true;
                if (x > 0 && ht[i - 1] < ht[i]) isMin = false;
                if (isMin && x + 1 < w && ht[i + 1] < ht[i]) isMin = false;
                if (isMin && y > 0 && ht[i - w] < ht[i]) isMin = false;
                if (isMin && y + 1 < h && ht[i + w] < ht[i]) isMin = false;

                if (!isMin) continue;

                stack.Clear();
                stack.Add(i);
                st[i] = 2;
                float hv = ht[i];

                for (int si = 0; si < stack.Length; si++)
                {
                    int ci = stack.Ptr[si];
                    int cx = ci % w;
                    int cy = ci / w;
                    if (cx > 0 && st[ci - 1] == 0 && math.abs(ht[ci - 1] - hv) < 0.0001f) { st[ci - 1] = 2; stack.Add(ci - 1); }
                    if (cx + 1 < w && st[ci + 1] == 0 && math.abs(ht[ci + 1] - hv) < 0.0001f) { st[ci + 1] = 2; stack.Add(ci + 1); }
                    if (cy > 0 && st[ci - w] == 0 && math.abs(ht[ci - w] - hv) < 0.0001f) { st[ci - w] = 2; stack.Add(ci - w); }
                    if (cy + 1 < h && st[ci + w] == 0 && math.abs(ht[ci + w] - hv) < 0.0001f) { st[ci + w] = 2; stack.Add(ci + w); }
                }

                int lbl = labelCount++;
                for (int si = 0; si < stack.Length; si++)
                    label[stack.Ptr[si]] = lbl;

                for (int si = 0; si < stack.Length; si++)
                {
                    int ci = stack.Ptr[si];
                    int cx = ci % w;
                    int cy = ci / w;
                    if (cx > 0 && st[ci - 1] == 0 && label[ci - 1] == -1) { st[ci - 1] = 1; label[ci - 1] = lbl; s.Heap.InsertOrDecrease(new HeapNode(ci - 1, ht[ci - 1])); }
                    if (cx + 1 < w && st[ci + 1] == 0 && label[ci + 1] == -1) { st[ci + 1] = 1; label[ci + 1] = lbl; s.Heap.InsertOrDecrease(new HeapNode(ci + 1, ht[ci + 1])); }
                    if (cy > 0 && st[ci - w] == 0 && label[ci - w] == -1) { st[ci - w] = 1; label[ci - w] = lbl; s.Heap.InsertOrDecrease(new HeapNode(ci - w, ht[ci - w])); }
                    if (cy + 1 < h && st[ci + w] == 0 && label[ci + w] == -1) { st[ci + w] = 1; label[ci + w] = lbl; s.Heap.InsertOrDecrease(new HeapNode(ci + w, ht[ci + w])); }
                }
            }

            stack.Dispose();
            return labelCount;
        }

        [BurstCompile]
        public static void Flood(ref WatershedState s, in NativeArray<float> height)
        {
            int* label = (int*)s.Label.GetUnsafePtr();
            byte* st = (byte*)s.State.GetUnsafePtr();
            float* ht = (float*)height.GetUnsafeReadOnlyPtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;

            while (!s.Heap.IsEmpty)
            {
                int u = s.Heap.Pop().Id;
                st[u] = 2;
                int ux = u % w;
                int uy = u / w;
                int lblU = label[u];

                if (ux > 0) FloodNeighbor(label, st, ht, w, h, u - 1, lblU, s);
                if (ux + 1 < w) FloodNeighbor(label, st, ht, w, h, u + 1, lblU, s);
                if (uy > 0) FloodNeighbor(label, st, ht, w, h, u - w, lblU, s);
                if (uy + 1 < h) FloodNeighbor(label, st, ht, w, h, u + w, lblU, s);
            }
        }

        private static void FloodNeighbor(int* label, byte* st, float* ht, int w, int h, int ni, int lblU, WatershedState s)
        {
            if (st[ni] == 2) return;
            if (label[ni] == -1) label[ni] = lblU;
            else if (label[ni] != lblU) label[ni] = -2;
            if (st[ni] == 0)
            {
                st[ni] = 1;
                s.Heap.InsertOrDecrease(new HeapNode(ni, ht[ni]));
            }
        }

        [BurstCompile]
        public static void ExtractBoundaries(ref WatershedState s, ref NativeArray<byte> boundary)
        {
            int* label = (int*)s.Label.GetUnsafePtr();
            byte* bnd = (byte*)boundary.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;

            for (int i = 0; i < len; i++) bnd[i] = 0;

            for (int i = 0; i < len; i++)
            {
                if (label[i] == -2) { bnd[i] = 1; continue; }
                if (label[i] < 0) continue;

                int x = i % w;
                int y = i / w;
                int li = label[i];
                bool isBorder = false;
                if (x > 0 && label[i - 1] >= 0 && label[i - 1] != li) isBorder = true;
                if (!isBorder && x + 1 < w && label[i + 1] >= 0 && label[i + 1] != li) isBorder = true;
                if (!isBorder && y > 0 && label[i - w] >= 0 && label[i - w] != li) isBorder = true;
                if (!isBorder && y + 1 < h && label[i + w] >= 0 && label[i + w] != li) isBorder = true;
                if (isBorder) bnd[i] = 1;
            }
        }

        public static void Dispose(ref WatershedState s)
        {
            if (s.Label.IsCreated) s.Label.Dispose();
            if (s.State.IsCreated) s.State.Dispose();
            if (s.Heap.IsCreated) s.Heap.Dispose();
        }
    }
}
