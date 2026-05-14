using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Jps
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct JpsState
    {
        public Grid2D Grid;
        public float* G;
        public int* Parent;
        public byte* Closed;
        public MinHeap Open;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class JpsApi
    {
        public static bool TryCreate(int width, int height, Allocator allocator, out JpsState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            if (!MinHeap.TryCreate(g.Length, allocator, out var open))
            {
                result = default;
                return false;
            }

            result = new JpsState
            {
                Allocator = allocator,
                Grid = g,
                G = (float*)Unity.Collections.AllocatorManager.Allocate(allocator, sizeof(float), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<float>(), g.Length),
                Parent = (int*)Unity.Collections.AllocatorManager.Allocate(allocator, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length),
                Closed = (byte*)Unity.Collections.AllocatorManager.Allocate(allocator, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), g.Length),
                Open = open
            };
            return true;
        }

        [BurstCompile]
        public static bool TrySearch(ref JpsState s, in NativeArray<byte> blocked, int start, int goal,
            ref NativeList<int> path)
        {
            if (!TryInitSearch(ref s, in blocked, start, goal)) return false;
            if (start == goal) { path.Add(start); return true; }
            if (s.Open.IsEmpty) return false;

            var blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            var w = s.Grid.Width;
            var h = s.Grid.Height;

            while (!s.Open.IsEmpty)
                if (TryStepSearch(ref s, blk, w, h, start, goal, ref path)) break;

            return path.Length > 0;
        }

        [BurstCompile]
        public static bool TryInitSearch(ref JpsState s, in NativeArray<byte> blocked, int start, int goal)
        {
            var g = s.G;
            var parent = s.Parent;
            var closed = s.Closed;
            var blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            var len = s.Grid.Length;

            for (var i = 0; i < len; i++)
            {
                g[i] = float.PositiveInfinity;
                parent[i] = -1;
                closed[i] = 0;
            }

            s.Open.Clear();

            if (blk[start] != 0 || blk[goal] != 0) return false;
            if (start == goal) return true;

            g[start] = 0f;
            s.Open.TryInsertOrDecrease(new HeapNode(start, Octile(0, 0, s.Grid.ToCoord(start), s.Grid.ToCoord(goal))));
            return true;
        }

        [BurstCompile]
        public static bool TryStepSearch(ref JpsState s, byte* blk, int w, int h, int start, int goal, ref NativeList<int> path)
        {
            if (s.Open.IsEmpty) return true;
            if (!s.Open.TryPop(out var current)) return true;

            var g = s.G;
            var parent = s.Parent;
            var closed = s.Closed;
            var cid = current.Id;
            closed[cid] = 1;

            if (cid == goal)
            {
                TryExtractPath(s.Parent, goal, start, ref path);
                return true;
            }

            var cp = s.Grid.ToCoord(cid);

            for (var d = 0; d < 8; d++)
            {
                var dir = Grid2D.Dir8(d);
                if (TryJump(blk, w, h, cp, dir, goal, out var jumpIdx))
                {
                    if (closed[jumpIdx] != 0) continue;
                    var jp = s.Grid.ToCoord(jumpIdx);
                    var cost = g[cid] + Octile(0, 0, cp, jp);
                    if (cost < g[jumpIdx])
                    {
                        g[jumpIdx] = cost;
                        parent[jumpIdx] = cid;
                        var f = cost + Octile(0, 0, jp, s.Grid.ToCoord(goal));
                        s.Open.TryInsertOrDecrease(new HeapNode(jumpIdx, f));
                    }
                }
            }

            return false;
        }

        public static bool TryJump(in JpsState s, in NativeArray<byte> blocked, int2 pos, int2 dir, int goal,
            out int jumpIdx)
        {
            var blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            return TryJump(blk, s.Grid.Width, s.Grid.Height, pos, dir, goal, out jumpIdx);
        }

        private static bool TryJump(byte* blk, int w, int h, int2 pos, int2 dir, int goal, out int jumpIdx)
        {
            jumpIdx = -1;
            var nx = pos.x + dir.x;
            var ny = pos.y + dir.y;

            if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) return false;
            var nIdx = ny * w + nx;
            if (blk[nIdx] != 0) return false;

            if (nIdx == goal)
            {
                jumpIdx = nIdx;
                return true;
            }

            if (HasForcedNeighbor(blk, w, h, nx, ny, dir))
            {
                jumpIdx = nIdx;
                return true;
            }

            if (dir.x != 0 && dir.y != 0)
            {
                if (TryJump(blk, w, h, new int2(nx, ny), new int2(dir.x, 0), goal, out _))
                {
                    jumpIdx = nIdx;
                    return true;
                }

                if (TryJump(blk, w, h, new int2(nx, ny), new int2(0, dir.y), goal, out _))
                {
                    jumpIdx = nIdx;
                    return true;
                }
            }

            return TryJump(blk, w, h, new int2(nx, ny), dir, goal, out jumpIdx);
        }

        [BurstCompile]
        public static bool TryExtractPath(int* parent, int goal, int start, ref NativeList<int> path)
        {
            path.Clear();
            var current = goal;
            while (current != start)
            {
                path.Add(current);
                current = parent[current];
                if (current < 0) break;
            }

            if (current == start)
                path.Add(start);

            var p = path.GetUnsafePtr();
            int lo = 0, hi = path.Length - 1;
            while (lo < hi)
            {
                var tmp = p[lo];
                p[lo] = p[hi];
                p[hi] = tmp;
                lo++;
                hi--;
            }

            return true;
        }

        public static void Dispose(ref JpsState s)
        {
            if (s.G != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.G); s.G = null; }
            if (s.Parent != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Parent); s.Parent = null; }
            if (s.Closed != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Closed); s.Closed = null; }
            if (s.Open.IsCreated) s.Open.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Octile(int _, int __, int2 a, int2 b)
        {
            var d = math.abs(a - b);
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
                for (var dy = -1; dy <= 1; dy += 2)
                {
                    var wy = py + dy;
                    if ((uint)wy >= (uint)h) continue;
                    if (blk[wy * w + px] != 0)
                    {
                        var fx = px + dir.x;
                        if ((uint)fx < (uint)w && blk[wy * w + fx] == 0) return true;
                    }
                }
            }
            else
            {
                for (var dx = -1; dx <= 1; dx += 2)
                {
                    var wx = px + dx;
                    if ((uint)wx >= (uint)w) continue;
                    if (blk[py * w + wx] != 0)
                    {
                        var fy = py + dir.y;
                        if ((uint)fy < (uint)h && blk[fy * w + wx] == 0) return true;
                    }
                }
            }

            return false;
        }
    }
}