using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Wfc
{
    public struct WfcState
    {
        public Grid2D Grid;
        public int PatternCount;
        public NativeArray<ulong> PossibleBits;
        public NativeArray<int> Entropy;
        public NativeArray<ulong> Compatibility; // pattern * 4 + dir -> bitset of compatible patterns
        public UnsafeQueue<int> Queue;
        public NativeArray<byte> Dirty; // set when entropy changes during propagation
    }

    [BurstCompile]
    public unsafe static class WfcApi
    {
        public static WfcState Create(int width, int height, int patternCount, Allocator a)
        {
            if (patternCount > 64 || patternCount < 1)
                throw new System.ArgumentException($"patternCount must be 1..64, got {patternCount}", nameof(patternCount));
            var g = Grid2D.Create(width, height);
            return new WfcState
            {
                Grid = g,
                PatternCount = patternCount,
                PossibleBits = new NativeArray<ulong>(g.Length, a),
                Entropy = new NativeArray<int>(g.Length, a),
                Compatibility = new NativeArray<ulong>(patternCount * 4, a),
                Queue = new UnsafeQueue<int>(a),
                Dirty = new NativeArray<byte>(g.Length, a),
            };
        }

        [BurstCompile]
        public static void InitializeAllPossible(ref WfcState s)
        {
            ulong all = 0UL;
            if (s.PatternCount == 64) all = ulong.MaxValue;
            else all = (1UL << s.PatternCount) - 1;

            int len = s.Grid.Length;
            ulong* possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();
            int* entropyPtr = (int*)s.Entropy.GetUnsafePtr();

            for (int i = 0; i < len; i++)
            {
                possiblePtr[i] = all;
                entropyPtr[i] = s.PatternCount;
            }
            s.Queue.Clear();
        }

        [BurstCompile]
        public static void LearnAdjacency(ref WfcState s, in NativeArray<int> sample, int sampleWidth, int sampleHeight)
        {
            s.Compatibility.Fill(0UL);
            int* samplePtr = (int*)sample.GetUnsafeReadOnlyPtr();
            ulong* compatibilityPtr = (ulong*)s.Compatibility.GetUnsafePtr();

            for (int y = 0; y < sampleHeight; y++)
            {
                for (int x = 0; x < sampleWidth; x++)
                {
                    int pattern = samplePtr[y * sampleWidth + x];
                    if (Hint.Unlikely(pattern < 0 || pattern >= s.PatternCount)) continue;

                    for (int d = 0; d < 4; d++)
                    {
                        int2 offset = Grid2D.Dir4(d);
                        int nx = x + offset.x;
                        int ny = y + offset.y;
                        if (Hint.Unlikely(nx < 0 || ny < 0 || nx >= sampleWidth || ny >= sampleHeight)) continue;
                        
                        int neighbor = samplePtr[ny * sampleWidth + nx];
                        if (Hint.Unlikely(neighbor < 0 || neighbor >= s.PatternCount)) continue;
                        compatibilityPtr[pattern * 4 + d] |= 1UL << neighbor;
                    }
                }
            }
        }

        [BurstCompile]
        public static bool Observe(ref WfcState s, int cell, int chosenPattern)
        {
            ulong mask = 1UL << chosenPattern;
            s.PossibleBits[cell] = mask;
            s.Entropy[cell] = 1;
            s.Queue.Enqueue(cell);
            return true;
        }

        [BurstCompile]
        public static bool Propagate(ref WfcState s)
        {
            int width = s.Grid.Width;
            int height = s.Grid.Height;
            ulong* possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();
            int* entropyPtr = (int*)s.Entropy.GetUnsafePtr();
            ulong* compatibilityPtr = (ulong*)s.Compatibility.GetUnsafeReadOnlyPtr();
            byte* dirtyPtr = (byte*)s.Dirty.GetUnsafePtr();

            while (s.Queue.TryDequeue(out int cell))
            {
                int y = cell / width;
                int x = cell % width;
                ulong cellPossible = possiblePtr[cell];

                for (int d = 0; d < 4; d++)
                {
                    int2 offset = Grid2D.Dir4(d);
                    int nx = x + offset.x;
                    int ny = y + offset.y;
                    if (Hint.Unlikely(nx < 0 || ny < 0 || nx >= width || ny >= height)) continue;
                    
                    int ni = ny * width + nx;
                    ulong niPossible = possiblePtr[ni];

                    // Compute union of compatibilities
                    ulong unionPossible = 0UL;
                    ulong temp = cellPossible;
                    while (temp != 0)
                    {
                        int cp = math.tzcnt(temp);
                        unionPossible |= compatibilityPtr[cp * 4 + d];
                        temp &= ~(1UL << cp);
                    }

                    ulong restricted = niPossible & unionPossible;
                    if (Hint.Unlikely(restricted == 0UL)) return false;

                    if (restricted != niPossible)
                    {
                        possiblePtr[ni] = restricted;
                        entropyPtr[ni] = math.countbits(restricted);
                        dirtyPtr[ni] = 1;
                        s.Queue.Enqueue(ni);
                    }
                }
            }
            return true;
        }

        [BurstCompile]
        public static bool Run(ref WfcState s, ref NativeArray<int> output, ref Unity.Mathematics.Random rng)
        {
            InitializeAllPossible(ref s);
            int len = s.Grid.Length;
            int* entropyPtr = (int*)s.Entropy.GetUnsafePtr();
            ulong* possiblePtr = (ulong*)s.PossibleBits.GetUnsafePtr();
            int* outputPtr = (int*)output.GetUnsafePtr();

            // Build min-entropy heap
            var heap = MinHeap.Create(len, Allocator.Temp);
            for (int i = 0; i < len; i++)
                if (entropyPtr[i] > 1)
                    heap.InsertOrDecrease(new HeapNode(i, entropyPtr[i]));

            while (!heap.IsEmpty)
            {
                int bestCell = heap.Pop().Id;
                int e = entropyPtr[bestCell];
                if (e <= 1) continue;           // already collapsed
                if (possiblePtr[bestCell] == 0UL) continue; // contradiction

                ulong possible = possiblePtr[bestCell];
                int count = e;

                int chosen = rng.NextInt(0, count);
                int pattern = -1;
                ulong temp = possible;
                for (int i = 0; i <= chosen; i++)
                {
                    pattern = math.tzcnt(temp);
                    temp &= ~(1UL << pattern);
                }

                Observe(ref s, bestCell, pattern);
                if (!Propagate(ref s)) { heap.Dispose(); return false; }

                // Re-heap only cells whose entropy changed during propagation
                byte* dirtyPtr = (byte*)s.Dirty.GetUnsafePtr();
                for (int i = 0; i < len; i++)
                {
                    if (dirtyPtr[i] != 0)
                    {
                        dirtyPtr[i] = 0;
                        if (entropyPtr[i] > 1)
                            heap.InsertOrDecrease(new HeapNode(i, entropyPtr[i]));
                    }
                }
            }
            heap.Dispose();

            for (int i = 0; i < len; i++)
                outputPtr[i] = math.tzcnt(possiblePtr[i]);
            return true;
        }

        public static void Dispose(ref WfcState s)
        {
            if (s.PossibleBits.IsCreated) s.PossibleBits.Dispose();
            if (s.Entropy.IsCreated) s.Entropy.Dispose();
            if (s.Compatibility.IsCreated) s.Compatibility.Dispose();
            if (s.Queue.IsCreated) s.Queue.Dispose();
        }
    }
}
