using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Belief
{
    public struct BeliefState
    {
        public Grid2D Grid;
        public int LabelCount;
        public NativeArray<float> Unary;     // cell * L + label
        public NativeArray<float> Messages;  // cell * 4 * L + dir * L + label
        public NativeArray<float> Belief;    // cell * L + label
        public NativeArray<float> Scratch;
    }

    public static class BeliefApi
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
                Belief = new NativeArray<float>(g.Length * labelCount, a),
                Scratch = new NativeArray<float>(labelCount, a),
            };
        }

        public static void ClearMessages(ref BeliefState s)
        {
            s.Messages.Fill(0f);
        }

        public static void SetUnary(ref BeliefState s, NativeArray<float> unary)
        {
            NativeArray<float>.Copy(unary, s.Unary);
        }

        public static void Iterate(ref BeliefState s, NativeArray<float> pairwise, int iterations)
        {
            int L = s.LabelCount;
            for (int iter = 0; iter < iterations; iter++)
            {
                // For each directed edge u->v (4 directions)
                for (int cell = 0; cell < s.Grid.Length; cell++)
                {
                    int2 p = s.Grid.ToCoord(cell);
                    for (int dir = 0; dir < 4; dir++)
                    {
                        int2 np = p + Grid2D.Directions4[dir];
                        if (!s.Grid.InBounds(np)) continue;
                        int neighbor = s.Grid.ToIndex(np);

                        // Compute message from cell to neighbor in direction dir
                        int oppDir = (dir + 2) % 4;

                        for (int lv = 0; lv < L; lv++)
                        {
                            float best = float.PositiveInfinity;
                            for (int lu = 0; lu < L; lu++)
                            {
                                float cost = s.Unary[cell * L + lu];
                                // Sum messages from other directions (not from the neighbor we're sending to)
                                for (int od = 0; od < 4; od++)
                                {
                                    if (od == oppDir) continue; // skip message from target direction
                                    int2 op = p + Grid2D.Directions4[od];
                                    if (s.Grid.InBounds(op))
                                        cost += s.Messages[cell * 4 * L + od * L + lu];
                                }
                                cost += pairwise[lu * L + lv];
                                if (cost < best) best = cost;
                            }
                            s.Scratch[lv] = best;
                        }

                        // Normalize to prevent overflow
                        float minVal = float.PositiveInfinity;
                        for (int lv = 0; lv < L; lv++)
                            if (s.Scratch[lv] < minVal) minVal = s.Scratch[lv];

                        // Write message into neighbor's incoming slot for the opposite direction
                        int msgIdx = neighbor * 4 * L + oppDir * L;
                        for (int lv = 0; lv < L; lv++)
                            s.Messages[msgIdx + lv] = s.Scratch[lv] - minVal;
                    }
                }
            }
        }

        public static void DecodeMap(ref BeliefState s, NativeArray<int> labels)
        {
            int L = s.LabelCount;
            for (int cell = 0; cell < s.Grid.Length; cell++)
            {
                float bestCost = float.PositiveInfinity;
                int bestLabel = 0;
                for (int l = 0; l < L; l++)
                {
                    float cost = s.Unary[cell * L + l];
                    for (int dir = 0; dir < 4; dir++)
                        cost += s.Messages[cell * 4 * L + dir * L + l];
                    s.Belief[cell * L + l] = cost;
                    if (cost < bestCost) { bestCost = cost; bestLabel = l; }
                }
                labels[cell] = bestLabel;
            }
        }

        public static void Dispose(ref BeliefState s)
        {
            if (s.Unary.IsCreated) s.Unary.Dispose();
            if (s.Messages.IsCreated) s.Messages.Dispose();
            if (s.Belief.IsCreated) s.Belief.Dispose();
            if (s.Scratch.IsCreated) s.Scratch.Dispose();
        }
    }
}
