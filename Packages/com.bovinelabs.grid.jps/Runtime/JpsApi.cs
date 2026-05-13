using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Jps
{
    [StructLayout(LayoutKind.Sequential)]
    public struct JpsState
    {
        public Grid2D Grid;
        public NativeArray<float> G;
        public NativeArray<int> Parent;
        public NativeArray<byte> Closed;
        public MinHeap Open;
    }

    [BurstCompile]
    public unsafe static class JpsApi
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

        [BurstCompile]
        public static bool Search(ref JpsState s, in NativeArray<byte> blocked, int start, int goal, ref NativeList<int> path)
        {
            path.Clear();
            float* g = (float*)s.G.GetUnsafePtr();
            int* parent = (int*)s.Parent.GetUnsafePtr();
            byte* closed = (byte*)s.Closed.GetUnsafePtr();
            byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;

            for (int i = 0; i < len; i++) { g[i] = float.PositiveInfinity; parent[i] = -1; closed[i] = 0; }
            s.Open.Clear();

            if (blk[start] != 0 || blk[goal] != 0) return false;
            if (start == goal) { path.Add(start); return true; }

            g[start] = 0f;
            s.Open.InsertOrDecrease(new HeapNode(start, Octile(0, 0, s.Grid.ToCoord(start), s.Grid.ToCoord(goal))));

            while (!s.Open.IsEmpty)
            {
                HeapNode current = s.Open.Pop();
                int cid = current.Id;
                closed[cid] = 1;

                if (cid == goal)
                {
                    ExtractPath(in s.Parent, goal, start, ref path);
                    return true;
                }

                int2 cp = s.Grid.ToCoord(cid);

                for (int d = 0; d < 8; d++)
                {
                    int2 dir = Grid2D.Dir8(d);
                    if (Jump(blk, w, h, cp, dir, goal, out int jumpIdx))
                    {
                        if (closed[jumpIdx] != 0) continue;
                        int2 jp = s.Grid.ToCoord(jumpIdx);
                        float cost = g[cid] + Octile(0, 0, cp, jp);
                        if (cost < g[jumpIdx])
                        {
                            g[jumpIdx] = cost;
                            parent[jumpIdx] = cid;
                            float f = cost + Octile(0, 0, jp, s.Grid.ToCoord(goal));
                            s.Open.InsertOrDecrease(new HeapNode(jumpIdx, f));
                        }
                    }
                }
            }

            return false;
        }

        public static bool Jump(in JpsState s, in NativeArray<byte> blocked, int2 pos, int2 dir, int goal, out int jumpIdx)
        {
            byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            return Jump(blk, s.Grid.Width, s.Grid.Height, pos, dir, goal, out jumpIdx);
        }

        private static bool Jump(byte* blk, int w, int h, int2 pos, int2 dir, int goal, out int jumpIdx)
        {
            jumpIdx = -1;
            int nx = pos.x + dir.x;
            int ny = pos.y + dir.y;

            if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) return false;
            int nIdx = ny * w + nx;
            if (blk[nIdx] != 0) return false;

            if (nIdx == goal) { jumpIdx = nIdx; return true; }
            if (HasForcedNeighbor(blk, w, h, nx, ny, dir)) { jumpIdx = nIdx; return true; }

            if (dir.x != 0 && dir.y != 0)
            {
                if (Jump(blk, w, h, new int2(nx, ny), new int2(dir.x, 0), goal, out _)) { jumpIdx = nIdx; return true; }
                if (Jump(blk, w, h, new int2(nx, ny), new int2(0, dir.y), goal, out _)) { jumpIdx = nIdx; return true; }
            }

            return Jump(blk, w, h, new int2(nx, ny), dir, goal, out jumpIdx);
        }

        [BurstCompile]
        public static void ExtractPath(in NativeArray<int> parent, int goal, int start, ref NativeList<int> path)
        {
            path.Clear();
            int current = goal;
            while (current != start)
            {
                path.Add(current);
                current = parent[current];
                if (current < 0) return;
            }
            path.Add(start);

            int* p = (int*)path.GetUnsafePtr();
            int lo = 0, hi = path.Length - 1;
            while (lo < hi) { int tmp = p[lo]; p[lo] = p[hi]; p[hi] = tmp; lo++; hi--; }
        }

        public static void Dispose(ref JpsState s)
        {
            if (s.G.IsCreated) s.G.Dispose();
            if (s.Parent.IsCreated) s.Parent.Dispose();
            if (s.Closed.IsCreated) s.Closed.Dispose();
            s.Open.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Octile(int _, int __, int2 a, int2 b)
        {
            int2 d = math.abs(a - b);
            return math.max(d.x, d.y) + 0.4142135f * math.min(d.x, d.y);
        }

        private static bool HasForcedNeighbor(byte* blk, int w, int h, int px, int py, int2 dir)
        {
            if (dir.x != 0 && dir.y != 0)
            {
                int nAx = px - dir.x, nAy = py;
                if ((uint)nAx < (uint)w && blk[nAy * w + nAx] != 0)
                {
                    int fx = nAx, fy = py + dir.y;
                    if ((uint)fx < (uint)w && (uint)fy < (uint)h && blk[fy * w + fx] == 0) return true;
                }
                int nBx = px, nBy = py - dir.y;
                if ((uint)nBy < (uint)h && blk[nBy * w + nBx] != 0)
                {
                    int fx = px + dir.x, fy = nBy;
                    if ((uint)fx < (uint)w && (uint)fy < (uint)h && blk[fy * w + fx] == 0) return true;
                }
            }
            else if (dir.x != 0)
            {
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    int wy = py + dy;
                    if ((uint)wy >= (uint)h) continue;
                    if (blk[wy * w + px] != 0)
                    {
                        int fx = px + dir.x;
                        if ((uint)fx < (uint)w && blk[wy * w + fx] == 0) return true;
                    }
                }
            }
            else
            {
                for (int dx = -1; dx <= 1; dx += 2)
                {
                    int wx = px + dx;
                    if ((uint)wx >= (uint)w) continue;
                    if (blk[py * w + wx] != 0)
                    {
                        int fy = py + dir.y;
                        if ((uint)fy < (uint)h && blk[fy * w + wx] == 0) return true;
                    }
                }
            }
            return false;
        }
    }
}
