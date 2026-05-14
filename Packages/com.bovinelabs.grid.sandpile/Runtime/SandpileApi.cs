using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BovineLabs.Grid.Sandpile
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SandpileState
    {
        public Grid2D Grid;
        public int* Grains;
        public UnsafeQueue<int> Queue;
        public byte* InQueue;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class SandpileApi
    {
        public static bool TryCreate(int width, int height, Allocator allocator, out SandpileState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new SandpileState
            {
                Allocator = allocator,
                Grid = g,
                Grains = (int*)Unity.Collections.AllocatorManager.Allocate(allocator, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length),
                Queue = new UnsafeQueue<int>(allocator),
                InQueue = (byte*)Unity.Collections.AllocatorManager.Allocate(allocator, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), g.Length)
            };
            return true;
        }

        [BurstCompile]
        public static void Clear(ref SandpileState s)
        {
            var grains = s.Grains;
            var inQueue = s.InQueue;
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++)
            {
                grains[i] = 0;
                inQueue[i] = 0;
            }

            s.Queue.Clear();
        }

        [BurstCompile]
        public static void AddGrains(ref SandpileState s, int cell, int amount)
        {
            var grains = s.Grains;
            var inQueue = s.InQueue;
            grains[cell] += amount;
            if (Hint.Likely(grains[cell] >= 4) && inQueue[cell] == 0)
            {
                s.Queue.Enqueue(cell);
                inQueue[cell] = 1;
            }
        }

        [BurstCompile]
        public static bool TryRelaxStep(ref SandpileState s)
        {
            if (!s.Queue.TryDequeue(out var cell)) return false;

            var grains = s.Grains;
            var inQueue = s.InQueue;
            inQueue[cell] = 0;

            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var cp = s.Grid.ToCoord(cell);
            var toppleCount = grains[cell] / 4;
            grains[cell] %= 4;

            if (Hint.Likely(cp.x + 1 < w))
            {
                var ni = cell + 1;
                grains[ni] += toppleCount;
                if (grains[ni] >= 4 && inQueue[ni] == 0)
                {
                    s.Queue.Enqueue(ni);
                    inQueue[ni] = 1;
                }
            }

            if (Hint.Likely(cp.y + 1 < h))
            {
                var ni = cell + w;
                grains[ni] += toppleCount;
                if (grains[ni] >= 4 && inQueue[ni] == 0)
                {
                    s.Queue.Enqueue(ni);
                    inQueue[ni] = 1;
                }
            }

            if (Hint.Likely(cp.x > 0))
            {
                var ni = cell - 1;
                grains[ni] += toppleCount;
                if (grains[ni] >= 4 && inQueue[ni] == 0)
                {
                    s.Queue.Enqueue(ni);
                    inQueue[ni] = 1;
                }
            }

            if (Hint.Likely(cp.y > 0))
            {
                var ni = cell - w;
                grains[ni] += toppleCount;
                if (grains[ni] >= 4 && inQueue[ni] == 0)
                {
                    s.Queue.Enqueue(ni);
                    inQueue[ni] = 1;
                }
            }

            return s.Queue.Count > 0;
        }

        [BurstCompile]
        public static void RelaxAll(ref SandpileState s)
        {
            TryRelaxAll(ref s);
        }

        [BurstCompile]
        public static bool TryRelaxAll(ref SandpileState s)
        {
            while (TryRelaxStep(ref s))
            {
            }

            return true;
        }

        [BurstCompile]
        public static bool IsStable(ref SandpileState s)
        {
            var grains = s.Grains;
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++)
                if (grains[i] >= 4)
                    return false;
            return true;
        }

        public static void Dispose(ref SandpileState s)
        {
            if (s.Grains != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Grains); s.Grains = null; }
            if (s.Queue.IsCreated) s.Queue.Dispose();
            if (s.InQueue != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.InQueue); s.InQueue = null; }
        }
    }
}