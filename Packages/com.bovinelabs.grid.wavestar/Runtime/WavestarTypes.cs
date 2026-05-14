using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wavestar
{
    public struct OctreeIndex : IEquatable<OctreeIndex>
    {
        public int x;
        public int y;
        public int z;
        public int height;

        public OctreeIndex(int x, int y, int z, int height)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.height = height;
        }


        public int Size => 1 << height;


        public float3 Center => new(
            x * Size + Size * 0.5f,
            y * Size + Size * 0.5f,
            z * Size + Size * 0.5f);


        public int3 MinCorner => new(x * Size, y * Size, z * Size);


        public int3 MaxCornerExclusive => new(
            (x + 1) * Size,
            (y + 1) * Size,
            (z + 1) * Size);


        public bool Contains(int3 point)
        {
            var s = Size;
            return point.x >= x * s && point.x < (x + 1) * s &&
                   point.y >= y * s && point.y < (y + 1) * s &&
                   point.z >= z * s && point.z < (z + 1) * s;
        }


        public bool Contains(float3 point)
        {
            float s = Size;
            float minX = x * s, maxX = (x + 1) * s;
            float minY = y * s, maxY = (y + 1) * s;
            float minZ = z * s, maxZ = (z + 1) * s;
            return point.x >= minX && point.x <= maxX &&
                   point.y >= minY && point.y <= maxY &&
                   point.z >= minZ && point.z <= maxZ;
        }


        public OctreeIndex Child(int childIndex)
        {
            var cx = childIndex & 1;
            var cy = (childIndex >> 1) & 1;
            var cz = (childIndex >> 2) & 1;
            return new OctreeIndex(x * 2 + cx, y * 2 + cy, z * 2 + cz, height - 1);
        }


        public OctreeIndex Parent => new(x >> 1, y >> 1, z >> 1, height + 1);


        public int MortonCode
        {
            get
            {
                uint spread(uint v)
                {
                    v = (v | (v << 16)) & 0x030000FF;
                    v = (v | (v << 8)) & 0x0300F00F;
                    v = (v | (v << 4)) & 0x030C30C3;
                    v = (v | (v << 2)) & 0x09249249;
                    return v;
                }

                var mx = spread((uint)x);
                var my = spread((uint)y);
                var mz = spread((uint)z);
                var morton = mx | (my << 1) | (mz << 2);

                return (int)(morton | ((uint)height << 24));
            }
        }

        public bool Equals(OctreeIndex other)
        {
            return x == other.x && y == other.y && z == other.z && height == other.height;
        }

        public override bool Equals(object obj)
        {
            return obj is OctreeIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return MortonCode;
        }

        public override string ToString()
        {
            return $"OctreeIndex({x}, {y}, {z}, h={height})";
        }

        public static bool operator ==(OctreeIndex a, OctreeIndex b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(OctreeIndex a, OctreeIndex b)
        {
            return !a.Equals(b);
        }
    }


    public struct SubvolumeData
    {
        public int predecessorX;
        public int predecessorY;
        public int predecessorZ;
        public float gCost;

        public SubvolumeData(int predX, int predY, int predZ, float gCost)
        {
            predecessorX = predX;
            predecessorY = predY;
            predecessorZ = predZ;
            this.gCost = gCost;
        }


        public int3 Predecessor => new(predecessorX, predecessorY, predecessorZ);


        public float3 PredecessorCenter => new(predecessorX, predecessorY, predecessorZ);

        public static SubvolumeData Invalid => new(0, 0, 0, float.PositiveInfinity);
    }


    public enum ComparisonResult : byte
    {
        StrictlyBetter,
        Ambiguous,
        NotBetter
    }


    public struct MultiResCostField : IDisposable
    {
        private NativeHashMap<int, SubvolumeData> data;

        public MultiResCostField(int capacity, Allocator allocator)
        {
            data = new NativeHashMap<int, SubvolumeData>(capacity, allocator);
        }

        public int Count => data.Count;

        public bool TryGetValue(OctreeIndex idx, out SubvolumeData subvolData)
        {
            return data.TryGetValue(idx.MortonCode, out subvolData);
        }

        public unsafe void Set(OctreeIndex idx, SubvolumeData sv)
        {
            data[idx.MortonCode] = sv;
        }

        public bool Contains(OctreeIndex idx)
        {
            return data.ContainsKey(idx.MortonCode);
        }

        public unsafe void Remove(OctreeIndex idx)
        {
            data.Remove(idx.MortonCode);
        }


        public NativeHashMap<int, SubvolumeData> RawData => data;

        public NativeArray<int> GetKeyArray(Allocator allocator)
        {
            return data.GetKeyArray(allocator);
        }

        public NativeArray<SubvolumeData> GetValueArray(Allocator allocator)
        {
            return data.GetValueArray(allocator);
        }

        public unsafe void Clear()
        {
            data.Clear();
        }

        public unsafe void Dispose()
        {
            if (data.IsCreated)
                data.Dispose();
        }

        public bool IsCreated => data.IsCreated;
    }


    public interface IObstacleMap
    {
        int SizeX { get; }
        int SizeY { get; }
        int SizeZ { get; }


        bool IsTraversable(int x, int y, int z);


        bool IsSubvolumeTraversable(OctreeIndex idx);
    }


    [BurstCompile]
    public struct NativeObstacleMap : IObstacleMap
    {
        private NativeArray<int> grid;


        public const int BlockedCellState = 1;

        public NativeObstacleMap(NativeArray<int> grid, int sizeX, int sizeY, int sizeZ)
        {
            this.grid = grid;
            this.SizeX = sizeX;
            this.SizeY = sizeY;
            this.SizeZ = sizeZ;
        }

        public int SizeX { get; }

        public int SizeY { get; }

        public int SizeZ { get; }

        public bool IsTraversable(int x, int y, int z)
        {
            if (x < 0 || x >= SizeX || y < 0 || y >= SizeY || z < 0 || z >= SizeZ)
                return false;
            var idx = x + y * SizeX + z * SizeX * SizeY;
            return grid[idx] != BlockedCellState;
        }

        public bool IsSubvolumeTraversable(OctreeIndex sv)
        {
            var s = sv.Size;
            var minX = sv.x * s;
            var minY = sv.y * s;
            var minZ = sv.z * s;
            var maxX = math.min(minX + s, SizeX);
            var maxY = math.min(minY + s, SizeY);
            var maxZ = math.min(minZ + s, SizeZ);

            for (var zz = minZ; zz < maxZ; zz++)
            for (var yy = minY; yy < maxY; yy++)
            for (var xx = minX; xx < maxX; xx++)
                if (!IsTraversable(xx, yy, zz))
                    return false;

            return true;
        }
    }


    public struct OpenSetElement : IComparable<OpenSetElement>
    {
        public OctreeIndex index;
        public float fScore;

        public OpenSetElement(OctreeIndex index, float fScore)
        {
            this.index = index;
            this.fScore = fScore;
        }

        public int CompareTo(OpenSetElement other)
        {
            return fScore.CompareTo(other.fScore);
        }
    }


    public struct NativeMinPQ : IDisposable
    {
        private NativeList<OpenSetElement> heap;

        public NativeMinPQ(Allocator allocator)
        {
            heap = new NativeList<OpenSetElement>(allocator);
        }

        public int Count => heap.Length;

        public bool IsCreated => heap.IsCreated;

        public unsafe void Clear()
        {
            heap.Clear();
        }

        public unsafe void Push(OpenSetElement element)
        {
            heap.Add(element);
            BubbleUp(heap.Length - 1);
        }

        public OpenSetElement Pop()
        {
            var root = heap[0];
            var last = heap.Length - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);
            if (heap.Length > 0)
                SinkDown(0);
            return root;
        }

        public unsafe void Dispose()
        {
            if (heap.IsCreated)
                heap.Dispose();
        }

        private void BubbleUp(int idx)
        {
            while (idx > 0)
            {
                var parent = (idx - 1) / 2;
                if (heap[idx].fScore < heap[parent].fScore)
                {
                    Swap(idx, parent);
                    idx = parent;
                }
                else
                {
                    break;
                }
            }
        }

        private void SinkDown(int idx)
        {
            var count = heap.Length;
            while (true)
            {
                var left = 2 * idx + 1;
                var right = 2 * idx + 2;
                var smallest = idx;

                if (left < count && heap[left].fScore < heap[smallest].fScore)
                    smallest = left;
                if (right < count && heap[right].fScore < heap[smallest].fScore)
                    smallest = right;

                if (smallest != idx)
                {
                    Swap(idx, smallest);
                    idx = smallest;
                }
                else
                {
                    break;
                }
            }
        }

        private void Swap(int a, int b)
        {
            var temp = heap[a];
            heap[a] = heap[b];
            heap[b] = temp;
        }
    }
}