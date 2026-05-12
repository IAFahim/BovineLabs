using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Wilson
{
    public struct WilsonState
    {
        public Grid2D Grid;
        public NativeArray<byte> InTree;
        public NativeArray<int> Parent;
        public NativeArray<int> WalkNext;
        public NativeList<int> Walk;
    }

    public static class WilsonApi
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
                Walk = new NativeList<int>(g.Length, a),
            };
        }

        public static void Initialize(ref WilsonState s, int root)
        {
            s.InTree.Fill((byte)0);
            s.Parent.Fill(-1);
            s.WalkNext.Fill(-1);
            s.Walk.Clear();
            s.InTree[root] = 1;
        }

        public static void AddRandomWalk(ref WilsonState s, ref Unity.Mathematics.Random rng, int start)
        {
            s.Walk.Clear();

            int current = start;
            while (s.InTree[current] == 0)
            {
                // Random neighbor
                int2 p = s.Grid.ToCoord(current);
                var neighbors = new NativeList<int>(4, Allocator.Temp);
                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (s.Grid.InBounds(np))
                        neighbors.Add(s.Grid.ToIndex(np));
                }

                int next = neighbors[rng.NextInt(0, neighbors.Length)];
                neighbors.Dispose();

                s.WalkNext[current] = next;
                current = next;
            }

            // Trace loop-erased path
            current = start;
            while (s.InTree[current] == 0)
            {
                int next = s.WalkNext[current];
                s.Parent[current] = next;
                s.InTree[current] = 1;
                s.Walk.Add(current);
                current = next;
            }

            // Clear walk markers
            for (int i = 0; i < s.Walk.Length; i++)
                s.WalkNext[s.Walk[i]] = -1;
        }

        public static void BuildTree(ref WilsonState s, ref Unity.Mathematics.Random rng)
        {
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.InTree[i] == 0)
                    AddRandomWalk(ref s, ref rng, i);
            }
        }

        public static void ExtractMazeWalls(ref WilsonState s, NativeArray<byte> walls)
        {
            // walls[i] = 1 means wall present, 0 = passage
            // Initially all walls, remove walls between parent-child
            walls.Fill((byte)1);
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.Parent[i] < 0) continue;
                int2 a = s.Grid.ToCoord(i);
                int2 b = s.Grid.ToCoord(s.Parent[i]);
                // This is a passage - we mark it in some representation
                walls[i] = 0;
            }
        }

        public static void Dispose(ref WilsonState s)
        {
            if (s.InTree.IsCreated) s.InTree.Dispose();
            if (s.Parent.IsCreated) s.Parent.Dispose();
            if (s.WalkNext.IsCreated) s.WalkNext.Dispose();
            if (s.Walk.IsCreated) s.Walk.Dispose();
        }
    }
}
