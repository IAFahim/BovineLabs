using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Domino
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DominoState
    {
        public Grid2D Grid;
        public NativeArray<byte> Region;
        public NativeArray<int> Height;
        public NativeArray<byte> MatchingDir;
    }

    [BurstCompile]
    public unsafe static class DominoApi
    {
        public static DominoState Create(int width, int height, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new DominoState
            {
                Grid = g,
                Region = new NativeArray<byte>(g.Length, a),
                Height = new NativeArray<int>(g.Length, a),
                MatchingDir = new NativeArray<byte>(g.Length, a),
            };
        }

        [BurstCompile]
        public static void SetRegion(ref DominoState s, NativeArray<byte> region)
        {
            UnsafeUtility.MemCpy(s.Region.GetUnsafePtr(), region.GetUnsafeReadOnlyPtr(), s.Grid.Length);
        }

        [BurstCompile]
        public static bool CheckTileableByParity(ref DominoState s)
        {
            byte* region = (byte*)s.Region.GetUnsafePtr();
            int w = s.Grid.Width;
            int len = s.Grid.Length;
            int black = 0, white = 0;
            for (int i = 0; i < len; i++)
            {
                if (region[i] == 0) continue;
                int x = i % w;
                int y = i / w;
                if ((x + y) % 2 == 0) black++;
                else white++;
            }
            return black == white;
        }

        [BurstCompile]
        public static bool BuildTilingByMatching(ref DominoState s)
        {
            byte* region = (byte*)s.Region.GetUnsafePtr();
            byte* matchDir = (byte*)s.MatchingDir.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;

            UnsafeUtility.MemSet(matchDir, 0, len);

            var matchTo = new NativeArray<int>(len, Allocator.Temp);
            int* mPtr = (int*)matchTo.GetUnsafePtr();
            for (int i = 0; i < len; i++) mPtr[i] = -1;

            // Greedy matching: black cells to white neighbors
            for (int i = 0; i < len; i++)
            {
                if (region[i] == 0) continue;
                int cx = i % w;
                int cy = i / w;
                if ((cx + cy) % 2 != 0) continue;
                if (mPtr[i] >= 0) continue;

                // Right (0), Up (1), Left (2), Down (3)
                int d = 0;
                // Right
                if (cx + 1 < w)
                {
                    int ni = i + 1;
                    if (region[ni] != 0 && mPtr[ni] < 0)
                    {
                        mPtr[i] = ni; mPtr[ni] = i;
                        matchDir[i] = 1;
                        continue;
                    }
                }
                // Up
                if (cy + 1 < h)
                {
                    int ni = i + w;
                    if (region[ni] != 0 && mPtr[ni] < 0)
                    {
                        mPtr[i] = ni; mPtr[ni] = i;
                        matchDir[i] = 2;
                        continue;
                    }
                }
                // Left
                if (cx > 0)
                {
                    int ni = i - 1;
                    if (region[ni] != 0 && mPtr[ni] < 0)
                    {
                        mPtr[i] = ni; mPtr[ni] = i;
                        matchDir[ni] = 1;
                        continue;
                    }
                }
                // Down
                if (cy > 0)
                {
                    int ni = i - w;
                    if (region[ni] != 0 && mPtr[ni] < 0)
                    {
                        mPtr[i] = ni; mPtr[ni] = i;
                        matchDir[ni] = 2;
                        continue;
                    }
                }
            }

            bool allMatched = true;
            for (int i = 0; i < len; i++)
            {
                if (region[i] == 0) continue;
                if (mPtr[i] < 0) { allMatched = false; break; }
            }
            matchTo.Dispose();
            return allMatched;
        }

        [BurstCompile]
        public static void BuildHeightFunction(ref DominoState s)
        {
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;
            byte* region = (byte*)s.Region.GetUnsafePtr();
            int* height = (int*)s.Height.GetUnsafePtr();

            var vis = new NativeArray<byte>(len, Allocator.Temp);
            byte* vPtr = (byte*)vis.GetUnsafePtr();
            UnsafeUtility.MemSet(vPtr, 0, len);
            UnsafeUtility.MemSet(height, 0, len * 4);

            var queue = new UnsafeQueue<int>(Allocator.Temp);

            if (region[0] == 1) { queue.Enqueue(0); vPtr[0] = 1; }

            while (queue.TryDequeue(out int cell))
            {
                int cx = cell % w;
                int cy = cell / w;

                // Right
                if (cx + 1 < w)
                {
                    int ni = cell + 1;
                    if (vPtr[ni] == 0 && region[ni] != 0)
                    { height[ni] = height[cell] + 1; vPtr[ni] = 1; queue.Enqueue(ni); }
                }
                // Up
                if (cy + 1 < h)
                {
                    int ni = cell + w;
                    if (vPtr[ni] == 0 && region[ni] != 0)
                    { height[ni] = height[cell] + 1; vPtr[ni] = 1; queue.Enqueue(ni); }
                }
                // Left
                if (cx > 0)
                {
                    int ni = cell - 1;
                    if (vPtr[ni] == 0 && region[ni] != 0)
                    { height[ni] = height[cell] - 1; vPtr[ni] = 1; queue.Enqueue(ni); }
                }
                // Down
                if (cy > 0)
                {
                    int ni = cell - w;
                    if (vPtr[ni] == 0 && region[ni] != 0)
                    { height[ni] = height[cell] - 1; vPtr[ni] = 1; queue.Enqueue(ni); }
                }
            }

            vis.Dispose();
            queue.Dispose();
        }

        public static bool FlipAt(ref DominoState s, int cell)
        {
            if (s.MatchingDir[cell] == 0) return false;
            return false;
        }

        public static void Dispose(ref DominoState s)
        {
            if (s.Region.IsCreated) s.Region.Dispose();
            if (s.Height.IsCreated) s.Height.Dispose();
            if (s.MatchingDir.IsCreated) s.MatchingDir.Dispose();
        }
    }
}
