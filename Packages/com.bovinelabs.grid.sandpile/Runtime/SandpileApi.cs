using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Sandpile
{
    public struct SandpileState
    {
        public Grid2D Grid;
        public NativeArray<int> Grains;
        public NativeQueue<int> Queue;
        public NativeArray<byte> InQueue;
    }

    public static class SandpileApi
    {
        public static SandpileState Create(int width, int height, Allocator allocator)
        {
            var g = Grid2D.Create(width, height);
            return new SandpileState
            {
                Grid = g,
                Grains = new NativeArray<int>(g.Length, allocator),
                Queue = new NativeQueue<int>(allocator),
                InQueue = new NativeArray<byte>(g.Length, allocator),
            };
        }

        public static void Clear(ref SandpileState s)
        {
            s.Grains.Fill(0);
            s.InQueue.Fill((byte)0);
            s.Queue.Clear();
        }

        public static void AddGrains(ref SandpileState s, int cell, int amount)
        {
            s.Grains[cell] += amount;
            if (s.Grains[cell] >= 4 && s.InQueue[cell] == 0)
            {
                s.Queue.Enqueue(cell);
                s.InQueue[cell] = 1;
            }
        }

        public static bool RelaxStep(ref SandpileState s)
        {
            if (!s.Queue.TryDequeue(out int cell)) return false;
            s.InQueue[cell] = 0;

            int toppleCount = s.Grains[cell] / 4;
            s.Grains[cell] %= 4;

            int2 cp = s.Grid.ToCoord(cell);
            for (int d = 0; d < 4; d++)
            {
                int2 np = cp + Grid2D.Directions4[d];
                if (!s.Grid.InBounds(np)) continue;
                int ni = s.Grid.ToIndex(np);
                s.Grains[ni] += toppleCount;
                if (s.Grains[ni] >= 4 && s.InQueue[ni] == 0)
                {
                    s.Queue.Enqueue(ni);
                    s.InQueue[ni] = 1;
                }
            }

            return s.Queue.Count > 0;
        }

        public static void RelaxAll(ref SandpileState s)
        {
            while (RelaxStep(ref s)) { }
        }

        public static bool IsStable(ref SandpileState s)
        {
            for (int i = 0; i < s.Grid.Length; i++)
                if (s.Grains[i] >= 4) return false;
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
