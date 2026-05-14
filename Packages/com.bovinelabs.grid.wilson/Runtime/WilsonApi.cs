using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wilson
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct WilsonState
    {
        public Grid2D Grid;
        public byte* InTree;
        public int* Parent;
        public int* WalkNext;
        public UnsafeList<int> Walk;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class WilsonApi
    {
        public static bool TryCreate(int width, int height, Allocator a, out WilsonState s)
        {
            s = default;
            if (!Grid2D.TryCreate(width, height, out var g)) return false;
            s = new WilsonState
            {
                Grid = g,
                InTree = (byte*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), g.Length),
                Parent = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length),
                WalkNext = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length),
                Walk = new UnsafeList<int>(g.Length, a)
            };
            return true;
        }

        [BurstCompile]
        public static bool TryInitialize(ref WilsonState s, int root)
        {
            var inTree = s.InTree;
            var parent = s.Parent;
            var walkNext = s.WalkNext;
            var len = s.Grid.Length;
            UnsafeUtility.MemSet(inTree, 0, len);
            for (var i = 0; i < len; i++)
            {
                parent[i] = -1;
                walkNext[i] = -1;
            }

            s.Walk.Clear();
            inTree[root] = 1;
            return true;
        }

        [BurstCompile]
        public static bool TryAddRandomWalk(ref WilsonState s, ref Random rng, int start)
        {
            s.Walk.Clear();
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var inTree = s.InTree;
            var walkNext = s.WalkNext;

            var current = start;
            while (inTree[current] == 0)
            {
                var cx = current % w;
                var cy = current / w;
                var count = 0;
                var neighbors = stackalloc int[4];

                if (cx + 1 < w) neighbors[count++] = current + 1;
                if (cy + 1 < h) neighbors[count++] = current + w;
                if (cx > 0) neighbors[count++] = current - 1;
                if (cy > 0) neighbors[count++] = current - w;

                var pick = rng.NextInt(0, count);
                var next = neighbors[pick];

                walkNext[current] = next;
                current = next;
            }

            var parent = s.Parent;
            current = start;
            while (inTree[current] == 0)
            {
                var next = walkNext[current];
                parent[current] = next;
                inTree[current] = 1;
                s.Walk.Add(current);
                current = next;
            }

            for (var i = 0; i < s.Walk.Length; i++)
                walkNext[s.Walk[i]] = -1;
            return true;
        }

        [BurstCompile]
        public static bool TryBuildTree(ref WilsonState s, ref Random rng)
        {
            var inTree = s.InTree;
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++)
                if (inTree[i] == 0)
                    if (!TryAddRandomWalk(ref s, ref rng, i))
                        return false;
            return true;
        }

        [BurstCompile]
        public static bool TryExtractMazeWalls(ref WilsonState s, ref NativeArray<byte> walls)
        {
            var wPtr = (byte*)walls.GetUnsafePtr();
            var parent = s.Parent;
            var len = s.Grid.Length;
            UnsafeUtility.MemSet(wPtr, 1, len);
            for (var i = 0; i < len; i++)
            {
                if (parent[i] < 0) continue;
                wPtr[i] = 0;
            }

            return true;
        }

        public static void Dispose(ref WilsonState s)
        {
            if (s.InTree != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.InTree); s.InTree = null; }
            if (s.Parent != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Parent); s.Parent = null; }
            if (s.WalkNext != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.WalkNext); s.WalkNext = null; }
            s.Walk.Dispose();
        }
    }
}