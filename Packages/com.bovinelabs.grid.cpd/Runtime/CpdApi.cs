using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Cpd
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CpdRun { public int Source; public int TargetMin; public int TargetMax; public byte FirstMove; }

    [StructLayout(LayoutKind.Sequential)]
    public struct CpdState
    {
        public Grid2D Grid;
        public UnsafeList<CpdRun> Runs;
        public NativeArray<RangeI> SourceRuns;
    }

    [BurstCompile]
    public unsafe static class CpdApi
    {
        public static CpdState Create(int width, int height, int maxRuns, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new CpdState
            {
                Grid = g,
                Runs = new UnsafeList<CpdRun>(maxRuns, a),
                SourceRuns = new NativeArray<RangeI>(g.Length, a),
            };
        }

        [BurstCompile]
        public static void Build(ref CpdState s, NativeArray<byte> blocked)
        {
            s.Runs.Clear();
            byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;

            var firstMove = new NativeArray<byte>(len, Allocator.Temp);
            byte* fm = (byte*)firstMove.GetUnsafePtr();
            var dist = new NativeArray<float>(len, Allocator.Temp);
            var parent = new NativeArray<int>(len, Allocator.Temp);
            var queue = new UnsafeQueue<int>(Allocator.Temp);

            for (int source = 0; source < len; source++)
            {
                if (blk[source] != 0)
                {
                    s.SourceRuns[source] = new RangeI(0, 0);
                    continue;
                }

                int runStart = s.Runs.Length;
                float* d = (float*)dist.GetUnsafePtr();
                int* p = (int*)parent.GetUnsafePtr();
                for (int i = 0; i < len; i++) { d[i] = float.PositiveInfinity; p[i] = -1; }
                d[source] = 0f;

                queue.Clear();
                queue.Enqueue(source);

                while (queue.TryDequeue(out int u))
                {
                    int ux = u % w;
                    int uy = u / w;
                    float du = d[u];
                    // Right, Up, Left, Down
                    if (ux + 1 < w) TryRelax(blk, d, p, queue, u, u + 1, du);
                    if (uy + 1 < h) TryRelax(blk, d, p, queue, u, u + w, du);
                    if (ux > 0) TryRelax(blk, d, p, queue, u, u - 1, du);
                    if (uy > 0) TryRelax(blk, d, p, queue, u, u - w, du);
                }

                for (int i = 0; i < len; i++) fm[i] = 255;

                for (int target = 0; target < len; target++)
                {
                    if (target == source || float.IsPositiveInfinity(d[target])) continue;
                    int cur = target;
                    while (p[cur] != source && p[cur] >= 0) cur = p[cur];
                    if (p[cur] == source)
                    {
                        int2 diff = s.Grid.ToCoord(cur) - s.Grid.ToCoord(source);
                        for (int dd = 0; dd < 4; dd++)
                        {
                            if (Grid2D.Directions4[dd].Equals(diff)) { fm[target] = (byte)dd; break; }
                        }
                    }
                }

                byte currentMove = 255;
                int runTargetMin = -1;
                for (int target = 0; target < len; target++)
                {
                    if (fm[target] == 255) continue;
                    if (fm[target] != currentMove)
                    {
                        if (currentMove != 255)
                            s.Runs.Add(new CpdRun { Source = source, TargetMin = runTargetMin, TargetMax = target - 1, FirstMove = currentMove });
                        currentMove = fm[target];
                        runTargetMin = target;
                    }
                }
                if (currentMove != 255)
                    s.Runs.Add(new CpdRun { Source = source, TargetMin = runTargetMin, TargetMax = len - 1, FirstMove = currentMove });

                s.SourceRuns[source] = new RangeI(runStart, s.Runs.Length - runStart);
            }

            firstMove.Dispose();
            dist.Dispose();
            parent.Dispose();
            queue.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryRelax(byte* blk, float* d, int* p, UnsafeQueue<int> q, int u, int ni, float du)
        {
            if (blk[ni] != 0) return;
            float nd = du + 1f;
            if (nd < d[ni]) { d[ni] = nd; p[ni] = u; q.Enqueue(ni); }
        }

        [BurstCompile]
        public static bool TryGetFirstMove(ref CpdState s, int source, int target, out byte move)
        {
            move = 255;
            if (Hint.Unlikely((uint)source >= (uint)s.Grid.Length)) return false;

            var range = s.SourceRuns[source];
            CpdRun* runs = (CpdRun*)s.Runs.Ptr;
            for (int i = range.Offset; i < range.Offset + range.Count; i++)
            {
                if (target >= runs[i].TargetMin && target <= runs[i].TargetMax)
                {
                    move = runs[i].FirstMove;
                    return true;
                }
            }
            return false;
        }

        [BurstCompile]
        public static void ExtractPath(ref CpdState s, int source, int target, NativeList<int> path)
        {
            path.Clear();
            int cur = source;
            path.Add(cur);
            int maxSteps = s.Grid.Length;

            while (cur != target && maxSteps-- > 0)
            {
                if (!TryGetFirstMove(ref s, cur, target, out byte move)) break;
                int2 p2 = s.Grid.ToCoord(cur) + Grid2D.Directions4[move];
                if (!s.Grid.InBounds(p2)) break;
                cur = s.Grid.ToIndex(p2);
                path.Add(cur);
            }
        }

        public static void Dispose(ref CpdState s)
        {
            s.Runs.Dispose();
            if (s.SourceRuns.IsCreated) s.SourceRuns.Dispose();
        }
    }
}
