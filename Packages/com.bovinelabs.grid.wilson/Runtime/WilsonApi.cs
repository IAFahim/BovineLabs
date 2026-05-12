using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Wilson
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WilsonState
    {
        public Grid2D Grid;
        public NativeArray<byte> InTree;
        public NativeArray<int> Parent;
        public NativeArray<int> WalkNext;
        public UnsafeList<int> Walk;
    }

    [BurstCompile]
    public unsafe static class WilsonApi
    {
        public static WilsonState Create(int width, int height, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new WilsonState
            {
                Grid = g,
                InTree = new NativeArray<byte>(g.Length, a),
                Parent = new NativeArray<int>(g.Length, a),
                WalkNext = new NativeArray<int>(g.Length, a),
                Walk = new UnsafeList<int>(g.Length, a),
            };
        }

        [BurstCompile]
        public static void Initialize(ref WilsonState s, int root)
        {
            byte* inTree = (byte*)s.InTree.GetUnsafePtr();
            int* parent = (int*)s.Parent.GetUnsafePtr();
            int* walkNext = (int*)s.WalkNext.GetUnsafePtr();
            int len = s.Grid.Length;
            UnsafeUtility.MemSet(inTree, 0, len);
            for (int i = 0; i < len; i++) { parent[i] = -1; walkNext[i] = -1; }
            s.Walk.Clear();
            inTree[root] = 1;
        }

        [BurstCompile]
        public static void AddRandomWalk(ref WilsonState s, ref Unity.Mathematics.Random rng, int start)
        {
            s.Walk.Clear();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            byte* inTree = (byte*)s.InTree.GetUnsafePtr();
            int* walkNext = (int*)s.WalkNext.GetUnsafePtr();

            int current = start;
            while (inTree[current] == 0)
            {
                int cx = current % w;
                int cy = current / w;
                int count = 0;
                int neighbors0 = -1, neighbors1 = -1, neighbors2 = -1, neighbors3 = -1;

                if (cx + 1 < w) { neighbors0 = current + 1; count = 1; }
                if (cy + 1 < h) { int n = current + w; if (count == 0) neighbors0 = n; else neighbors1 = n; count++; }
                if (cx > 0) { int n = current - 1; if (count <= 1) { if (count == 0) neighbors0 = n; else neighbors1 = n; } else neighbors2 = n; count++; }
                if (cy > 0) { int n = current - w; if (count <= 1) { if (count == 0) neighbors0 = n; else neighbors1 = n; } else if (count == 2) neighbors2 = n; else neighbors3 = n; count++; }

                int pick = rng.NextInt(0, count);
                int next = pick == 0 ? neighbors0 : pick == 1 ? neighbors1 : pick == 2 ? neighbors2 : neighbors3;

                walkNext[current] = next;
                current = next;
            }

            // Trace loop-erased path
            int* parent = (int*)s.Parent.GetUnsafePtr();
            current = start;
            while (inTree[current] == 0)
            {
                int next = walkNext[current];
                parent[current] = next;
                inTree[current] = 1;
                s.Walk.Add(current);
                current = next;
            }

            for (int i = 0; i < s.Walk.Length; i++)
                walkNext[s.Walk[i]] = -1;
        }

        [BurstCompile]
        public static void BuildTree(ref WilsonState s, ref Unity.Mathematics.Random rng)
        {
            byte* inTree = (byte*)s.InTree.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++)
            {
                if (inTree[i] == 0)
                    AddRandomWalk(ref s, ref rng, i);
            }
        }

        [BurstCompile]
        public static void ExtractMazeWalls(ref WilsonState s, NativeArray<byte> walls)
        {
            byte* wPtr = (byte*)walls.GetUnsafePtr();
            int* parent = (int*)s.Parent.GetUnsafePtr();
            int len = s.Grid.Length;
            UnsafeUtility.MemSet(wPtr, 1, len);
            for (int i = 0; i < len; i++)
            {
                if (parent[i] < 0) continue;
                wPtr[i] = 0;
            }
        }

        public static void Dispose(ref WilsonState s)
        {
            if (s.InTree.IsCreated) s.InTree.Dispose();
            if (s.Parent.IsCreated) s.Parent.Dispose();
            if (s.WalkNext.IsCreated) s.WalkNext.Dispose();
            s.Walk.Dispose();
        }
    }
}
