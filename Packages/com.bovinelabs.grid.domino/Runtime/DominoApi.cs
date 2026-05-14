using System.Runtime.InteropServices;
using BovineLabs.Grid.GraphCut;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BovineLabs.Grid.Domino
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DominoState
    {
        public Grid2D Grid;
        public byte* Region;
        public int* Height;
        public byte* MatchingDir;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class DominoApi
    {
        public static bool TryCreate(int width, int height, Allocator a, out DominoState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new DominoState
            {
                Allocator = a,
                Grid = g,
                Region = (byte*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), g.Length),
                Height = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length),
                MatchingDir = (byte*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), g.Length)
            };
            return true;
        }

        [BurstCompile]
        public static void SetRegion(ref DominoState s, in NativeArray<byte> region)
        {
            UnsafeUtility.MemCpy(s.Region, region.GetUnsafeReadOnlyPtr(), s.Grid.Length);
        }

        [BurstCompile]
        public static bool CheckTileableByParity(ref DominoState s)
        {
            var region = s.Region;
            var w = s.Grid.Width;
            var len = s.Grid.Length;
            int black = 0, white = 0;
            for (var i = 0; i < len; i++)
            {
                if (region[i] == 0) continue;
                var x = i % w;
                var y = i / w;
                if ((x + y) % 2 == 0) black++;
                else white++;
            }

            return black == white;
        }

        [BurstCompile]
        public static bool TryBuildTilingByMatching(ref DominoState s)
        {
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var len = s.Grid.Length;
            var region = s.Region;
            var matchDir = s.MatchingDir;
            UnsafeUtility.MemSet(matchDir, 0, len);

            if (!GraphCutApi.TryCreate(w, h, len * 10, Allocator.Temp, out var cut)) return false;

            cut.EdgeTo.Clear();
            cut.EdgeCap.Clear();
            cut.EdgeFlow.Clear();
            cut.EdgeRev.Clear();
            cut.EdgeNext.Clear();

            var sourceIdx = len;
            var sinkIdx = len + 1;
            int blackCount = 0, whiteCount = 0;

            for (var i = 0; i < len; i++)
            {
                if (region[i] == 0) continue;
                var x = i % w;
                var y = i / w;
                if ((x + y) % 2 == 0) blackCount++;
                else whiteCount++;
            }

            if (blackCount != whiteCount)
            {
                GraphCutApi.Dispose(ref cut);
                return false;
            }

            for (var i = 0; i < len + 2; i++) cut.EdgeHead[i] = -1;

            for (var i = 0; i < len; i++)
            {
                if (region[i] == 0) continue;
                var x = i % w;
                var y = i / w;

                if ((x + y) % 2 == 0)
                {
                    GraphCutApi.AddEdgeInternal(ref cut, sourceIdx, i, 1);
                    if (x + 1 < w && region[i + 1] != 0)
                        GraphCutApi.AddEdgeInternal(ref cut, i, i + 1, 1);
                    if (x > 0 && region[i - 1] != 0)
                        GraphCutApi.AddEdgeInternal(ref cut, i, i - 1, 1);
                    if (y + 1 < h && region[i + w] != 0)
                        GraphCutApi.AddEdgeInternal(ref cut, i, i + w, 1);
                    if (y > 0 && region[i - w] != 0)
                        GraphCutApi.AddEdgeInternal(ref cut, i, i - w, 1);
                }
                else
                {
                    GraphCutApi.AddEdgeInternal(ref cut, i, sinkIdx, 1);
                }
            }

            if (!GraphCutApi.TryMinCut(ref cut))
            {
                GraphCutApi.Dispose(ref cut);
                return false;
            }

            var head = cut.EdgeHead;
            var to = cut.EdgeTo.Ptr;
            var flow = cut.EdgeFlow.Ptr;
            var next = cut.EdgeNext.Ptr;

            var matchedCount = 0;
            for (var e = head[sourceIdx]; e != -1; e = next[e])
                if (flow[e] > 0)
                {
                    var u = to[e];
                    for (var ee = head[u]; ee != -1; ee = next[ee])
                        if (flow[ee] > 0 && to[ee] < len)
                        {
                            var v = to[ee];
                            matchedCount++;
                            var diff = v - u;
                            if (diff == 1) matchDir[u] = 1;
                            else if (diff == w) matchDir[u] = 2;
                            else if (diff == -1) matchDir[v] = 1;
                            else if (diff == -w) matchDir[v] = 2;
                        }
                }

            GraphCutApi.Dispose(ref cut);
            return matchedCount == blackCount;
        }

        [BurstCompile]
        public static bool TryBuildHeightFunction(ref DominoState s)
        {
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var len = s.Grid.Length;
            var region = s.Region;
            var height = s.Height;

            var vis = new Unity.Collections.NativeArray<byte>(len, Allocator.Temp);
            var vPtr = (byte*)vis.GetUnsafePtr();
            UnsafeUtility.MemSet(vPtr, 0, len);
            UnsafeUtility.MemSet(height, 0, len * 4);

            var queue = new UnsafeQueue<int>(Allocator.Temp);

            for (var i = 0; i < len; i++)
                if (region[i] == 1)
                {
                    queue.Enqueue(i);
                    vPtr[i] = 1;
                    break;
                }

            while (queue.TryDequeue(out var cell))
            {
                var cx = cell % w;
                var cy = cell / w;

                if (cx + 1 < w)
                {
                    var ni = cell + 1;
                    if (vPtr[ni] == 0 && region[ni] != 0)
                    {
                        height[ni] = height[cell] + 1;
                        vPtr[ni] = 1;
                        queue.Enqueue(ni);
                    }
                }

                if (cy + 1 < h)
                {
                    var ni = cell + w;
                    if (vPtr[ni] == 0 && region[ni] != 0)
                    {
                        height[ni] = height[cell] + 1;
                        vPtr[ni] = 1;
                        queue.Enqueue(ni);
                    }
                }

                if (cx > 0)
                {
                    var ni = cell - 1;
                    if (vPtr[ni] == 0 && region[ni] != 0)
                    {
                        height[ni] = height[cell] - 1;
                        vPtr[ni] = 1;
                        queue.Enqueue(ni);
                    }
                }

                if (cy > 0)
                {
                    var ni = cell - w;
                    if (vPtr[ni] == 0 && region[ni] != 0)
                    {
                        height[ni] = height[cell] - 1;
                        vPtr[ni] = 1;
                        queue.Enqueue(ni);
                    }
                }
            }

            vis.Dispose();
            queue.Dispose();
            return true;
        }

        public static bool TryFlipAt(ref DominoState s, int cell)
        {
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            if (cell < 0 || cell >= s.Grid.Length || s.MatchingDir[cell] == 0) return false;

            var cx = cell % w;
            var cy = cell / w;

            if (s.MatchingDir[cell] == 1)
            {
                if (cy + 1 >= h || cx + 1 >= w) return false;
                var n1 = cell + 1;
                var n2 = cell + w;
                var n3 = cell + w + 1;
                if (s.MatchingDir[n2] == 1 && s.MatchingDir[n1] == 0 && s.MatchingDir[n3] == 0)
                {
                    s.MatchingDir[cell] = 2;
                    s.MatchingDir[n1] = 2;
                    s.MatchingDir[n2] = 0;
                    return true;
                }
            }
            else if (s.MatchingDir[cell] == 2)
            {
                if (cy + 1 >= h || cx + 1 >= w) return false;
                var n1 = cell + 1;
                var n2 = cell + w;
                var n3 = cell + w + 1;
                if (s.MatchingDir[n1] == 2 && s.MatchingDir[n2] == 0 && s.MatchingDir[n3] == 0)
                {
                    s.MatchingDir[cell] = 1;
                    s.MatchingDir[n2] = 1;
                    s.MatchingDir[n1] = 0;
                    return true;
                }
            }

            return false;
        }

        public static void Dispose(ref DominoState s)
        {
            if (s.Region != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Region); s.Region = null; }
            if (s.Height != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Height); s.Height = null; }
            if (s.MatchingDir != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.MatchingDir); s.MatchingDir = null; }
        }
    }
}