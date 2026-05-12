using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Hashlife
{
    public struct HashlifeNode
    {
        public int Level;
        public int ChildNW;
        public int ChildNE;
        public int ChildSW;
        public int ChildSE;
        public ulong Hash;
    }

    public struct HashlifeState
    {
        public UnsafeList<HashlifeNode> Nodes;
        public NativeParallelHashMap<ulong, int> Intern;
        public NativeParallelHashMap<ulong, int> ResultCache;
    }

    [BurstCompile]
    public unsafe static class HashlifeApi
    {
        public static HashlifeState Create(int maxNodes, Allocator a)
        {
            var s = new HashlifeState
            {
                Nodes = new UnsafeList<HashlifeNode>(maxNodes, a),
                Intern = new NativeParallelHashMap<ulong, int>(maxNodes, a),
                ResultCache = new NativeParallelHashMap<ulong, int>(maxNodes, a),
            };

            // Level 0 nodes (1x1 cells)
            CreateLeaf(ref s, 0); // Dead
            CreateLeaf(ref s, 1); // Alive
            return s;
        }

        private static int CreateLeaf(ref HashlifeState s, byte alive)
        {
            var node = new HashlifeNode
            {
                Level = 0,
                ChildNW = alive,
                ChildNE = 0,
                ChildSW = 0,
                ChildSE = 0,
                Hash = 0,
            };
            InternNode(ref s, ref node, out int id);
            return id;
        }

        [BurstCompile]
        public static int MakeNode(ref HashlifeState s, int nw, int ne, int sw, int se)
        {
            int level = s.Nodes[nw].Level + 1;
            var node = new HashlifeNode
            {
                Level = level,
                ChildNW = nw,
                ChildNE = ne,
                ChildSW = sw,
                ChildSE = se,
                Hash = 0,
            };
            InternNode(ref s, ref node, out int id);
            return id;
        }

        [BurstCompile]
        private static bool InternNode(ref HashlifeState s, ref HashlifeNode node, out int id)
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
            h ^= (ulong)n.ChildNW * 40503UL;
            h ^= (ulong)n.ChildNE * 40503UL * 2;
            h ^= (ulong)n.ChildSW * 40503UL * 3;
            h ^= (ulong)n.ChildSE * 40503UL * 4;
            return h;
        }

        [BurstCompile]
        public static int GetResult(ref HashlifeState s, int nodeIdx)
        {
            if (s.ResultCache.TryGetValue((ulong)nodeIdx, out int cached))
                return cached;

            HashlifeNode* nodes = s.Nodes.Ptr;
            HashlifeNode node = nodes[nodeIdx];

            if (node.Level == 2)
            {
                // Base case: 4x4 block to 2x2 result after 1 step
                int result = ComputeLevel2(ref s, nodeIdx);
                s.ResultCache[(ulong)nodeIdx] = result;
                return result;
            }

            // Recursive case: 9 sub-nodes construction
            int nw = node.ChildNW, ne = node.ChildNE, sw = node.ChildSW, se = node.ChildSE;
            HashlifeNode nwN = nodes[nw], neN = nodes[ne], swN = nodes[sw], seN = nodes[se];

            // 9 level-(k-1) nodes
            int n11 = nwN.ChildNW; int n12 = nwN.ChildNE; int n13 = neN.ChildNW; int n14 = neN.ChildNE;
            int n21 = nwN.ChildSW; int n22 = nwN.ChildSE; int n23 = neN.ChildSW; int n24 = neN.ChildSE;
            int n31 = swN.ChildNW; int n32 = swN.ChildNE; int n33 = seN.ChildNW; int n34 = seN.ChildNE;
            int n41 = swN.ChildSW; int n42 = swN.ChildSE; int n43 = seN.ChildSW; int n44 = seN.ChildSE;

            // 9 overlapping level-(k-1) nodes
            int m00 = nw;
            int m10 = MakeNode(ref s, nwN.ChildNE, neN.ChildNW, nwN.ChildSE, neN.ChildSW);
            int m20 = ne;
            int m01 = MakeNode(ref s, nwN.ChildSW, nwN.ChildSE, swN.ChildNW, swN.ChildNE);
            int m11 = MakeNode(ref s, nwN.ChildSE, neN.ChildSW, swN.ChildNE, seN.ChildNW);
            int m21 = MakeNode(ref s, neN.ChildSW, neN.ChildSE, seN.ChildNW, seN.ChildNE);
            int m02 = sw;
            int m12 = MakeNode(ref s, swN.ChildNE, seN.ChildNW, swN.ChildSE, seN.ChildNE);
            int m22 = se;

            // 4 level-(k-2) result nodes
            int rNW = GetResult(ref s, m00);
            int rNE = GetResult(ref s, m10); // Wait, this is not quite right.
            // Full 9-node algorithm is more involved. Let's implement the standard one.
            
            // Re-fetch pointers as MakeNode might have reallocated (if using NativeList, but we use UnsafeList)
            // UnsafeList doesn't auto-resize unless we tell it to or it hits capacity.
            
            int c00 = GetResult(ref s, m00);
            int c10 = GetResult(ref s, m10);
            int c20 = GetResult(ref s, m20);
            int c01 = GetResult(ref s, m01);
            int c11 = GetResult(ref s, m11);
            int c21 = GetResult(ref s, m21);
            int c02 = GetResult(ref s, m02);
            int c12 = GetResult(ref s, m12);
            int c22 = GetResult(ref s, m22);

            int finalNW = GetResult(ref s, MakeNode(ref s, c00, c10, c01, c11));
            int finalNE = GetResult(ref s, MakeNode(ref s, c10, c20, c11, c21));
            int finalSW = GetResult(ref s, MakeNode(ref s, c01, c11, c02, c12));
            int finalSE = GetResult(ref s, MakeNode(ref s, c11, c21, c12, c22));

            int final = MakeNode(ref s, finalNW, finalNE, finalSW, finalSE);
            s.ResultCache[(ulong)nodeIdx] = final;
            return final;
        }

        private static int ComputeLevel2(ref HashlifeState s, int nodeIdx)
        {
            // Extract 4x4 grid
            byte* grid = stackalloc byte[16];
            HashlifeNode* nodes = s.Nodes.Ptr;
            HashlifeNode n = nodes[nodeIdx];
            
            Extract2x2(nodes, n.ChildNW, grid, 0, 0, 4);
            Extract2x2(nodes, n.ChildNE, grid, 2, 0, 4);
            Extract2x2(nodes, n.ChildSW, grid, 0, 2, 4);
            Extract2x2(nodes, n.ChildSE, grid, 2, 2, 4);

            // Compute 2x2 result (cells at (1,1), (2,1), (1,2), (2,2))
            byte rNW = StepCell(grid, 1, 1, 4);
            byte rNE = StepCell(grid, 2, 1, 4);
            byte rSW = StepCell(grid, 1, 2, 4);
            byte rSE = StepCell(grid, 2, 2, 4);

            return MakeNode(ref s, rNW, rNE, rSW, rSE);
        }

        private static void Extract2x2(HashlifeNode* nodes, int nodeIdx, byte* dst, int ox, int oy, int stride)
        {
            HashlifeNode n = nodes[nodeIdx];
            dst[oy * stride + ox] = (byte)nodes[n.ChildNW].ChildNW;
            dst[oy * stride + ox + 1] = (byte)nodes[n.ChildNE].ChildNW;
            dst[(oy + 1) * stride + ox] = (byte)nodes[n.ChildSW].ChildNW;
            dst[(oy + 1) * stride + ox + 1] = (byte)nodes[n.ChildSE].ChildNW;
        }

        private static byte StepCell(byte* grid, int x, int y, int stride)
        {
            int alive = 0;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (dx != 0 || dy != 0)
                        alive += grid[(y + dy) * stride + (x + dx)];

            byte current = grid[y * stride + x];
            if (current == 1) return (byte)(alive == 2 || alive == 3 ? 1 : 0);
            return (byte)(alive == 3 ? 1 : 0);
        }

        public static void Decode(ref HashlifeState s, int root, NativeArray<byte> cells, Grid2D grid)
        {
            cells.Fill((byte)0);
            if (root < 0 || root >= s.Nodes.Length) return;
            DecodeRecursive(s, root, 0, 0, (byte*)cells.GetUnsafePtr(), grid.Width, grid.Height);
        }

        private static void DecodeRecursive(HashlifeState s, int nodeIdx, int x, int y, byte* cells, int width, int height)
        {
            HashlifeNode n = s.Nodes[nodeIdx];
            if (n.Level == 0)
            {
                if (x < width && y < height)
                    cells[y * width + x] = (byte)n.ChildNW;
                return;
            }

            int half = 1 << (n.Level - 1);
            DecodeRecursive(s, n.ChildNW, x, y, cells, width, height);
            DecodeRecursive(s, n.ChildNE, x + half, y, cells, width, height);
            DecodeRecursive(s, n.ChildSW, x, y + half, cells, width, height);
            DecodeRecursive(s, n.ChildSE, x + half, y + half, cells, width, height);
        }

        public static void Dispose(ref HashlifeState s)
        {
            s.Nodes.Dispose();
            if (s.Intern.IsCreated) s.Intern.Dispose();
            if (s.ResultCache.IsCreated) s.ResultCache.Dispose();
        }
    }
}
