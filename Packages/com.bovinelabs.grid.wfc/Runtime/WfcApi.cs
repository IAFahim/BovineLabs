using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wfc
{
    public unsafe struct WfcState
    {
        public Grid2D Grid;
        public int PatternCount;
        public ulong* PossibleBits;
        public int* Entropy;
        public ulong* Compatibility;
        public UnsafeQueue<int> Queue;
        public byte* Dirty;

        public MinHeap ObserveHeap;
        public byte WfcComplete;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class WfcApi
    {
        public static bool TryCreate(int width, int height, int patternCount, Allocator a, out WfcState s)
        {
            s = default;
            if (patternCount > 64 || patternCount < 1) return false;
            if (!Grid2D.TryCreate(width, height, out var g)) return false;
            if (!MinHeap.TryCreate(g.Length, a, out var heap)) return false;
            s = new WfcState
            {
                Grid = g,
                PatternCount = patternCount,
                PossibleBits = (ulong*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(ulong), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<ulong>(), g.Length),
                Entropy = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length),
                Compatibility = (ulong*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(ulong), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<ulong>(), patternCount * 4),
                Queue = new UnsafeQueue<int>(a),
                Dirty = (byte*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), g.Length),
                ObserveHeap = heap,
                WfcComplete = 0
            };
            return true;
        }

        [BurstCompile]
        public static bool TryInitializeAllPossible(ref WfcState s)
        {
            var all = 0UL;
            if (s.PatternCount == 64) all = ulong.MaxValue;
            else all = (1UL << s.PatternCount) - 1;

            var len = s.Grid.Length;
            var possiblePtr = s.PossibleBits;
            var entropyPtr = s.Entropy;

            for (var i = 0; i < len; i++)
            {
                possiblePtr[i] = all;
                entropyPtr[i] = s.PatternCount;
            }

            s.Queue.Clear();
            UnsafeUtility.MemSet(s.Dirty, 0, s.Grid.Length * sizeof(byte));
            return true;
        }

        [BurstCompile]
        public static bool TryLearnAdjacency(ref WfcState s, in NativeArray<int> sample, int sampleWidth,
            int sampleHeight)
        {
            UnsafeUtility.MemSet(s.Compatibility, 0, s.PatternCount * 4 * sizeof(ulong));
            var samplePtr = (int*)sample.GetUnsafeReadOnlyPtr();
            var compatibilityPtr = s.Compatibility;

            for (var y = 0; y < sampleHeight; y++)
            for (var x = 0; x < sampleWidth; x++)
            {
                var pattern = samplePtr[y * sampleWidth + x];
                if (Hint.Unlikely(pattern < 0 || pattern >= s.PatternCount)) continue;

                for (var d = 0; d < 4; d++)
                {
                    var offset = Grid2D.Dir4(d);
                    var nx = x + offset.x;
                    var ny = y + offset.y;
                    if (Hint.Unlikely(nx < 0 || ny < 0 || nx >= sampleWidth || ny >= sampleHeight)) continue;

                    var neighbor = samplePtr[ny * sampleWidth + nx];
                    if (Hint.Unlikely(neighbor < 0 || neighbor >= s.PatternCount)) continue;
                    compatibilityPtr[pattern * 4 + d] |= 1UL << neighbor;
                }
            }

            return true;
        }

        [BurstCompile]
        public static bool TryObserve(ref WfcState s, int cell, int chosenPattern)
        {
            if (Hint.Unlikely(!s.Grid.InBounds(cell) || (uint)chosenPattern >= (uint)s.PatternCount)) return false;
            var mask = 1UL << chosenPattern;
            s.PossibleBits[cell] = mask;
            s.Entropy[cell] = 1;
            s.Queue.Enqueue(cell);
            return true;
        }

        [BurstCompile]
        public static bool TryPropagate(ref WfcState s)
        {
            var width = s.Grid.Width;
            var height = s.Grid.Height;
            var possiblePtr = s.PossibleBits;
            var entropyPtr = s.Entropy;
            var compatibilityPtr = s.Compatibility;
            var dirtyPtr = s.Dirty;

            while (s.Queue.TryDequeue(out var cell))
            {
                var y = cell / width;
                var x = cell % width;
                var cellPossible = possiblePtr[cell];

                for (var d = 0; d < 4; d++)
                {
                    var offset = Grid2D.Dir4(d);
                    var nx = x + offset.x;
                    var ny = y + offset.y;
                    if (Hint.Unlikely(nx < 0 || ny < 0 || nx >= width || ny >= height)) continue;

                    var ni = ny * width + nx;
                    var niPossible = possiblePtr[ni];

                    var unionPossible = 0UL;
                    var temp = cellPossible;
                    while (temp != 0)
                    {
                        var cp = math.tzcnt(temp);
                        unionPossible |= compatibilityPtr[cp * 4 + d];
                        temp &= ~(1UL << cp);
                    }

                    var restricted = niPossible & unionPossible;
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
        public static bool TryRun(ref WfcState s, ref NativeArray<int> output, ref Random rng)
        {
            if (!TryInitWfc(ref s)) return false;

            while (s.WfcComplete == 0)
                TryObserveStep(ref s, ref rng);

            if (s.WfcComplete == 2) return false;

            var len = s.Grid.Length;
            var outputPtr = (int*)output.GetUnsafePtr();
            var possiblePtr = s.PossibleBits;
            for (var i = 0; i < len; i++)
            {
                var bits = possiblePtr[i];
                outputPtr[i] = bits == 0 ? -1 : math.tzcnt(bits);
            }

            return true;
        }

        [BurstCompile]
        public static bool TryInitWfc(ref WfcState s)
        {
            if (!TryInitializeAllPossible(ref s)) return false;

            s.ObserveHeap.Clear();
            s.WfcComplete = 0;

            var len = s.Grid.Length;
            var entropyPtr = s.Entropy;

            for (var i = 0; i < len; i++)
                if (entropyPtr[i] > 1)
                    if (!s.ObserveHeap.TryInsertOrDecrease(new HeapNode(i, entropyPtr[i])))
                        return false;

            return true;
        }

        [BurstCompile]
        public static bool TryObserveStep(ref WfcState s, ref Random rng)
        {
            if (s.WfcComplete != 0) return false;

            var len = s.Grid.Length;
            var entropyPtr = s.Entropy;
            var possiblePtr = s.PossibleBits;

            while (!s.ObserveHeap.IsEmpty)
            {
                if (!s.ObserveHeap.TryPop(out var top))
                {
                    s.WfcComplete = 1;
                    return false;
                }

                var bestCell = top.Id;
                var e = entropyPtr[bestCell];
                if (e <= 1) continue;
                if (possiblePtr[bestCell] == 0UL)
                {
                    s.WfcComplete = 2;
                    return false;
                }

                var possible = possiblePtr[bestCell];
                var count = e;
                var chosen = rng.NextInt(0, count);
                var pattern = -1;
                var temp = possible;
                for (var i = 0; i <= chosen; i++)
                {
                    pattern = math.tzcnt(temp);
                    temp &= ~(1UL << pattern);
                }

                TryObserve(ref s, bestCell, pattern);
                if (!TryPropagate(ref s))
                {
                    s.WfcComplete = 2;
                    return false;
                }

                var dirtyPtr = s.Dirty;
                for (var i = 0; i < len; i++)
                    if (dirtyPtr[i] != 0)
                    {
                        dirtyPtr[i] = 0;
                        if (entropyPtr[i] > 1)
                            if (!s.ObserveHeap.TryInsertOrDecrease(new HeapNode(i, entropyPtr[i])))
                                return false;
                    }

                return true;
            }

            s.WfcComplete = 1;
            return false;
        }

        [BurstCompile]
        public static bool TryExtractOutput(ref WfcState s, ref NativeArray<int> output)
        {
            if (s.WfcComplete != 1) return false;

            var len = s.Grid.Length;
            var possiblePtr = s.PossibleBits;
            var outputPtr = (int*)output.GetUnsafePtr();
            for (var i = 0; i < len; i++)
            {
                var bits = possiblePtr[i];
                outputPtr[i] = bits == 0 ? -1 : math.tzcnt(bits);
            }

            return true;
        }

        public static void Dispose(ref WfcState s)
        {
            if (s.PossibleBits != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.PossibleBits); s.PossibleBits = null; }
            if (s.Entropy != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Entropy); s.Entropy = null; }
            if (s.Compatibility != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Compatibility); s.Compatibility = null; }
            if (s.Queue.IsCreated) s.Queue.Dispose();
            if (s.Dirty != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Dirty); s.Dirty = null; }
            if (s.ObserveHeap.IsCreated) s.ObserveHeap.Dispose();
        }
    }
}