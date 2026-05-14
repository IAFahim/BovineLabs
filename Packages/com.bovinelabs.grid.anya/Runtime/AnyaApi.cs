using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Anya
{
    [BurstCompile]
    public static unsafe class AnyaApi
    {
        private const double EPS = 1e-7;

        public static bool TryCreate(int width, int height, int maxNodes, AllocatorManager.AllocatorHandle a,
            out AnyaState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g) ||
                !DoubleMinHeap.TryCreate(maxNodes, a.ToAllocator, out var heap))
            {
                result = default;
                return false;
            }

            var rootCount = (width + 1) * (height + 1);
            result = new AnyaState
            {
                Grid = g,
                Heap = heap,
                Pool = new UnsafeList<AnyaNode>(maxNodes, a.ToAllocator),
                Allocator = a,
                RootGCost = (double*)AllocatorManager.Allocate(a, sizeof(double) * rootCount,
                    UnsafeUtility.AlignOf<double>())
            };
            return true;
        }

        [BurstCompile]
        public static bool TrySearch(
            ref AnyaState s,
            in NativeArray<byte> blocked,
            ref int2 start,
            ref int2 goal,
            ref NativeList<int2> path)
        {
            if (!TryInitSearch(ref s, in blocked, ref start, ref goal))
                return false;

            if (s.SearchComplete != 0)
                return TryExtractPath(ref s, ref path);

            var blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            while (!s.Heap.IsEmpty)
                if (TryStepSearchInternal(ref s, blk))
                    break;

            if (s.BestNode < 0) return false;
            ExtractPath(in s.Pool, s.BestNode, s.Goal, ref path);
            return true;
        }

        [BurstCompile]
        public static bool TryInitSearch(
            ref AnyaState s,
            in NativeArray<byte> blocked,
            ref int2 start,
            ref int2 goal)
        {
            s.Heap.Clear();
            s.Pool.Clear();

            var rootCount = (s.Grid.Width + 1) * (s.Grid.Height + 1);
            for (var i = 0; i < rootCount; i++) s.RootGCost[i] = double.PositiveInfinity;

            s.Start = start;
            s.Goal = goal;
            s.BestNode = -1;
            s.BestCost = double.PositiveInfinity;
            s.SearchComplete = 0;

            if (Hint.Unlikely(!s.Grid.InBounds(start) || !s.Grid.InBounds(goal))) return false;

            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var blk = (byte*)blocked.GetUnsafeReadOnlyPtr();

            if (blk[start.y * w + start.x] != 0 || blk[goal.y * w + goal.x] != 0) return false;

            if (start.Equals(goal))
            {
                s.BestNode = 0;
                s.Pool.Add(new AnyaNode
                {
                    L = start.x, R = start.x, y = start.y, dy = 0,
                    Root = new double2(start.x, start.y), RootG = 0.0, Parent = -1
                });
                s.SearchComplete = 1;
                return true;
            }

            s.RootGCost[start.y * (w + 1) + start.x] = 0.0;

            if (LineOfSight(w, h, blk, start, goal))
            {
                s.BestNode = s.Pool.Length;
                s.Pool.Add(new AnyaNode
                {
                    L = goal.x, R = goal.x, y = goal.y, dy = 0,
                    Root = new double2(start.x, start.y), RootG = 0.0, Parent = -1
                });
                s.SearchComplete = 1;
                return true;
            }

            var lInt = start.x;
            while (lInt > 0 && IsEdgePassable(lInt - 1, start.y, w, h, blk)) lInt--;

            var rInt = start.x;
            while (rInt < w && IsEdgePassable(rInt, start.y, w, h, blk)) rInt++;

            PushNode(ref s, lInt, rInt, start.y, 0, new double2(start.x, start.y), 0.0, -1, goal);
            return true;
        }

        [BurstCompile]
        public static bool TryStepSearch(ref AnyaState s, in NativeArray<byte> blocked)
        {
            var blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            return TryStepSearchInternal(ref s, blk);
        }

        private static bool TryStepSearchInternal(ref AnyaState s, byte* blk)
        {
            if (Hint.Unlikely(s.Heap.IsEmpty || s.SearchComplete != 0))
                return true;

            if (!s.Heap.TryPop(out var top))
            {
                s.SearchComplete = 1;
                return true;
            }

            if (top.Key0 >= s.BestCost)
            {
                s.SearchComplete = 1;
                return true;
            }

            var uIdx = top.Id;
            var u = s.Pool[uIdx];

            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var goal = s.Goal;

            if (u.y == goal.y && goal.x >= u.L - EPS && goal.x <= u.R + EPS)
            {
                var goalD = new double2(goal.x, goal.y);
                var cost = u.RootG + math.distance(u.Root, goalD);
                if (cost < s.BestCost)
                {
                    s.BestCost = cost;
                    s.BestNode = s.Pool.Length;
                    s.Pool.Add(new AnyaNode
                    {
                        L = goal.x, R = goal.x, y = u.y, dy = 0,
                        Root = u.Root, RootG = u.RootG, Parent = uIdx
                    });
                }
            }

            for (var dir = -1; dir <= 1; dir += 2)
            {
                var ny = u.y + dir;
                if (ny < 0 || ny > h) continue;

                var cellY = math.min(u.y, ny);
                if (cellY < 0 || cellY >= h) continue;

                ExpandCorners(ref s, in u, uIdx, dir, cellY, ny, w, h, blk, goal);

                var pL = u.L;
                var pR = u.R;
                if (u.Root.y != u.y)
                {
                    var forwardDir = u.y > u.Root.y ? 1 : -1;
                    if (dir != forwardDir) continue;

                    var ratio = (ny - u.Root.y) / (u.y - u.Root.y);
                    pL = u.Root.x + (u.L - u.Root.x) * ratio;
                    pR = u.Root.x + (u.R - u.Root.x) * ratio;
                }
                else
                {
                    if (u.Root.x <= u.L + EPS)
                    {
                        pR = double.PositiveInfinity;
                    }
                    else if (u.Root.x >= u.R - EPS)
                    {
                        pL = double.NegativeInfinity;
                    }
                    else
                    {
                        pL = double.NegativeInfinity;
                        pR = double.PositiveInfinity;
                    }
                }

                var projStart = math.isinf(pL) ? 0 : math.max(0, (int)math.floor(pL));
                var projEnd = math.isinf(pR) ? w - 1 : math.min(w - 1, (int)math.ceil(pR));

                var x = projStart;
                while (x <= projEnd)
                {
                    if (blk[cellY * w + x] != 0)
                    {
                        x++;
                        continue;
                    }

                    var runEnd = x;
                    while (runEnd + 1 <= projEnd && blk[cellY * w + runEnd + 1] == 0)
                        runEnd++;

                    var oL = math.max(pL, x);
                    var oR = math.min(pR, runEnd + 1.0);
                    if (oL > oR + EPS)
                    {
                        x = runEnd + 1;
                        continue;
                    }

                    if (ny == goal.y && goal.x >= oL - EPS && goal.x <= oR + EPS)
                    {
                        var goalD = new double2(goal.x, goal.y);
                        var cost = u.RootG + math.distance(u.Root, goalD);
                        if (cost < s.BestCost)
                        {
                            s.BestCost = cost;
                            s.BestNode = s.Pool.Length;
                            s.Pool.Add(new AnyaNode
                            {
                                L = goal.x, R = goal.x, y = ny, dy = 0,
                                Root = u.Root, RootG = u.RootG, Parent = uIdx
                            });
                        }
                    }

                    PushNode(ref s, oL, oR, ny, u.dy, u.Root, u.RootG, uIdx, goal);
                    x = runEnd + 1;
                }
            }

            return false;
        }

        [BurstCompile]
        public static bool TryExtractPath(ref AnyaState s, ref NativeList<int2> path)
        {
            path.Clear();
            if (s.BestNode < 0) return false;

            if (s.Pool[s.BestNode].Parent < 0 && s.Start.Equals(s.Goal))
            {
                path.Add(s.Start);
                return true;
            }

            ExtractPath(in s.Pool, s.BestNode, s.Goal, ref path);
            return path.Length > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ExpandCorners(
            ref AnyaState s,
            in AnyaNode u,
            int uIdx,
            int dir,
            int cellY,
            int ny,
            int w,
            int h,
            byte* blk,
            int2 goal)
        {
            var lX = (int)math.round(u.L);
            var rX = (int)math.round(u.R);

            for (var ix = lX; ix <= rX; ix++)
            {
                if (ix < 1 || ix >= w) continue;

                var leftBlocked = blk[cellY * w + (ix - 1)] != 0;
                var rightBlocked = ix < w && blk[cellY * w + ix] != 0;

                if (leftBlocked && !rightBlocked)
                    if (u.Root.x <= ix + EPS)
                        TryAddCornerNode(ref s, u, uIdx, ix, w, h, blk, goal);

                if (!leftBlocked && rightBlocked)
                    if (u.Root.x >= ix - EPS)
                        TryAddCornerNode(ref s, u, uIdx, ix, w, h, blk, goal);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryAddCornerNode(
            ref AnyaState s,
            in AnyaNode u,
            int uIdx,
            int cornerX,
            int w,
            int h,
            byte* blk,
            int2 goal)
        {
            var newRoot = new double2(cornerX, u.y);
            var newG = u.RootG + math.distance(u.Root, newRoot);
            var rootIdx = u.y * (w + 1) + cornerX;

            if (newG < s.RootGCost[rootIdx])
            {
                s.RootGCost[rootIdx] = newG;

                if (goal.y == u.y && LineOfSight(w, h, blk, new int2(cornerX, u.y), goal))
                {
                    var cost = newG + math.distance(newRoot, new double2(goal.x, goal.y));
                    if (cost < s.BestCost)
                    {
                        s.BestCost = cost;
                        s.BestNode = s.Pool.Length;
                        s.Pool.Add(new AnyaNode
                        {
                            L = goal.x, R = goal.x, y = goal.y, dy = 0,
                            Root = newRoot, RootG = newG, Parent = uIdx
                        });
                    }
                }

                var fL = cornerX;
                while (fL > 0 && IsEdgePassable(fL - 1, u.y, w, h, blk)) fL--;

                var fR = cornerX;
                while (fR < w && IsEdgePassable(fR, u.y, w, h, blk)) fR++;

                if (fR - fL > 0)
                {
                    if (u.y + 1 <= h)
                        PushNode(ref s, fL, fR, u.y + 1, 1, newRoot, newG, uIdx, goal);
                    if (u.y - 1 >= 0)
                        PushNode(ref s, fL, fR, u.y - 1, -1, newRoot, newG, uIdx, goal);
                }

                if (cornerX < w && !IsEdgePassable(cornerX, u.y, w, h, blk))
                {
                    var fR2 = cornerX + 1;
                    while (fR2 < w && IsEdgePassable(fR2, u.y, w, h, blk)) fR2++;

                    if (fR2 > cornerX + 1)
                    {
                        if (u.y + 1 <= h)
                            PushNode(ref s, cornerX + 1, fR2, u.y + 1, 1, newRoot, newG, uIdx, goal);
                        if (u.y - 1 >= 0)
                            PushNode(ref s, cornerX + 1, fR2, u.y - 1, -1, newRoot, newG, uIdx, goal);
                    }
                }

                if (cornerX > 0 && !IsEdgePassable(cornerX - 1, u.y, w, h, blk))
                {
                    var fL2 = cornerX - 1;
                    while (fL2 > 0 && IsEdgePassable(fL2 - 1, u.y, w, h, blk)) fL2--;

                    if (cornerX - 1 > fL2)
                    {
                        if (u.y + 1 <= h)
                            PushNode(ref s, fL2, cornerX - 1, u.y + 1, 1, newRoot, newG, uIdx, goal);
                        if (u.y - 1 >= 0)
                            PushNode(ref s, fL2, cornerX - 1, u.y - 1, -1, newRoot, newG, uIdx, goal);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NodeEquals(in AnyaNode a, double L, double R, int y, double2 root)
        {
            return a.y == y
                   && math.abs(a.L - L) < EPS
                   && math.abs(a.R - R) < EPS
                   && math.abs(a.Root.x - root.x) < EPS
                   && math.abs(a.Root.y - root.y) < EPS;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PushNode(ref AnyaState s, double L, double R, int y, int dy, double2 root, double rootG,
            int parent, int2 goal)
        {
            if (R - L <= EPS) return;

            for (var i = 0; i < s.Pool.Length; i++)
                if (NodeEquals(s.Pool[i], L, R, y, root))
                {
                    if (s.Pool[i].RootG <= rootG + EPS) return;
                    s.Pool[i] = new AnyaNode
                    {
                        L = L, R = R, y = y, dy = dy, Root = root, RootG = rootG, Parent = parent
                    };
                    double xInt = goal.x;
                    if (goal.y != root.y)
                    {
                        var ratio = (y - root.y) / (goal.y - root.y);
                        xInt = root.x + (goal.x - root.x) * ratio;
                    }

                    var xOpt = math.max(L, math.min(R, xInt));
                    var f = rootG + math.distance(root, new double2(xOpt, y)) +
                            math.distance(new double2(xOpt, y), new double2(goal.x, goal.y));
                    s.Heap.TryInsertOrDecrease(new DoubleHeapNode(i, f));
                    return;
                }

            double xInt2 = goal.x;
            if (goal.y != root.y)
            {
                var ratio = (y - root.y) / (goal.y - root.y);
                xInt2 = root.x + (goal.x - root.x) * ratio;
            }

            var xOpt2 = math.max(L, math.min(R, xInt2));
            var f2 = rootG + math.distance(root, new double2(xOpt2, y)) +
                     math.distance(new double2(xOpt2, y), new double2(goal.x, goal.y));

            var idx = s.Pool.Length;
            s.Pool.Add(new AnyaNode
            {
                L = L,
                R = R,
                y = y,
                dy = dy,
                Root = root,
                RootG = rootG,
                Parent = parent
            });
            s.Heap.TryInsertOrDecrease(new DoubleHeapNode(idx, f2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEdgePassable(int x, int y, int w, int h, byte* blk)
        {
            var below = x >= 0 && x < w && y >= 0 && y < h && blk[y * w + x] == 0;
            var above = x >= 0 && x < w && y - 1 >= 0 && y - 1 < h && blk[(y - 1) * w + x] == 0;
            return below || above;
        }

        [BurstCompile]
        public static bool LineOfSight(int w, int h, [NoAlias] byte* blk, int2 from, int2 to)
        {
            if (from.x == to.x && from.y == to.y)
                return from.x >= 0 && from.y >= 0 && from.x < w && from.y < h && blk[from.y * w + from.x] == 0;

            double dx = to.x - from.x;
            double dy = to.y - from.y;
            var absDx = math.abs(dx);
            var absDy = math.abs(dy);

            var stepX = dx > 0 ? 1 : dx < 0 ? -1 : 0;
            var stepY = dy > 0 ? 1 : dy < 0 ? -1 : 0;

            double tMaxX, tMaxY;
            var tDeltaX = absDx > 1e-12 ? 1.0 / absDx : double.PositiveInfinity;
            var tDeltaY = absDy > 1e-12 ? 1.0 / absDy : double.PositiveInfinity;

            if (stepX > 0) tMaxX = (math.floor(from.x) + 1.0 - from.x) * tDeltaX;
            else if (stepX < 0) tMaxX = (from.x - math.floor(from.x)) * tDeltaX;
            else tMaxX = double.PositiveInfinity;

            if (stepY > 0) tMaxY = (math.floor(from.y) + 1.0 - from.y) * tDeltaY;
            else if (stepY < 0) tMaxY = (from.y - math.floor(from.y)) * tDeltaY;
            else tMaxY = double.PositiveInfinity;

            var x = from.x;
            var y = from.y;

            if (x < 0 || y < 0 || x >= w || y >= h) return false;
            if (blk[y * w + x] != 0) return false;

            while (true)
            {
                if (x == to.x && y == to.y) return true;

                if (tMaxX < tMaxY)
                {
                    if (tMaxX > 1.0)
                    {
                        x = to.x;
                        y = to.y;
                    }
                    else
                    {
                        x += stepX;
                        tMaxX += tDeltaX;
                    }
                }
                else
                {
                    if (tMaxY > 1.0)
                    {
                        x = to.x;
                        y = to.y;
                    }
                    else
                    {
                        y += stepY;
                        tMaxY += tDeltaY;
                    }
                }

                if (x < 0 || y < 0 || x >= w || y >= h) return false;
                if (blk[y * w + x] != 0) return false;
            }
        }

        private static void ExtractPath(in UnsafeList<AnyaNode> pool, int nodeIdx, int2 goal, ref NativeList<int2> path)
        {
            path.Add(goal);
            var cur = nodeIdx;
            var lastRoot = new double2(-1, -1);
            while (cur >= 0)
            {
                var node = pool[cur];
                if (math.distance(node.Root, lastRoot) > EPS)
                {
                    var pt = new int2((int)math.round(node.Root.x), (int)math.round(node.Root.y));
                    if (path.Length == 0 || !path[path.Length - 1].Equals(pt)) path.Add(pt);
                    lastRoot = node.Root;
                }

                cur = node.Parent;
            }

            for (int i = 0, j = path.Length - 1; i < j; i++, j--) (path[i], path[j]) = (path[j], path[i]);
        }

        public static void Dispose(ref AnyaState s)
        {
            s.Dispose();
        }
    }
}