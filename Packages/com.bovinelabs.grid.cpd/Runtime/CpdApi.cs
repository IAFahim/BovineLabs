using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Cpd
{
    public struct CpdRun { public int Source; public int TargetMin; public int TargetMax; public byte FirstMove; }

    public struct CpdState
    {
        public Grid2D Grid;
        public NativeList<CpdRun> Runs;
        public NativeArray<RangeI> SourceRuns;
    }

    public static class CpdApi
    {
        public static CpdState Create(int width, int height, int maxRuns, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new CpdState
            {
                Grid = g,
                Runs = new NativeList<CpdRun>(maxRuns, a),
                SourceRuns = new NativeArray<RangeI>(g.Length, a),
            };
        }

        public static void Build(ref CpdState s, NativeArray<byte> blocked)
        {
            s.Runs.Clear();
            var firstMove = new NativeArray<byte>(s.Grid.Length, Allocator.Temp);

            for (int source = 0; source < s.Grid.Length; source++)
            {
                if (blocked[source] != 0)
                {
                    s.SourceRuns[source] = new RangeI(0, 0);
                    continue;
                }

                int runStart = s.Runs.Length;

                // BFS from source to find first move for every target
                var dist = new NativeArray<float>(s.Grid.Length, Allocator.Temp);
                var parent = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
                dist.Fill(float.PositiveInfinity);
                parent.Fill(-1);
                dist[source] = 0f;

                var queue = new NativeQueue<int>(Allocator.Temp);
                queue.Enqueue(source);

                while (queue.TryDequeue(out int u))
                {
                    int2 up = s.Grid.ToCoord(u);
                    for (int d = 0; d < 4; d++)
                    {
                        int2 np = up + Grid2D.Directions4[d];
                        if (!s.Grid.InBounds(np)) continue;
                        int ni = s.Grid.ToIndex(np);
                        if (blocked[ni] != 0 || dist[ni] <= dist[u] + 1f) continue;
                        dist[ni] = dist[u] + 1f;
                        parent[ni] = u;
                        queue.Enqueue(ni);
                    }
                }

                // Compute first move for each reachable target
                firstMove.Fill((byte)255);
                for (int target = 0; target < s.Grid.Length; target++)
                {
                    if (target == source || float.IsPositiveInfinity(dist[target])) continue;
                    // Trace back to find first move
                    int cur = target;
                    while (parent[cur] != source && parent[cur] >= 0)
                        cur = parent[cur];
                    if (parent[cur] == source)
                    {
                        int2 diff = s.Grid.ToCoord(cur) - s.Grid.ToCoord(source);
                        for (int d = 0; d < 4; d++)
                        {
                            if (Grid2D.Directions4[d].Equals(diff))
                            { firstMove[target] = (byte)d; break; }
                        }
                    }
                }

                // Compress into runs
                byte currentMove = 255;
                int runTargetMin = -1;
                for (int target = 0; target < s.Grid.Length; target++)
                {
                    if (firstMove[target] == 255) continue;
                    if (firstMove[target] != currentMove)
                    {
                        if (currentMove != 255)
                            s.Runs.Add(new CpdRun { Source = source, TargetMin = runTargetMin, TargetMax = target - 1, FirstMove = currentMove });
                        currentMove = firstMove[target];
                        runTargetMin = target;
                    }
                }
                if (currentMove != 255)
                    s.Runs.Add(new CpdRun { Source = source, TargetMin = runTargetMin, TargetMax = s.Grid.Length - 1, FirstMove = currentMove });

                s.SourceRuns[source] = new RangeI(runStart, s.Runs.Length - runStart);

                dist.Dispose();
                parent.Dispose();
                queue.Dispose();
            }

            firstMove.Dispose();
        }

        public static bool TryGetFirstMove(ref CpdState s, int source, int target, out byte move)
        {
            move = 255;
            if (source < 0 || source >= s.Grid.Length) return false;

            var range = s.SourceRuns[source];
            for (int i = range.Offset; i < range.Offset + range.Count; i++)
            {
                var run = s.Runs[i];
                if (target >= run.TargetMin && target <= run.TargetMax)
                {
                    move = run.FirstMove;
                    return true;
                }
            }
            return false;
        }

        public static void ExtractPath(ref CpdState s, int source, int target, NativeList<int> path)
        {
            path.Clear();
            int cur = source;
            path.Add(cur);
            int maxSteps = s.Grid.Length;

            while (cur != target && maxSteps-- > 0)
            {
                if (!TryGetFirstMove(ref s, cur, target, out byte move)) break;
                int2 p = s.Grid.ToCoord(cur) + Grid2D.Directions4[move];
                if (!s.Grid.InBounds(p)) break;
                cur = s.Grid.ToIndex(p);
                path.Add(cur);
            }
        }

        public static void Dispose(ref CpdState s)
        {
            if (s.Runs.IsCreated) s.Runs.Dispose();
            if (s.SourceRuns.IsCreated) s.SourceRuns.Dispose();
        }
    }
}
