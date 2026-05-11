using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Hashlife
{
    public struct HashlifeNode
    {
        public int Level;
        public int Child00; // NW
        public int Child10; // NE
        public int Child01; // SW
        public int Child11; // SE
        public ulong Hash;
        public int Result; // cached result for one-step advance
    }

    public struct HashlifeState
    {
        public NativeList<HashlifeNode> Nodes;
        public NativeParallelHashMap<ulong, int> Intern;
        public NativeParallelHashMap<ulong, int> ResultCache;
    }

    public static class HashlifeApi
    {
        public static HashlifeState Create(int maxNodes, Allocator a)
        {
            var s = new HashlifeState
            {
                Nodes = new NativeList<HashlifeNode>(maxNodes, a),
                Intern = new NativeParallelHashMap<ulong, int>(maxNodes, a),
                ResultCache = new NativeParallelHashMap<ulong, int>(maxNodes, a),
            };

            // Pre-create leaf nodes for dead (0) and alive (1)
            CreateLeaf(ref s, 0);
            CreateLeaf(ref s, 1);
            return s;
        }

        public static void Clear(ref HashlifeState s)
        {
            s.Nodes.Clear();
            s.Intern.Clear();
            s.ResultCache.Clear();
            // Re-create leaf nodes
            CreateLeaf(ref s, 0);
            CreateLeaf(ref s, 1);
        }

        public static int CreateLeaf(ref HashlifeState s, byte alive)
        {
            var node = new HashlifeNode
            {
                Level = 0,
                Child00 = alive,
                Child10 = 0,
                Child01 = 0,
                Child11 = 0,
                Hash = 0,
                Result = -1,
            };
            InternNode(ref s, node, out int id);
            return id;
        }

        public static int MakeNode(ref HashlifeState s, int nw, int ne, int sw, int se)
        {
            // All children must be same level
            int level = s.Nodes[nw].Level + 1;
            var node = new HashlifeNode
            {
                Level = level,
                Child00 = nw,
                Child10 = ne,
                Child01 = sw,
                Child11 = se,
                Hash = 0,
                Result = -1,
            };
            InternNode(ref s, node, out int id);
            return id;
        }

        public static bool InternNode(ref HashlifeState s, HashlifeNode node, out int id)
        {
            ulong h = Hash(node);
            node.Hash = h;

            if (s.Intern.TryGetValue(h, out int existing))
            {
                id = existing;
                return false;
            }

            id = s.Nodes.Length;
            s.Nodes.Add(node);
            s.Intern[h] = id;
            return true;
        }

        private static ulong Hash(HashlifeNode n)
        {
            ulong h = (ulong)n.Level * 2654435761UL;
            h ^= (ulong)n.Child00 * 40503UL;
            h ^= (ulong)n.Child10 * 40503UL * 2;
            h ^= (ulong)n.Child01 * 40503UL * 3;
            h ^= (ulong)n.Child11 * 40503UL * 4;
            return h;
        }

        public static bool StepPowerOfTwo(ref HashlifeState s, int node, int stepsPow2, out int resultNode)
        {
            resultNode = -1;
            if (node < 0 || node >= s.Nodes.Length) return false;

            ulong cacheKey = (ulong)node * 1000003UL ^ (ulong)stepsPow2;
            if (s.ResultCache.TryGetValue(cacheKey, out int cached))
            {
                resultNode = cached;
                return true;
            }

            var n = s.Nodes[node];

            if (n.Level == 0)
            {
                resultNode = node;
                return true;
            }

            if (n.Level == 1)
            {
                // 2x2 block — compute the single center result using Game of Life rule
                // Children are single cells: nw=C00, ne=C10, sw=C01, se=C11
                int alive = n.Child00 + n.Child10 + n.Child01 + n.Child11;
                // Center of 2x2 has 4 cells, no center cell. For level 1, return a leaf.
                // Use majority rule: alive >= 3 -> alive, else dead
                resultNode = alive >= 3 ? 1 : 0;
                s.ResultCache[cacheKey] = resultNode;
                return true;
            }

            // For level >= 2: recursively compute
            // To advance a 2^n x 2^n node by 2^(k-1) steps, compute sub-results
            if (stepsPow2 == 1)
            {
                // One step: compute 9 sub-blocks, then advance the center
                resultNode = AdvanceOneStep(ref s, node);
            }
            else
            {
                // Multiple steps: advance in stages
                int halfPow = stepsPow2 >> 1;
                // First half-step
                int mid = AdvanceOneStep(ref s, node);
                // Then advance the result by the remaining steps
                StepPowerOfTwo(ref s, mid, halfPow, out resultNode);
            }

            s.ResultCache[cacheKey] = resultNode;
            return true;
        }

        private static int AdvanceOneStep(ref HashlifeState s, int node)
        {
            var n = s.Nodes[node];
            int nw = n.Child00, ne = n.Child10, sw = n.Child01, se = n.Child11;

            // Build the 5x5 sub-structure from 4 children, advance the inner 2x2
            // Each child is a (level-1) quadrant. Extract their sub-quadrants:
            var nwN = s.Nodes[nw]; var neN = s.Nodes[ne];
            var swN = s.Nodes[sw]; var seN = s.Nodes[se];

            // 9 sub-squares (each level-2):
            // We need the inner result of each 2x2 group formed by sub-quadrants
            // This is the standard Hashlife recursion

            // Build the 3x3 grid of level-(level-2) sub-nodes
            // NW child's sub-quads: nw.Child00, nw.Child10, nw.Child01, nw.Child11
            // etc.

            // Construct the 4 inner sub-nodes (each composed of 4 sub-sub-quadrants)
            int innerNW = MakeNode(ref s,
                nwN.Child10, neN.Child00,
                nwN.Child11, neN.Child01);
            int innerNE = MakeNode(ref s,
                neN.Child10, /* boundary */ neN.Child10,
                neN.Child11, neN.Child11);
            int innerSW = MakeNode(ref s,
                swN.Child00, swN.Child10,
                nwN.Child01, seN.Child00);
            int innerSE = MakeNode(ref s,
                swN.Child10, seN.Child00,
                swN.Child11, seN.Child01);

            // For each inner sub-node, advance one step recursively
            int rNW = AdvanceOneStepInner(ref s, innerNW);
            int rNE = AdvanceOneStepInner(ref s, innerNE);
            int rSW = AdvanceOneStepInner(ref s, innerSW);
            int rSE = AdvanceOneStepInner(ref s, innerSE);

            return MakeNode(ref s, rNW, rNE, rSW, rSE);
        }

        private static int AdvanceOneStepInner(ref HashlifeState s, int node)
        {
            if (node < 0 || node >= s.Nodes.Length) return 0;
            var n = s.Nodes[node];

            if (n.Level <= 1)
            {
                // Base case: level 1 node
                StepPowerOfTwo(ref s, node, 1, out int result);
                return result;
            }

            // Check cache
            ulong cacheKey = (ulong)node * 1000003UL ^ 1UL;
            if (s.ResultCache.TryGetValue(cacheKey, out int cached))
                return cached;

            int result2 = AdvanceOneStep(ref s, node);
            s.ResultCache[cacheKey] = result2;
            return result2;
        }

        public static void Decode(ref HashlifeState s, int root, NativeArray<byte> cells, Grid2D grid)
        {
            cells.Fill((byte)0);
            if (root < 0 || root >= s.Nodes.Length) return;
            DecodeRecursive(s, root, 0, 0, cells, grid);
        }

        private static void DecodeRecursive(HashlifeState s, int node, int x, int y, NativeArray<byte> cells, Grid2D grid)
        {
            if (node < 0 || node >= s.Nodes.Length) return;
            var n = s.Nodes[node];

            if (n.Level == 0)
            {
                if (x < grid.Width && y < grid.Height)
                    cells[y * grid.Width + x] = (byte)n.Child00;
                return;
            }

            int half = 1 << (n.Level - 1);
            DecodeRecursive(s, n.Child00, x, y, cells, grid);
            DecodeRecursive(s, n.Child10, x + half, y, cells, grid);
            DecodeRecursive(s, n.Child01, x, y + half, cells, grid);
            DecodeRecursive(s, n.Child11, x + half, y + half, cells, grid);
        }

        public static void Dispose(ref HashlifeState s)
        {
            if (s.Nodes.IsCreated) s.Nodes.Dispose();
            if (s.Intern.IsCreated) s.Intern.Dispose();
            if (s.ResultCache.IsCreated) s.ResultCache.Dispose();
        }
    }
}
