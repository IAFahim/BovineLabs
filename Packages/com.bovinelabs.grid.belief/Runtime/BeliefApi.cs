using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BovineLabs.Grid.Belief
{
    [BurstCompile]
    public static unsafe class BeliefApi
    {
        public static bool TryCreate(int width, int height, int labelCount, Allocator a, out BeliefState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g) || labelCount <= 0)
            {
                result = default;
                return false;
            }

            result = new BeliefState
            {
                Grid = g,
                LabelCount = labelCount,
                Allocator = a,
                Unary = (float*)AllocatorManager.Allocate(a, sizeof(float), UnsafeUtility.AlignOf<float>(), g.Length * labelCount),
                Messages = (float*)AllocatorManager.Allocate(a, sizeof(float), UnsafeUtility.AlignOf<float>(), g.Length * 4 * labelCount),
                MessagesNext = (float*)AllocatorManager.Allocate(a, sizeof(float), UnsafeUtility.AlignOf<float>(), g.Length * 4 * labelCount),
                Belief = (float*)AllocatorManager.Allocate(a, sizeof(float), UnsafeUtility.AlignOf<float>(), g.Length * labelCount),
                Scratch = (float*)AllocatorManager.Allocate(a, sizeof(float), UnsafeUtility.AlignOf<float>(), labelCount)
            };
            return true;
        }

        public static void ClearMessages(ref BeliefState s)
        {
            UnsafeUtility.MemSet(s.Messages, 0, s.Grid.Length * 4 * s.LabelCount * sizeof(float));
            UnsafeUtility.MemSet(s.MessagesNext, 0, s.Grid.Length * 4 * s.LabelCount * sizeof(float));
        }

        public static void SetUnary(ref BeliefState s, in NativeArray<float> unary)
        {
            UnsafeUtility.MemCpy(s.Unary, unary.GetUnsafeReadOnlyPtr(), s.Grid.Length * s.LabelCount * sizeof(float));
        }

        [BurstCompile]
        public static bool TryIterate(ref BeliefState s, in NativeArray<float> pairwise, int iterations)
        {
            if (!pairwise.IsCreated || iterations < 0) return false;

            var L = s.LabelCount;
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var cellCount = w * h;

            var unaryPtr = s.Unary;
            var pairwisePtr = (float*)pairwise.GetUnsafeReadOnlyPtr();
            var messagesPtr = s.Messages;
            var nextMsgPtr = s.MessagesNext;
            var scratchPtr = s.Scratch;


            for (var iter = 0; iter < iterations; iter++)
            {
                UnsafeUtility.MemSet(nextMsgPtr, 0, (long)cellCount * 4 * L * sizeof(float));

                var cellIdx = 0;
                for (var cy = 0; cy < h; cy++)
                for (var cx = 0; cx < w; cx++)
                {
                    var canDir0 = cx < w - 1; // (x+1, y)
                    var canDir1 = cy < h - 1; // (x, y+1)
                    var canDir2 = cx > 0; // (x-1, y)
                    var canDir3 = cy > 0; // (x, y-1)

                    var ni0 = cellIdx + 1; // dir 0: (x+1, y)
                    var ni1 = cellIdx + w; // dir 1: (x, y+1)
                    var ni2 = cellIdx - 1; // dir 2: (x-1, y)
                    var ni3 = cellIdx - w; // dir 3: (x, y-1)

                    for (var dir = 0; dir < 4; dir++)
                    {
                        int ni;
                        var oppDir = (dir + 2) & 3;

                        if (dir == 0)
                        {
                            if (!canDir0) continue;
                            ni = ni0;
                        }
                        else if (dir == 1)
                        {
                            if (!canDir1) continue;
                            ni = ni1;
                        }
                        else if (dir == 2)
                        {
                            if (!canDir2) continue;
                            ni = ni2;
                        }
                        else
                        {
                            if (!canDir3) continue;
                            ni = ni3;
                        }

                        var msgBase = cellIdx * 4 * L;
                        var unaryBase = cellIdx * L;

                        for (var lv = 0; lv < L; lv++)
                        {
                            var best = float.MaxValue;
                            var pairBase = lv;

                            for (var lu = 0; lu < L; lu++)
                            {
                                var cost = unaryPtr[unaryBase + lu];

                                if (oppDir != 0) cost += messagesPtr[msgBase + 0 * L + lu];
                                if (oppDir != 1) cost += messagesPtr[msgBase + 1 * L + lu];
                                if (oppDir != 2) cost += messagesPtr[msgBase + 2 * L + lu];
                                if (oppDir != 3) cost += messagesPtr[msgBase + 3 * L + lu];

                                cost += pairwisePtr[lu * L + pairBase];

                                if (cost < best) best = cost;
                            }

                            scratchPtr[lv] = best;
                        }

                        var minVal = float.MaxValue;
                        for (var lv = 0; lv < L; lv++)
                            if (scratchPtr[lv] < minVal)
                                minVal = scratchPtr[lv];

                        var writeIdx = ni * 4 * L + oppDir * L;
                        for (var lv = 0; lv < L; lv++)
                            nextMsgPtr[writeIdx + lv] = scratchPtr[lv] - minVal;
                    }

                    cellIdx++;
                }

                var tempMsg = s.Messages;
                s.Messages = s.MessagesNext;
                s.MessagesNext = tempMsg;

                messagesPtr = s.Messages;
                nextMsgPtr = s.MessagesNext;
            }

            return true;
        }

        [BurstCompile]
        public static bool TryDecodeMap(ref BeliefState s, ref NativeArray<int> labels)
        {
            if (!labels.IsCreated || labels.Length < s.Grid.Length) return false;

            var L = s.LabelCount;
            var cellCount = s.Grid.Length;
            var unaryPtr = s.Unary;
            var messagesPtr = s.Messages;
            var beliefPtr = s.Belief;
            var labelsPtr = (int*)labels.GetUnsafePtr();

            for (var cell = 0; cell < cellCount; cell++)
            {
                var bestCost = float.MaxValue;
                var bestLabel = 0;
                var unaryBase = cell * L;
                var msgBase = cell * 4 * L;

                for (var l = 0; l < L; l++)
                {
                    var cost = unaryPtr[unaryBase + l]
                               + messagesPtr[msgBase + 0 * L + l]
                               + messagesPtr[msgBase + 1 * L + l]
                               + messagesPtr[msgBase + 2 * L + l]
                               + messagesPtr[msgBase + 3 * L + l];

                    beliefPtr[unaryBase + l] = cost;
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestLabel = l;
                    }
                }

                labelsPtr[cell] = bestLabel;
            }

            return true;
        }

        public static void Dispose(ref BeliefState s)
        {
            if (s.Unary != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Unary); s.Unary = null; }
            if (s.Messages != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Messages); s.Messages = null; }
            if (s.MessagesNext != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.MessagesNext); s.MessagesNext = null; }
            if (s.Belief != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Belief); s.Belief = null; }
            if (s.Scratch != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Scratch); s.Scratch = null; }
        }
    }
}