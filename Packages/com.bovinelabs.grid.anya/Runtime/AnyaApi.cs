using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Anya
{
    public struct Interval
    {
        public int Y;
        public float XL, XR;

        public bool Contains(float x) => x >= XL && x <= XR;
    }

    public struct AnyaNode
    {
        public Interval Interval;
        public float2 Root;
        public float G;
        public int Parent; // Index in the pool
    }

    public struct AnyaState
    {
        public Grid2D Grid;
        public MinHeap Heap;
        public UnsafeList<AnyaNode> Pool;
    }

    [BurstCompile]
    public unsafe static class AnyaApi
    {
        public static AnyaState Create(int width, int height, int maxNodes, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new AnyaState
            {
                Grid = g,
                Heap = MinHeap.Create(maxNodes, a),
                Pool = new UnsafeList<AnyaNode>(maxNodes, a),
            };
        }

        [BurstCompile]
        public static bool Search(
            ref AnyaState s,
            in NativeArray<byte> blocked,
            int2 start,
            int2 goal,
            ref NativeList<int2> path)
        {
            path.Clear();
            s.Heap.Clear();
            s.Pool.Clear();

            int width = s.Grid.Width;
            int height = s.Grid.Height;
            byte* blockedPtr = (byte*)blocked.GetUnsafeReadOnlyPtr();

            if (Hint.Unlikely(!s.Grid.InBounds(start) || !s.Grid.InBounds(goal))) return false;
            if (Hint.Unlikely(blockedPtr[start.y * width + start.x] != 0 || blockedPtr[goal.y * width + goal.x] != 0)) return false;

            if (start.Equals(goal))
            {
                path.Add(start);
                return true;
            }

            var startNode = new AnyaNode
            {
                Interval = new Interval { Y = start.y, XL = start.x, XR = start.x },
                Root = new float2(start.x, start.y),
                G = 0,
                Parent = -1
            };

            int startIdx = s.Pool.Length;
            s.Pool.Add(startNode);
            s.Heap.InsertOrDecrease(new HeapNode(startIdx, Grid2D.HeuristicEuclidean(start, goal)));

            while (!s.Heap.IsEmpty)
            {
                int uIdx = s.Heap.Pop().Id;
                AnyaNode* u = s.Pool.Ptr + uIdx;

                if (u->Interval.Y == goal.y && u->Interval.Contains(goal.x))
                {
                    if (LineOfSight(in s.Grid, blockedPtr, new int2((int)math.round(u->Root.x), (int)math.round(u->Root.y)), goal))
                    {
                        ExtractPath(in s.Pool, uIdx, goal, ref path);
                        return true;
                    }
                }
                
                Expand(ref s, uIdx, blockedPtr, goal);
            }

            return false;
        }

        [BurstCompile]
        private static void Expand(ref AnyaState s, int uIdx, [NoAlias] byte* blocked, int2 goal)
        {
            AnyaNode* u = s.Pool.Ptr + uIdx;
            
            if (u->Interval.Y >= u->Root.y) ExpandNextRow(ref s, uIdx, 1, blocked, goal);
            if (u->Interval.Y <= u->Root.y) ExpandNextRow(ref s, uIdx, -1, blocked, goal);
        }

        [BurstCompile]
        private static void ExpandNextRow(ref AnyaState s, int uIdx, int dy, [NoAlias] byte* blocked, int2 goal)
        {
            AnyaNode* u = s.Pool.Ptr + uIdx;
            int nextY = u->Interval.Y + dy;
            int width = s.Grid.Width;
            int height = s.Grid.Height;

            if (Hint.Unlikely(nextY < 0 || nextY > height)) return;

            float h1 = u->Interval.Y - u->Root.y;
            float h2 = nextY - u->Root.y;
            
            float projL, projR;
            if (Hint.Unlikely(math.abs(h1) < 0.001f))
            {
                projL = 0; projR = width;
            }
            else
            {
                float ratio = h2 / h1;
                projL = u->Root.x + (u->Interval.XL - u->Root.x) * ratio;
                projR = u->Root.x + (u->Interval.XR - u->Root.x) * ratio;
            }

            if (projL > projR) { float tmp = projL; projL = projR; projR = tmp; }

            int cellY = (dy > 0) ? u->Interval.Y : u->Interval.Y - 1;
            if (Hint.Unlikely(cellY < 0 || cellY >= height)) return;

            int xMin = (int)math.floor(projL);
            int xMax = (int)math.ceil(projR);
            xMin = math.max(0, xMin);
            xMax = math.min(width, xMax);

            float currentL = -1;
            for (int x = xMin; x < xMax; x++)
            {
                if (blocked[cellY * width + x] == 0)
                {
                    if (currentL < 0) currentL = x;
                }
                else
                {
                    if (currentL >= 0)
                    {
                        AddSuccessor(ref s, uIdx, nextY, math.max(projL, currentL), math.min(projR, x), goal);
                        currentL = -1;
                    }
                }
            }
            if (currentL >= 0)
            {
                AddSuccessor(ref s, uIdx, nextY, math.max(projL, currentL), math.min(projR, xMax), goal);
            }
        }

        [BurstCompile]
        private static void AddSuccessor(ref AnyaState s, int parentIdx, int y, float xl, float xr, int2 goal)
        {
            if (xl >= xr) return;
            
            AnyaNode* parent = s.Pool.Ptr + parentIdx;
            var node = new AnyaNode
            {
                Interval = new Interval { Y = y, XL = xl, XR = xr },
                Root = parent->Root,
                G = parent->G,
                Parent = parentIdx
            };
            
            float2 mid = new float2((xl + xr) * 0.5f, y);
            node.G = parent->G + math.distance(parent->Root, mid);

            int idx = s.Pool.Length;
            s.Pool.Add(node);
            float f = node.G + Grid2D.HeuristicEuclidean(new int2((int)math.round(mid.x), y), goal);
            s.Heap.InsertOrDecrease(new HeapNode(idx, f));
        }

        [BurstCompile]
        private static void ExtractPath(in UnsafeList<AnyaNode> pool, int nodeIdx, int2 goal, ref NativeList<int2> path)
        {
            path.Add(goal);
            int cur = nodeIdx;
            while (cur >= 0)
            {
                AnyaNode* node = pool.Ptr + cur;
                var rootInt = new int2((int)math.round(node->Root.x), (int)math.round(node->Root.y));
                if (Hint.Likely(!path[path.Length-1].Equals(rootInt)))
                    path.Add(rootInt);
                cur = node->Parent;
            }
            
            for (int i = 0, j = path.Length - 1; i < j; i++, j--)
            {
                var tmp = path[i]; path[i] = path[j]; path[j] = tmp;
            }
        }

        [BurstCompile]
        public static bool LineOfSight(in Grid2D grid, [NoAlias] byte* blocked, int2 from, int2 to)
        {
            int dx = math.abs(to.x - from.x);
            int dy = math.abs(to.y - from.y);
            int sx = from.x < to.x ? 1 : -1;
            int sy = from.y < to.y ? 1 : -1;
            int err = dx - dy;
            int x = from.x, y = from.y;
            int width = grid.Width;
            int height = grid.Height;

            while (true)
            {
                if (Hint.Unlikely(x < 0 || y < 0 || x >= width || y >= height)) return false;
                if (blocked[y * width + x] != 0) return false;
                if (x == to.x && y == to.y) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
            return true;
        }

        public static void Dispose(ref AnyaState s)
        {
            s.Heap.Dispose();
            if (s.Pool.IsCreated) s.Pool.Dispose();
        }
    }
}
