using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Sandpile
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SandpileState
    {
        public Grid2D Grid;
        public NativeArray<int> Grains;
        public UnsafeQueue<int> Queue;
        public NativeArray<byte> InQueue;
    }

    [BurstCompile]
    public unsafe static class SandpileApi
    {
        public static SandpileState Create(int width, int height, Allocator allocator)
        {
            var g = Grid2D.Create(width, height);
            return new SandpileState
            {
                Grid = g,
                Grains = new NativeArray<int>(g.Length, allocator),
                Queue = new UnsafeQueue<int>(allocator),
                InQueue = new NativeArray<byte>(g.Length, allocator),
            };
        }

        [BurstCompile]
        public static void Clear(ref SandpileState s)
        {
            int* grains = (int*)s.Grains.GetUnsafePtr();
            byte* inQueue = (byte*)s.InQueue.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++)
            {
                grains[i] = 0;
                inQueue[i] = 0;
            }
            s.Queue.Clear();
        }

        [BurstCompile]
        public static void AddGrains(ref SandpileState s, int cell, int amount)
        {
            int* grains = (int*)s.Grains.GetUnsafePtr();
            byte* inQueue = (byte*)s.InQueue.GetUnsafePtr();
            grains[cell] += amount;
            if (Hint.Likely(grains[cell] >= 4) && inQueue[cell] == 0)
            {
                s.Queue.Enqueue(cell);
                inQueue[cell] = 1;
            }
        }

        [BurstCompile]
        public static bool RelaxStep(ref SandpileState s)
        {
            if (!s.Queue.TryDequeue(out int cell)) return false;

            int* grains = (int*)s.Grains.GetUnsafePtr();
            byte* inQueue = (byte*)s.InQueue.GetUnsafePtr();
            inQueue[cell] = 0;

            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int2 cp = s.Grid.ToCoord(cell);
            int toppleCount = grains[cell] / 4;
            grains[cell] %= 4;

            // Unrolled 4-direction neighbors with inlined bounds
            // Right
            if (Hint.Likely(cp.x + 1 < w))
            {
                int ni = cell + 1;
                grains[ni] += toppleCount;
                if (grains[ni] >= 4 && inQueue[ni] == 0) { s.Queue.Enqueue(ni); inQueue[ni] = 1; }
            }
            // Up
            if (Hint.Likely(cp.y + 1 < h))
            {
                int ni = cell + w;
                grains[ni] += toppleCount;
                if (grains[ni] >= 4 && inQueue[ni] == 0) { s.Queue.Enqueue(ni); inQueue[ni] = 1; }
            }
            // Left
            if (Hint.Likely(cp.x > 0))
            {
                int ni = cell - 1;
                grains[ni] += toppleCount;
                if (grains[ni] >= 4 && inQueue[ni] == 0) { s.Queue.Enqueue(ni); inQueue[ni] = 1; }
            }
            // Down
            if (Hint.Likely(cp.y > 0))
            {
                int ni = cell - w;
                grains[ni] += toppleCount;
                if (grains[ni] >= 4 && inQueue[ni] == 0) { s.Queue.Enqueue(ni); inQueue[ni] = 1; }
            }

            return s.Queue.Count > 0;
        }

        [BurstCompile]
        public static void RelaxAll(ref SandpileState s)
        {
            while (RelaxStep(ref s)) { }
        }

        [BurstCompile]
        public static bool IsStable(ref SandpileState s)
        {
            int* grains = (int*)s.Grains.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++)
                if (grains[i] >= 4) return false;
            return true;
        }

        public static void Dispose(ref SandpileState s)
        {
            if (s.Grains.IsCreated) s.Grains.Dispose();
            if (s.Queue.IsCreated) s.Queue.Dispose();
            if (s.InQueue.IsCreated) s.InQueue.Dispose();
        }
    }
}
