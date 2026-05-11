using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.DStarLite
{
    public struct DStarLiteState
    {
        public Grid2D Grid;
        public int Start;
        public int Goal;
        public float Km;
        public NativeArray<float> G;
        public NativeArray<float> RHS;
        public MinHeap Open;
        public NativeArray<byte> InOpen;
        public NativeArray<int> Parent;
    }

    public static class DStarLiteApi
    {
        public static DStarLiteState Create(int width, int height, Allocator allocator)
        {
            var g = Grid2D.Create(width, height);
            return new DStarLiteState
            {
                Grid = g,
                G = new NativeArray<float>(g.Length, allocator),
                RHS = new NativeArray<float>(g.Length, allocator),
                Open = MinHeap.Create(g.Length, allocator),
                InOpen = new NativeArray<byte>(g.Length, allocator),
                Parent = new NativeArray<int>(g.Length, allocator),
            };
        }

        public static void Initialize(ref DStarLiteState s, int start, int goal, NativeArray<byte> blocked)
        {
            s.Start = start;
            s.Goal = goal;
            s.Km = 0f;
            s.Open.Clear();
            s.InOpen.Fill((byte)0);
            s.G.Fill(float.PositiveInfinity);
            s.RHS.Fill(float.PositiveInfinity);
            s.Parent.Fill(-1);

            if (blocked[goal] != 0) return; // goal blocked → no path possible

            s.RHS[goal] = 0f;
            var key = CalculateKey(ref s, goal);
            s.Open.InsertOrDecrease(new HeapNode(goal, key.x, key.y));
            s.InOpen[goal] = 1;
        }

        public static void NotifyMoved(ref DStarLiteState s, int newStart)
        {
            s.Km += Grid2D.HeuristicOctile(s.Grid.ToCoord(s.Start), s.Grid.ToCoord(newStart));
            s.Start = newStart;
        }

        public static void UpdateCell(ref DStarLiteState s, NativeArray<byte> blocked, NativeArray<float> cost, int cell)
        {
            UpdateVertex(ref s, blocked, cost, cell);
        }

        public static bool Repair(ref DStarLiteState s, NativeArray<byte> blocked, NativeArray<float> cost, int maxPops)
        {
            int pops = 0;
            while (!s.Open.IsEmpty && pops < maxPops)
            {
                var topKey = CalculateKey(ref s, s.Start);
                var openTop = s.Open.Peek();

                if (!LessOrEqual(openTop.Key0, openTop.Key1, topKey.x, topKey.y) &&
                    s.RHS[s.Start] == s.G[s.Start])
                    return true;

                var u = s.Open.Pop();
                s.InOpen[u.Id] = 0;
                pops++;

                int uid = u.Id;
                var uKey = new float2(u.Key0, u.Key1);
                var trueKey = CalculateKey(ref s, uid);

                if (Less(uKey.x, uKey.y, trueKey.x, trueKey.y))
                {
                    s.Open.InsertOrDecrease(new HeapNode(uid, trueKey.x, trueKey.y));
                    s.InOpen[uid] = 1;
                }
                else if (s.G[uid] > s.RHS[uid])
                {
                    s.G[uid] = s.RHS[uid];
                    UpdateSuccessors(ref s, blocked, cost, uid);
                }
                else
                {
                    s.G[uid] = float.PositiveInfinity;
                    UpdateVertex(ref s, blocked, cost, uid);
                    UpdateSuccessors(ref s, blocked, cost, uid);
                }
            }

            return s.RHS[s.Start] < float.PositiveInfinity;
        }

        public static void ExtractPath(ref DStarLiteState s, NativeArray<byte> blocked, NativeArray<float> cost, NativeList<int> path)
        {
            path.Clear();
            if (s.RHS[s.Start] >= float.PositiveInfinity) return;
            if (blocked[s.Start] != 0) return;

            int current = s.Start;
            path.Add(current);
            int maxSteps = s.Grid.Length * 2;
            int steps = 0;

            while (current != s.Goal && steps < maxSteps)
            {
                int best = -1;
                float bestCost = float.PositiveInfinity;
                float bestG = float.PositiveInfinity;

                int2 cp = s.Grid.ToCoord(current);
                for (int d = 0; d < 8; d++)
                {
                    int2 np = cp + Grid2D.Directions8[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (blocked[ni] != 0) continue;

                    float edgeCost = GetEdgeCost(s.Grid.Width, cost, current, ni, blocked);
                    float total = edgeCost + s.G[ni];
                    if (total < bestCost || (total == bestCost && s.G[ni] < bestG))
                    {
                        bestCost = total;
                        bestG = s.G[ni];
                        best = ni;
                    }
                }

                if (best < 0) break;
                current = best;
                path.Add(current);
                steps++;
            }
        }

        public static void Dispose(ref DStarLiteState s)
        {
            if (s.G.IsCreated) s.G.Dispose();
            if (s.RHS.IsCreated) s.RHS.Dispose();
            s.Open.Dispose();
            if (s.InOpen.IsCreated) s.InOpen.Dispose();
            if (s.Parent.IsCreated) s.Parent.Dispose();
        }

        private static float2 CalculateKey(ref DStarLiteState s, int cell)
        {
            float minGRhs = math.min(s.G[cell], s.RHS[cell]);
            float h = Grid2D.HeuristicOctile(s.Grid.ToCoord(s.Start), s.Grid.ToCoord(cell));
            return new float2(minGRhs + h + s.Km, minGRhs);
        }

        private static void UpdateVertex(ref DStarLiteState s, NativeArray<byte> blocked, NativeArray<float> cost, int cell)
        {
            if (cell != s.Goal)
            {
                float minRhs = float.PositiveInfinity;
                int2 cp = s.Grid.ToCoord(cell);
                for (int d = 0; d < 8; d++)
                {
                    int2 np = cp + Grid2D.Directions8[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (blocked[ni] != 0) continue;

                    float edgeCost = GetEdgeCost(s.Grid.Width, cost, cell, ni, blocked);
                    float candidate = edgeCost + s.G[ni];
                    if (candidate < minRhs)
                    {
                        minRhs = candidate;
                        s.Parent[cell] = ni;
                    }
                }
                s.RHS[cell] = minRhs;
            }

            if (s.G[cell] != s.RHS[cell])
            {
                var key = CalculateKey(ref s, cell);
                s.Open.InsertOrDecrease(new HeapNode(cell, key.x, key.y));
                s.InOpen[cell] = 1;
            }
            else
            {
                s.InOpen[cell] = 0;
            }
        }

        private static void UpdateSuccessors(ref DStarLiteState s, NativeArray<byte> blocked, NativeArray<float> cost, int cell)
        {
            int2 cp = s.Grid.ToCoord(cell);
            for (int d = 0; d < 8; d++)
            {
                int2 np = cp + Grid2D.Directions8[d];
                if (!s.Grid.InBounds(np)) continue;
                int ni = s.Grid.ToIndex(np);
                if (blocked[ni] != 0) continue;
                UpdateVertex(ref s, blocked, cost, ni);
            }
        }

        private static float GetEdgeCost(int gridWidth, NativeArray<float> cost, int from, int to, NativeArray<byte> blocked)
        {
            if (blocked[to] != 0) return float.PositiveInfinity;
            if (cost.Length > 0)
                return (cost[from] + cost[to]) * 0.5f;

            // Determine cardinal vs diagonal from index difference
            int diff = math.abs(from - to);
            if (diff == 1 || diff == gridWidth)
                return 1f; // cardinal
            return 1.414f; // diagonal
        }

        private static bool LessOrEqual(float k0a, float k1a, float k0b, float k1b)
        {
            if (k0a != k0b) return k0a <= k0b;
            return k1a <= k1b;
        }

        private static bool Less(float k0a, float k1a, float k0b, float k1b)
        {
            if (k0a != k0b) return k0a < k0b;
            return k1a < k1b;
        }
    }
}
