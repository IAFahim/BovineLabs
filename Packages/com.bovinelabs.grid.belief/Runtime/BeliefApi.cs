using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Belief
{
    public struct BeliefState
    {
        public Grid2D Grid;
        public int LabelCount;
        public NativeArray<float> Unary;     // cell * L + label
        public NativeArray<float> Messages;  // cell * 4 * L + dir * L + label
        public NativeArray<float> MessagesNext;
        public NativeArray<float> Belief;    // cell * L + label
        public NativeArray<float> Scratch;
    }

    [BurstCompile]
    public unsafe static class BeliefApi
    {
        public static BeliefState Create(int width, int height, int labelCount, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new BeliefState
            {
                Grid = g,
                LabelCount = labelCount,
                Unary = new NativeArray<float>(g.Length * labelCount, a),
                Messages = new NativeArray<float>(g.Length * 4 * labelCount, a),
                MessagesNext = new NativeArray<float>(g.Length * 4 * labelCount, a),
                Belief = new NativeArray<float>(g.Length * labelCount, a),
                Scratch = new NativeArray<float>(labelCount, a),
            };
        }

        public static void ClearMessages(ref BeliefState s)
        {
            s.Messages.Fill(0f);
            s.MessagesNext.Fill(0f);
        }

        public static void SetUnary(ref BeliefState s, in NativeArray<float> unary)
        {
            NativeArray<float>.Copy(unary, s.Unary);
        }

        [BurstCompile]
        public static void Iterate(ref BeliefState s, in NativeArray<float> pairwise, int iterations)
        {
            int L = s.LabelCount;
            int cellCount = s.Grid.Length;
            int width = s.Grid.Width;
            int height = s.Grid.Height;

            float* unaryPtr = (float*)s.Unary.GetUnsafeReadOnlyPtr();
            float* pairwisePtr = (float*)pairwise.GetUnsafeReadOnlyPtr();
            float* messagesPtr = (float*)s.Messages.GetUnsafePtr();
            float* nextMessagesPtr = (float*)s.MessagesNext.GetUnsafePtr();
            float* scratchPtr = (float*)s.Scratch.GetUnsafePtr();

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int cell = 0; cell < cellCount; cell++)
                {
                    int y = cell / width;
                    int x = cell % width;

                    for (int dir = 0; dir < 4; dir++)
                    {
                        int2 offset = Grid2D.Dir4(dir);
                        int nx = x + offset.x;
                        int ny = y + offset.y;

                        if (Hint.Unlikely(nx < 0 || ny < 0 || nx >= width || ny >= height)) continue;
                        
                        int neighbor = ny * width + nx;
                        int oppDir = (dir + 2) % 4;

                        // Compute message from cell to neighbor in direction dir
                        for (int lv = 0; lv < L; lv++)
                        {
                            float best = float.PositiveInfinity;
                            for (int lu = 0; lu < L; lu++)
                            {
                                float cost = unaryPtr[cell * L + lu];
                                
                                // Sum messages from other directions
                                for (int od = 0; od < 4; od++)
                                {
                                    if (od == oppDir) continue;
                                    int2 oOffset = Grid2D.Dir4(od);
                                    int ox = x + oOffset.x;
                                    int oy = y + oOffset.y;
                                    
                                    if (Hint.Likely(ox >= 0 && oy >= 0 && ox < width && oy < height))
                                        cost += messagesPtr[cell * 4 * L + od * L + lu];
                                }
                                cost += pairwisePtr[lu * L + lv];
                                if (cost < best) best = cost;
                            }
                            scratchPtr[lv] = best;
                        }

                        // Normalize
                        float minVal = float.PositiveInfinity;
                        for (int lv = 0; lv < L; lv++)
                            if (scratchPtr[lv] < minVal) minVal = scratchPtr[lv];

                        int msgIdx = neighbor * 4 * L + oppDir * L;
                        for (int lv = 0; lv < L; lv++)
                            nextMessagesPtr[msgIdx + lv] = scratchPtr[lv] - minVal;
                    }
                }

                // Swap pointers conceptually by swapping the NativeArray handles in the state
                var tmp = s.Messages;
                s.Messages = s.MessagesNext;
                s.MessagesNext = tmp;
                
                // Update pointers for next iteration
                messagesPtr = (float*)s.Messages.GetUnsafePtr();
                nextMessagesPtr = (float*)s.MessagesNext.GetUnsafePtr();
            }
        }

        [BurstCompile]
        public static void DecodeMap(ref BeliefState s, ref NativeArray<int> labels)
        {
            int L = s.LabelCount;
            int cellCount = s.Grid.Length;
            float* unaryPtr = (float*)s.Unary.GetUnsafeReadOnlyPtr();
            float* messagesPtr = (float*)s.Messages.GetUnsafeReadOnlyPtr();
            float* beliefPtr = (float*)s.Belief.GetUnsafePtr();
            int* labelsPtr = (int*)labels.GetUnsafePtr();

            for (int cell = 0; cell < cellCount; cell++)
            {
                float bestCost = float.PositiveInfinity;
                int bestLabel = 0;
                for (int l = 0; l < L; l++)
                {
                    float cost = unaryPtr[cell * L + l];
                    for (int dir = 0; dir < 4; dir++)
                        cost += messagesPtr[cell * 4 * L + dir * L + l];
                    beliefPtr[cell * L + l] = cost;
                    if (cost < bestCost) { bestCost = cost; bestLabel = l; }
                }
                labelsPtr[cell] = bestLabel;
            }
        }

        public static void Dispose(ref BeliefState s)
        {
            if (s.Unary.IsCreated) s.Unary.Dispose();
            if (s.Messages.IsCreated) s.Messages.Dispose();
            if (s.MessagesNext.IsCreated) s.MessagesNext.Dispose();
            if (s.Belief.IsCreated) s.Belief.Dispose();
            if (s.Scratch.IsCreated) s.Scratch.Dispose();
        }
    }
}
