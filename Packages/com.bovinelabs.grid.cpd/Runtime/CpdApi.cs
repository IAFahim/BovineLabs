using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BovineLabs.Grid.Cpd
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CpdRun
    {
        public int Source;
        public int TargetMin;
        public int TargetMax;
        public byte FirstMove;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CpdState
    {
        public Grid2D Grid;
        public UnsafeList<CpdRun> Runs;
        public RangeI* SourceRuns;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class CpdApi
    {
        public static bool TryCreate(int width, int height, int maxRuns, Allocator a, out CpdState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new CpdState
            {
                Allocator = a,
                Grid = g,
                Runs = new UnsafeList<CpdRun>(maxRuns, a),
                SourceRuns = (RangeI*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(RangeI), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<RangeI>(), g.Length)
            };
            return true;
        }

        [BurstCompile]
        public static bool TryBuild(ref CpdState s, in NativeArray<byte> blocked)
        {
            if (!blocked.IsCreated) return false;

            s.Runs.Clear();
            var blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var len = s.Grid.Length;

            var firstMove = new Unity.Collections.NativeArray<byte>(len, Allocator.Temp);
            var fm = (byte*)firstMove.GetUnsafePtr();
            var dist = new Unity.Collections.NativeArray<float>(len, Allocator.Temp);
            var parent = new Unity.Collections.NativeArray<int>(len, Allocator.Temp);
            var queue = new UnsafeQueue<int>(Allocator.Temp);

            for (var source = 0; source < len; source++)
            {
                if (blk[source] != 0)
                {
                    s.SourceRuns[source] = new RangeI(0, 0);
                    continue;
                }

                var runStart = s.Runs.Length;
                var d = (float*)dist.GetUnsafePtr();
                var p = (int*)parent.GetUnsafePtr();
                for (var i = 0; i < len; i++)
                {
                    d[i] = float.PositiveInfinity;
                    p[i] = -1;
                }

                d[source] = 0f;

                queue.Clear();
                queue.Enqueue(source);

                while (queue.TryDequeue(out var u))
                {
                    var ux = u % w;
                    var uy = u / w;
                    var du = d[u];
                    if (ux + 1 < w) TryRelax(blk, d, p, queue, u, u + 1, du);
                    if (uy + 1 < h) TryRelax(blk, d, p, queue, u, u + w, du);
                    if (ux > 0) TryRelax(blk, d, p, queue, u, u - 1, du);
                    if (uy > 0) TryRelax(blk, d, p, queue, u, u - w, du);
                }

                for (var i = 0; i < len; i++) fm[i] = 255;

                for (var target = 0; target < len; target++)
                {
                    if (target == source || float.IsPositiveInfinity(d[target])) continue;
                    var cur = target;
                    while (p[cur] != source && p[cur] >= 0) cur = p[cur];
                    if (p[cur] == source)
                    {
                        var diff = s.Grid.ToCoord(cur) - s.Grid.ToCoord(source);
                        for (var dd = 0; dd < 4; dd++)
                            if (Grid2D.Dir4(dd).Equals(diff))
                            {
                                fm[target] = (byte)dd;
                                break;
                            }
                    }
                }

                byte currentMove = 255;
                var runTargetMin = -1;
                for (var target = 0; target < len; target++)
                {
                    if (fm[target] == 255) continue;
                    if (fm[target] != currentMove)
                    {
                        if (currentMove != 255)
                            s.Runs.Add(new CpdRun
                            {
                                Source = source, TargetMin = runTargetMin, TargetMax = target - 1,
                                FirstMove = currentMove
                            });
                        currentMove = fm[target];
                        runTargetMin = target;
                    }
                }

                if (currentMove != 255)
                    s.Runs.Add(new CpdRun
                        { Source = source, TargetMin = runTargetMin, TargetMax = len - 1, FirstMove = currentMove });

                s.SourceRuns[source] = new RangeI(runStart, s.Runs.Length - runStart);
            }

            firstMove.Dispose();
            dist.Dispose();
            parent.Dispose();
            queue.Dispose();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryRelax(byte* blk, float* d, int* p, UnsafeQueue<int> q, int u, int ni, float du)
        {
            if (blk[ni] != 0) return;
            var nd = du + 1f;
            if (nd < d[ni])
            {
                d[ni] = nd;
                p[ni] = u;
                q.Enqueue(ni);
            }
        }

        [BurstCompile]
        public static bool TryGetFirstMove(ref CpdState s, int source, int target, out byte move)
        {
            move = 255;
            if (Hint.Unlikely((uint)source >= (uint)s.Grid.Length)) return false;

            var range = s.SourceRuns[source];
            var runs = s.Runs.Ptr;
            for (var i = range.Offset; i < range.Offset + range.Count; i++)
                if (target >= runs[i].TargetMin && target <= runs[i].TargetMax)
                {
                    move = runs[i].FirstMove;
                    return true;
                }

            return false;
        }

        [BurstCompile]
        public static bool TryExtractPath(ref CpdState s, int source, int target, ref NativeList<int> path)
        {
            path.Clear();
            if (!s.Grid.InBounds(source) || !s.Grid.InBounds(target)) return false;

            var cur = source;
            path.Add(cur);
            var maxSteps = s.Grid.Length;

            while (cur != target && maxSteps-- > 0)
            {
                if (!TryGetFirstMove(ref s, cur, target, out var move)) break;
                var p2 = s.Grid.ToCoord(cur) + Grid2D.Dir4(move);
                if (!s.Grid.InBounds(p2)) break;
                cur = s.Grid.ToIndex(p2);
                path.Add(cur);
            }

            return path.Length > 0 && path[path.Length - 1] == target;
        }

        public static void Dispose(ref CpdState s)
        {
            if (s.Runs.IsCreated) s.Runs.Dispose();
            if (s.SourceRuns != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.SourceRuns); s.SourceRuns = null; }
        }
    }
}