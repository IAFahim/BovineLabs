using Unity.Collections;
using Unity.Mathematics;
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

    public static class WfcApi
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

        public static void InitializeAllPossible(ref WfcState s)
        {
            ulong all = 0UL;
            for (int i = 0; i < s.PatternCount && i < 64; i++)
                all |= 1UL << i;

            for (int i = 0; i < s.Grid.Length; i++)
            {
                s.PossibleBits[i] = all;
                s.Entropy[i] = s.PatternCount;
            }
            s.Queue.Clear();
        }

        public static void LearnAdjacency(ref WfcState s, NativeArray<int> sample, int sampleWidth, int sampleHeight)
        {
            s.Compatibility.Fill(0UL);
            for (int y = 0; y < sampleHeight; y++)
            {
                for (int x = 0; x < sampleWidth; x++)
                {
                    int pattern = sample[y * sampleWidth + x];
                    if (pattern < 0 || pattern >= s.PatternCount) continue;

                    for (int d = 0; d < 4; d++)
                    {
                        int2 np = new int2(x, y) + Grid2D.Directions4[d];
                        if (np.x < 0 || np.y < 0 || np.x >= sampleWidth || np.y >= sampleHeight) continue;
                        int neighbor = sample[np.y * sampleWidth + np.x];
                        if (neighbor < 0 || neighbor >= s.PatternCount) continue;
                        s.Compatibility[pattern * 4 + d] |= 1UL << neighbor;
                    }
                }
            }
        }

        public static bool Observe(ref WfcState s, int cell, int chosenPattern)
        {
            ulong mask = 1UL << chosenPattern;
            s.PossibleBits[cell] = mask;
            s.Entropy[cell] = 1;
            s.Queue.Enqueue(cell);
            return true;
        }

        public static bool Propagate(ref WfcState s)
        {
            while (s.Queue.TryDequeue(out int cell))
            {
                int2 p = s.Grid.ToCoord(cell);
                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);

                    // Compute possible patterns in neighbor based on cell's possibilities
                    ulong neighborPossible = 0UL;
                    ulong cellPossible = s.PossibleBits[cell];
                    for (int cp = 0; cp < s.PatternCount && cp < 64; cp++)
                    {
                        if ((cellPossible & (1UL << cp)) == 0) continue;
                        // cp is at cell in direction d from neighbor.
                        // For neighbor to be compatible, it must be compatible with cp in the opposite direction.
                        int oppDir = (d + 2) % 4;
                        neighborPossible |= s.Compatibility[cp * 4 + oppDir];
                    }

                    ulong restricted = s.PossibleBits[ni] & neighborPossible;
                    if (restricted == 0UL) return false; // contradiction

                    if (restricted != s.PossibleBits[ni])
                    {
                        s.PossibleBits[ni] = restricted;
                        int count = 0;
                        for (int i = 0; i < s.PatternCount && i < 64; i++)
                            if ((restricted & (1UL << i)) != 0) count++;
                        s.Entropy[ni] = count;
                        s.Queue.Enqueue(ni);
                    }
                }
            }
            return true;
        }

        public static bool Run(ref WfcState s, NativeArray<int> output, ref Unity.Mathematics.Random rng)
        {
            InitializeAllPossible(ref s);

            while (true)
            {
                // Find min entropy > 1
                int bestCell = -1;
                int bestEntropy = int.MaxValue;
                for (int i = 0; i < s.Grid.Length; i++)
                {
                    if (s.Entropy[i] <= 1) continue;
                    if (s.Entropy[i] < bestEntropy) { bestEntropy = s.Entropy[i]; bestCell = i; }
                }

                if (bestCell < 0) break;

                ulong possible = s.PossibleBits[bestCell];
                int count = 0;
                for (int i = 0; i < s.PatternCount && i < 64; i++)
                    if ((possible & (1UL << i)) != 0) count++;

                int chosen = (int)(rng.NextInt(0, count));
                int pattern = 0;
                int seen = 0;
                for (int i = 0; i < s.PatternCount && i < 64; i++)
                {
                    if ((possible & (1UL << i)) != 0)
                    {
                        if (seen == chosen) { pattern = i; break; }
                        seen++;
                    }
                }

                Observe(ref s, bestCell, pattern);
                if (!Propagate(ref s)) return false;
            }

            for (int i = 0; i < s.Grid.Length; i++)
            {
                ulong bits = s.PossibleBits[i];
                for (int p = 0; p < s.PatternCount && p < 64; p++)
                {
                    if ((bits & (1UL << p)) != 0) { output[i] = p; break; }
                }
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
