using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

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

    public unsafe struct FlatHashMap
    {
        public ulong* Keys;
        public int* Values;
        public int Capacity;
        public int Count;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;

        public static bool TryCreate(int capacity, Unity.Collections.AllocatorManager.AllocatorHandle allocator, out FlatHashMap map)
        {
            capacity = math.ceilpow2(capacity * 2);
            map = new FlatHashMap
            {
                Keys = (ulong*)Unity.Collections.AllocatorManager.Allocate(allocator, sizeof(ulong), UnsafeUtility.AlignOf<ulong>(), capacity),
                Values = (int*)Unity.Collections.AllocatorManager.Allocate(allocator, sizeof(int), UnsafeUtility.AlignOf<int>(), capacity),
                Capacity = capacity,
                Count = 0,
                Allocator = allocator
            };
            UnsafeUtility.MemSet(map.Keys, 0xFF, capacity * sizeof(ulong));
            return true;
        }

        public unsafe void Dispose()
        {
            if (Keys != null)
            {
                Unity.Collections.AllocatorManager.Free(Allocator, Keys);
                Keys = null;
            }
            if (Values != null)
            {
                Unity.Collections.AllocatorManager.Free(Allocator, Values);
                Values = null;
            }
        }

        public bool TryGetValue(ulong key, out int value)
        {
            if (Count == 0) { value = 0; return false; }
            uint mask = (uint)(Capacity - 1);
            uint hash = (uint)(key ^ (key >> 32));
            uint idx = hash & mask;

            for (int i = 0; i < Capacity; i++)
            {
                ulong k = Keys[idx];
                if (k == key)
                {
                    value = Values[idx];
                    return true;
                }
                if (k == 0xFFFFFFFFFFFFFFFFUL)
                {
                    break;
                }
                idx = (idx + 1) & mask;
            }
            value = 0;
            return false;
        }

        public unsafe void Add(ulong key, int value)
        {
            uint mask = (uint)(Capacity - 1);
            uint hash = (uint)(key ^ (key >> 32));
            uint idx = hash & mask;

            for (int i = 0; i < Capacity; i++)
            {
                ulong k = Keys[idx];
                if (k == 0xFFFFFFFFFFFFFFFFUL || k == key)
                {
                    if (k == 0xFFFFFFFFFFFFFFFFUL) Count++;
                    Keys[idx] = key;
                    Values[idx] = value;
                    return;
                }
                idx = (idx + 1) & mask;
            }
        }
    }

    public unsafe struct HashlifeState
    {
        public UnsafeList<HashlifeNode> Nodes;
        public FlatHashMap Intern;
        public FlatHashMap ResultCache;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class HashlifeApi
    {
        public static bool TryCreate(int maxNodes, Allocator a, out HashlifeState result)
        {
            FlatHashMap.TryCreate(maxNodes, a, out var internMap);
            FlatHashMap.TryCreate(maxNodes, a, out var cacheMap);

            result = new HashlifeState
            {
                Allocator = a,
                Nodes = new UnsafeList<HashlifeNode>(maxNodes, a),
                Intern = internMap,
                ResultCache = cacheMap
            };

            CreateLeaf(ref result, 0, out _);
            CreateLeaf(ref result, 1, out _);
            return true;
        }

        private static bool CreateLeaf(ref HashlifeState s, byte alive, out int id)
        {
            var node = new HashlifeNode
            {
                Level = 0,
                ChildNW = alive,
                ChildNE = 0,
                ChildSW = 0,
                ChildSE = 0,
                Hash = 0
            };
            return TryInternNode(ref s, ref node, out id);
        }

        [BurstCompile]
        public static bool TryMakeNode(ref HashlifeState s, int nw, int ne, int sw, int se, out int id)
        {
            var level = s.Nodes[nw].Level + 1;
            var node = new HashlifeNode
            {
                Level = level,
                ChildNW = nw,
                ChildNE = ne,
                ChildSW = sw,
                ChildSE = se,
                Hash = 0
            };
            return TryInternNode(ref s, ref node, out id);
        }

        [BurstCompile]
        private static bool TryInternNode(ref HashlifeState s, ref HashlifeNode node, out int id)
        {
            var h = Hash(node);
            node.Hash = h;

            if (s.Intern.TryGetValue(h, out var existing))
            {
                id = existing;
                return true;
            }

            id = s.Nodes.Length;
            s.Nodes.Add(node);
            s.Intern.Add(h, id);
            return true;
        }

        private static ulong Hash(HashlifeNode n)
        {
            var h = (ulong)n.Level * 2654435761UL;
            h ^= (ulong)n.ChildNW * 40503UL;
            h ^= (ulong)n.ChildNE * 40503UL * 2;
            h ^= (ulong)n.ChildSW * 40503UL * 3;
            h ^= (ulong)n.ChildSE * 40503UL * 4;
            return h;
        }

        [BurstCompile]
        public static bool TryGetResult(ref HashlifeState s, int nodeIdx, out int result)
        {
            if (s.ResultCache.TryGetValue((ulong)nodeIdx, out result))
                return true;

            var nodes = s.Nodes.Ptr;
            var node = nodes[nodeIdx];

            if (node.Level == 2)
            {
                if (!ComputeLevel2(ref s, nodeIdx, out result)) return false;
                s.ResultCache.Add((ulong)nodeIdx, result);
                return true;
            }

            HashlifeNode nNW = nodes[node.ChildNW],
                nNE = nodes[node.ChildNE],
                nSW = nodes[node.ChildSW],
                nSE = nodes[node.ChildSE];

            var m00 = node.ChildNW;
            TryMakeNode(ref s, nNW.ChildNE, nNE.ChildNW, nNW.ChildSE, nNE.ChildSW, out var m10);
            var m20 = node.ChildNE;
            TryMakeNode(ref s, nNW.ChildSW, nNW.ChildSE, nSW.ChildNW, nSW.ChildNE, out var m01);
            TryMakeNode(ref s, nNW.ChildSE, nNE.ChildSW, nSW.ChildNE, nSE.ChildNW, out var m11);
            TryMakeNode(ref s, nNE.ChildSW, nNE.ChildSE, nSE.ChildNW, nSE.ChildNE, out var m21);
            var m02 = node.ChildSW;
            TryMakeNode(ref s, nSW.ChildNE, nSE.ChildNW, nSW.ChildSE, nSE.ChildNE, out var m12);
            var m22 = node.ChildSE;

            TryGetResult(ref s, m00, out var c00);
            TryGetResult(ref s, m10, out var c10);
            TryGetResult(ref s, m20, out var c20);
            TryGetResult(ref s, m01, out var c01);
            TryGetResult(ref s, m11, out var c11);
            TryGetResult(ref s, m21, out var c21);
            TryGetResult(ref s, m02, out var c02);
            TryGetResult(ref s, m12, out var c12);
            TryGetResult(ref s, m22, out var c22);

            TryMakeNode(ref s, c00, c10, c01, c11, out var n00);
            TryMakeNode(ref s, c10, c20, c11, c21, out var n10);
            TryMakeNode(ref s, c01, c11, c02, c12, out var n01);
            TryMakeNode(ref s, c11, c21, c12, c22, out var n11);

            TryGetResult(ref s, n00, out var r00);
            TryGetResult(ref s, n10, out var r10);
            TryGetResult(ref s, n01, out var r01);
            TryGetResult(ref s, n11, out var r11);

            TryMakeNode(ref s, r00, r10, r01, r11, out result);

            s.ResultCache.Add((ulong)nodeIdx, result);
            return true;
        }

        private static bool ComputeLevel2(ref HashlifeState s, int nodeIdx, out int result)
        {
            var grid = stackalloc byte[16];
            var nodes = s.Nodes.Ptr;
            var n = nodes[nodeIdx];

            Extract2x2(nodes, n.ChildNW, grid, 0, 0, 4);
            Extract2x2(nodes, n.ChildNE, grid, 2, 0, 4);
            Extract2x2(nodes, n.ChildSW, grid, 0, 2, 4);
            Extract2x2(nodes, n.ChildSE, grid, 2, 2, 4);

            var rNW = StepCell(grid, 1, 1, 4);
            var rNE = StepCell(grid, 2, 1, 4);
            var rSW = StepCell(grid, 1, 2, 4);
            var rSE = StepCell(grid, 2, 2, 4);

            return TryMakeNode(ref s, rNW, rNE, rSW, rSE, out result);
        }

        private static void Extract2x2(HashlifeNode* nodes, int nodeIdx, byte* dst, int ox, int oy, int stride)
        {
            var n = nodes[nodeIdx];
            dst[oy * stride + ox] = (byte)nodes[n.ChildNW].ChildNW;
            dst[oy * stride + ox + 1] = (byte)nodes[n.ChildNE].ChildNW;
            dst[(oy + 1) * stride + ox] = (byte)nodes[n.ChildSW].ChildNW;
            dst[(oy + 1) * stride + ox + 1] = (byte)nodes[n.ChildSE].ChildNW;
        }

        private static byte StepCell(byte* grid, int x, int y, int stride)
        {
            var alive = 0;
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
                if (dx != 0 || dy != 0)
                    alive += grid[(y + dy) * stride + x + dx];

            var current = grid[y * stride + x];
            if (current == 1) return (byte)(alive == 2 || alive == 3 ? 1 : 0);
            return (byte)(alive == 3 ? 1 : 0);
        }

        public static void Decode(ref HashlifeState s, int root, NativeArray<byte> cells, Grid2D grid)
        {
            cells.Fill((byte)0);
            if (root < 0 || root >= s.Nodes.Length) return;
            DecodeRecursive(s, root, 0, 0, (byte*)cells.GetUnsafePtr(), grid.Width, grid.Height);
        }

        private static void DecodeRecursive(HashlifeState s, int nodeIdx, int x, int y, byte* cells, int width,
            int height)
        {
            var n = s.Nodes[nodeIdx];
            if (n.Level == 0)
            {
                if (x < width && y < height)
                    cells[y * width + x] = (byte)n.ChildNW;
                return;
            }

            var half = 1 << (n.Level - 1);
            DecodeRecursive(s, n.ChildNW, x, y, cells, width, height);
            DecodeRecursive(s, n.ChildNE, x + half, y, cells, width, height);
            DecodeRecursive(s, n.ChildSW, x, y + half, cells, width, height);
            DecodeRecursive(s, n.ChildSE, x + half, y + half, cells, width, height);
        }

        public static void Dispose(ref HashlifeState s)
        {
            if (s.Nodes.IsCreated) s.Nodes.Dispose();
            s.Intern.Dispose();
            s.ResultCache.Dispose();
        }
    }
}