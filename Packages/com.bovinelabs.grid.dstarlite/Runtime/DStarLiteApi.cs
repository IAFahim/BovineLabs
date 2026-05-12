using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.DStarLite
{
    [StructLayout(LayoutKind.Sequential)]
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

    [BurstCompile]
    public unsafe static class DStarLiteApi
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

        [BurstCompile]
        public static void Initialize(ref DStarLiteState s, int start, int goal, NativeArray<byte> blocked)
        {
            s.Start = start;
            s.Goal = goal;
            s.Km = 0f;
            s.Open.Clear();

            float* gPtr = (float*)s.G.GetUnsafePtr();
            float* rhsPtr = (float*)s.RHS.GetUnsafePtr();
            byte* inOpen = (byte*)s.InOpen.GetUnsafePtr();
            int* parent = (int*)s.Parent.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++) { gPtr[i] = float.PositiveInfinity; rhsPtr[i] = float.PositiveInfinity; inOpen[i] = 0; parent[i] = -1; }

            byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            if (blk[goal] != 0) return;

            rhsPtr[goal] = 0f;
            var key = CalculateKey(ref s, goal);
            s.Open.InsertOrDecrease(new HeapNode(goal, key.x, key.y));
            inOpen[goal] = 1;
        }

        [BurstCompile]
        public static void NotifyMoved(ref DStarLiteState s, int newStart)
        {
            s.Km += Grid2D.HeuristicOctile(s.Grid.ToCoord(s.Start), s.Grid.ToCoord(newStart));
            s.Start = newStart;
        }

        [BurstCompile]
        public static void UpdateCell(ref DStarLiteState s, NativeArray<byte> blocked, NativeArray<float> cost, int cell)
        {
            UpdateVertex(ref s, blocked, cost, cell);
        }

        [BurstCompile]
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

        [BurstCompile]
        public static void ExtractPath(ref DStarLiteState s, NativeArray<byte> blocked, NativeArray<float> cost, NativeList<int> path)
        {
            path.Clear();
            if (s.RHS[s.Start] >= float.PositiveInfinity) return;
            if (blocked[s.Start] != 0) return;

            int current = s.Start;
            path.Add(current);
            int maxSteps = s.Grid.Length * 2;

            while (current != s.Goal && maxSteps-- > 0)
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

            int diff = math.abs(from - to);
            return math.select(1.414f, 1f, diff == 1 || diff == gridWidth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool LessOrEqual(float k0a, float k1a, float k0b, float k1b)
        {
            return k0a != k0b ? k0a <= k0b : k1a <= k1b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Less(float k0a, float k1a, float k0b, float k1b)
        {
            return k0a != k0b ? k0a < k0b : k1a < k1b;
        }
    }
}
