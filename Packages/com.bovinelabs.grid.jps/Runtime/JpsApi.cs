using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Jps
{
    public struct JpsState
    {
        public Grid2D Grid;
        public NativeArray<float> G;
        public NativeArray<int> Parent;
        public NativeArray<byte> Closed;
        public MinHeap Open;
    }

    public static class JpsApi
    {
        public static JpsState Create(int width, int height, Allocator allocator)
        {
            var g = Grid2D.Create(width, height);
            return new JpsState
            {
                Grid = g,
                G = new NativeArray<float>(g.Length, allocator),
                Parent = new NativeArray<int>(g.Length, allocator),
                Closed = new NativeArray<byte>(g.Length, allocator),
                Open = MinHeap.Create(g.Length, allocator),
            };
        }

        public static bool Search(ref JpsState s, NativeArray<byte> blocked, int start, int goal, NativeList<int> path)
        {
            path.Clear();
            s.G.Fill(float.PositiveInfinity);
            s.Parent.Fill(-1);
            s.Closed.Fill((byte)0);
            s.Open.Clear();

            if (blocked[start] != 0 || blocked[goal] != 0) return false;
            if (start == goal) { path.Add(start); return true; }

            s.G[start] = 0f;
            s.Open.InsertOrDecrease(new HeapNode(start, Heuristic(s, start, goal)));

            while (!s.Open.IsEmpty)
            {
                HeapNode current = s.Open.Pop();
                int cid = current.Id;
                s.Closed[cid] = 1;

                if (cid == goal)
                {
                    ExtractPath(s.Parent, goal, start, path);
                    return true;
                }

                int2 cp = s.Grid.ToCoord(cid);

                // Identify successors
                for (int d = 0; d < 8; d++)
                {
                    int2 dir = Grid2D.Directions8[d];
                    if (Jump(s, blocked, cp, dir, goal, out int jumpIdx))
                    {
                        if (s.Closed[jumpIdx] != 0) continue;

                        int2 jp = s.Grid.ToCoord(jumpIdx);
                        float cost = s.G[cid] + Grid2D.HeuristicOctile(cp, jp);

                        if (cost < s.G[jumpIdx])
                        {
                            s.G[jumpIdx] = cost;
                            s.Parent[jumpIdx] = cid;
                            float f = cost + Heuristic(s, jumpIdx, goal);
                            s.Open.InsertOrDecrease(new HeapNode(jumpIdx, f));
                        }
                    }
                }
            }

            return false;
        }

        public static bool Jump(in JpsState s, NativeArray<byte> blocked, int2 pos, int2 dir, int goal, out int jumpIdx)
        {
            jumpIdx = -1;
            int2 next = pos + dir;

            if (!s.Grid.InBounds(next) || blocked[s.Grid.ToIndex(next)] != 0)
                return false;

            int nIdx = s.Grid.ToIndex(next);

            if (nIdx == goal)
            {
                jumpIdx = nIdx;
                return true;
            }

            // Forced neighbor check
            if (HasForcedNeighbor(s, blocked, next, dir))
            {
                jumpIdx = nIdx;
                return true;
            }

            // Diagonal: recurse both cardinal components first
            if (dir.x != 0 && dir.y != 0)
            {
                if (Jump(s, blocked, next, new int2(dir.x, 0), goal, out _))
                {
                    jumpIdx = nIdx;
                    return true;
                }
                if (Jump(s, blocked, next, new int2(0, dir.y), goal, out _))
                {
                    jumpIdx = nIdx;
                    return true;
                }
            }

            // Continue straight
            return Jump(s, blocked, next, dir, goal, out jumpIdx);
        }

        public static void ExtractPath(NativeArray<int> parent, int goal, int start, NativeList<int> path)
        {
            path.Clear();
            int current = goal;
            while (current != start)
            {
                path.Add(current);
                current = parent[current];
                if (current < 0) return; // broken path
            }
            path.Add(start);

            // Reverse in-place
            for (int i = 0, j = path.Length - 1; i < j; i++, j--)
            {
                int tmp = path[i];
                path[i] = path[j];
                path[j] = tmp;
            }
        }

        public static void Dispose(ref JpsState s)
        {
            if (s.G.IsCreated) s.G.Dispose();
            if (s.Parent.IsCreated) s.Parent.Dispose();
            if (s.Closed.IsCreated) s.Closed.Dispose();
            s.Open.Dispose();
        }

        private static float Heuristic(in JpsState s, int a, int b)
        {
            return Grid2D.HeuristicOctile(s.Grid.ToCoord(a), s.Grid.ToCoord(b));
        }

        private static bool HasForcedNeighbor(in JpsState s, NativeArray<byte> blocked, int2 pos, int2 dir)
        {
            if (dir.x != 0 && dir.y != 0)
            {
                // Diagonal: check for forced neighbors
                // Moving (dx, dy), forced if perpendicular is blocked
                int2 perpA = new int2(-dir.x, 0);
                int2 perpB = new int2(0, -dir.y);

                int2 nA = pos + perpA;
                int2 nB = pos + perpB;

                if (s.Grid.InBounds(nA) && blocked[s.Grid.ToIndex(nA)] != 0)
                {
                    int2 forced = pos + perpA + new int2(0, dir.y);
                    if (s.Grid.InBounds(forced) && blocked[s.Grid.ToIndex(forced)] == 0)
                        return true;
                }

                if (s.Grid.InBounds(nB) && blocked[s.Grid.ToIndex(nB)] != 0)
                {
                    int2 forced = pos + perpB + new int2(dir.x, 0);
                    if (s.Grid.InBounds(forced) && blocked[s.Grid.ToIndex(forced)] == 0)
                        return true;
                }
            }
            else
            {
                // Cardinal: check for forced neighbors
                if (dir.x != 0)
                {
                    // Horizontal movement
                    for (int dy = -1; dy <= 1; dy += 2)
                    {
                        int2 wall = pos + new int2(0, dy);
                        if (s.Grid.InBounds(wall) && blocked[s.Grid.ToIndex(wall)] != 0)
                        {
                            int2 forced = wall + new int2(dir.x, 0);
                            if (s.Grid.InBounds(forced) && blocked[s.Grid.ToIndex(forced)] == 0)
                                return true;
                        }
                    }
                }
                else
                {
                    // Vertical movement
                    for (int dx = -1; dx <= 1; dx += 2)
                    {
                        int2 wall = pos + new int2(dx, 0);
                        if (s.Grid.InBounds(wall) && blocked[s.Grid.ToIndex(wall)] != 0)
                        {
                            int2 forced = wall + new int2(0, dir.y);
                            if (s.Grid.InBounds(forced) && blocked[s.Grid.ToIndex(forced)] == 0)
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
