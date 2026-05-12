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
        public NativeQueue<int> Queue;
    }

    [BurstCompile]
    public unsafe static class WfcApi
    {
        public static WfcState Create(int width, int height, int patternCount, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new WfcState
            {
                Grid = g,
                PatternCount = patternCount,
                PossibleBits = new NativeArray<ulong>(g.Length, a),
                Entropy = new NativeArray<int>(g.Length, a),
                Compatibility = new NativeArray<ulong>(patternCount * 4, a),
                Queue = new NativeQueue<int>(a),
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
                        int2 offset = Grid2D.Directions4[d];
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

            while (s.Queue.TryDequeue(out int cell))
            {
                int y = cell / width;
                int x = cell % width;
                ulong cellPossible = possiblePtr[cell];

                for (int d = 0; d < 4; d++)
                {
                    int2 offset = Grid2D.Directions4[d];
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

            while (true)
            {
                int bestCell = -1;
                int bestEntropy = int.MaxValue;
                for (int i = 0; i < len; i++)
                {
                    int e = entropyPtr[i];
                    if (e <= 1) continue;
                    if (e < bestEntropy) { bestEntropy = e; bestCell = i; }
                }

                if (bestCell < 0) break;

                ulong possible = possiblePtr[bestCell];
                int count = entropyPtr[bestCell];

                int chosen = rng.NextInt(0, count);
                int pattern = -1;
                ulong temp = possible;
                for (int i = 0; i <= chosen; i++)
                {
                    pattern = math.tzcnt(temp);
                    temp &= ~(1UL << pattern);
                }

                Observe(ref s, bestCell, pattern);
                if (!Propagate(ref s)) return false;
            }

            for (int i = 0; i < len; i++)
            {
                outputPtr[i] = math.tzcnt(possiblePtr[i]);
            }
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
