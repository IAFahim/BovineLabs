using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wavestar
{
    /// <summary>
    /// Identifies a subvolume in the multi-resolution octree.
    /// (x, y, z) is the integer position at the given height (resolution level).
    /// height=0 is finest resolution; larger height = coarser (larger subvolume).
    /// Side length of a subvolume at height h = 2^h.
    /// </summary>
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

        /// <summary>
        /// Side length of this subvolume: 2^height.
        /// </summary>
        public int Size => 1 << height;

        /// <summary>
        /// Center of this subvolume in world coordinates (assuming cell size = 1).
        /// </summary>
        public float3 Center => new float3(
            x * Size + Size * 0.5f,
            y * Size + Size * 0.5f,
            z * Size + Size * 0.5f);

        /// <summary>
        /// Minimum corner of this subvolume.
        /// </summary>
        public int3 MinCorner => new int3(x * Size, y * Size, z * Size);

        /// <summary>
        /// Maximum corner (exclusive) of this subvolume.
        /// </summary>
        public int3 MaxCornerExclusive => new int3(
            (x + 1) * Size,
            (y + 1) * Size,
            (z + 1) * Size);

        /// <summary>
        /// Check if a point (in grid coordinates) is contained within this subvolume.
        /// </summary>
        public bool Contains(int3 point)
        {
            int s = Size;
            return point.x >= x * s && point.x < (x + 1) * s &&
                   point.y >= y * s && point.y < (y + 1) * s &&
                   point.z >= z * s && point.z < (z + 1) * s;
        }

        /// <summary>
        /// Check if a point (continuous) is inside the subvolume bounds.
        /// </summary>
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

        /// <summary>
        /// Compute the child index at the given child position (0-7).
        /// Child positions are determined by the low bits of the coordinates.
        /// </summary>
        public OctreeIndex Child(int childIndex)
        {
            int cx = (childIndex & 1);
            int cy = (childIndex >> 1) & 1;
            int cz = (childIndex >> 2) & 1;
            return new OctreeIndex(x * 2 + cx, y * 2 + cy, z * 2 + cz, height - 1);
        }

        /// <summary>
        /// Get the parent of this subvolume (one level coarser).
        /// </summary>
        public OctreeIndex Parent => new OctreeIndex(x >> 1, y >> 1, z >> 1, height + 1);

        /// <summary>
        /// Compute a Morton/Z-order code for this subvolume.
        /// Encodes (x, y, z, height) into a single int for hashing.
        /// We interleave bits of x, y, z and use the top bits for height.
        /// </summary>
        public int MortonCode
        {
            get
            {
                // Spread bits for x, y, z and combine
                uint spread(uint v)
                {
                    v = (v | (v << 16)) & 0x030000FF;
                    v = (v | (v << 8)) & 0x0300F00F;
                    v = (v | (v << 4)) & 0x030C30C3;
                    v = (v | (v << 2)) & 0x09249249;
                    return v;
                }

                uint mx = spread((uint)x);
                uint my = spread((uint)y);
                uint mz = spread((uint)z);
                uint morton = mx | (my << 1) | (mz << 2);
                // Encode height in upper bits
                return (int)(morton | ((uint)height << 24));
            }
        }

        public bool Equals(OctreeIndex other)
        {
            return x == other.x && y == other.y && z == other.z && height == other.height;
        }

        public override bool Equals(object obj) => obj is OctreeIndex other && Equals(other);

        public override int GetHashCode() => MortonCode;

        public override string ToString() => $"OctreeIndex({x}, {y}, {z}, h={height})";

        public static bool operator ==(OctreeIndex a, OctreeIndex b) => a.Equals(b);
        public static bool operator !=(OctreeIndex a, OctreeIndex b) => !a.Equals(b);
    }

    /// <summary>
    /// Search state stored per subvolume in the cost field.
    /// Records the predecessor position and the g-cost to reach the center of this subvolume.
    /// </summary>
    public struct SubvolumeData
    {
        public int predecessorX;
        public int predecessorY;
        public int predecessorZ;
        public float gCost;

        public SubvolumeData(int predX, int predY, int predZ, float gCost)
        {
            this.predecessorX = predX;
            this.predecessorY = predY;
            this.predecessorZ = predZ;
            this.gCost = gCost;
        }

        /// <summary>
        /// The predecessor position as int3 (grid coordinates of predecessor center).
        /// </summary>
        public int3 Predecessor => new int3(predecessorX, predecessorY, predecessorZ);

        /// <summary>
        /// Predecessor center in continuous coordinates.
        /// </summary>
        public float3 PredecessorCenter => new float3(predecessorX, predecessorY, predecessorZ);

        public static SubvolumeData Invalid => new SubvolumeData(0, 0, 0, float.PositiveInfinity);
    }

    /// <summary>
    /// Result of comparing two candidate g-costs during UpdateSubvolume.
    /// StrictlyBetter: new cost is strictly less than old by more than epsilon threshold.
    /// Ambiguous: new cost is within epsilon of old - need to refine.
    /// NotBetter: new cost is strictly worse.
    /// </summary>
    public enum ComparisonResult : byte
    {
        StrictlyBetter,
        Ambiguous,
        NotBetter
    }

    /// <summary>
    /// Multi-resolution cost field backed by a NativeHashMap.
    /// Maps morton codes of OctreeIndex subvolumes to their SubvolumeData.
    /// Provides compressed storage since large subvolumes share a single predecessor.
    /// </summary>
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

        public void Set(OctreeIndex idx, SubvolumeData sv)
        {
            data[idx.MortonCode] = sv;
        }

        public bool Contains(OctreeIndex idx)
        {
            return data.ContainsKey(idx.MortonCode);
        }

        public void Remove(OctreeIndex idx)
        {
            data.Remove(idx.MortonCode);
        }

        /// <summary>
        /// Get the raw NativeHashMap for iteration in Burst jobs.
        /// </summary>
        public NativeHashMap<int, SubvolumeData> RawData => data;

        public NativeArray<int> GetKeyArray(Allocator allocator)
        {
            return data.GetKeyArray(allocator);
        }

        public NativeArray<SubvolumeData> GetValueArray(Allocator allocator)
        {
            return data.GetValueArray(allocator);
        }

        public void Clear()
        {
            data.Clear();
        }

        public void Dispose()
        {
            if (data.IsCreated)
                data.Dispose();
        }

        public bool IsCreated => data.IsCreated;
    }

    /// <summary>
    /// Interface for checking if a subvolume is traversable.
    /// Implementations provide obstacle data from different sources.
    /// </summary>
    public interface IObstacleMap
    {
        /// <summary>
        /// Check if a single cell at grid coordinates (x, y, z) is traversable.
        /// </summary>
        bool IsTraversable(int x, int y, int z);

        /// <summary>
        /// Check if an entire subvolume (all cells within it) is traversable.
        /// A subvolume is traversable only if ALL cells within it are traversable.
        /// </summary>
        bool IsSubvolumeTraversable(OctreeIndex idx);

        /// <summary>
        /// Grid dimensions.
        /// </summary>
        int SizeX { get; }
        int SizeY { get; }
        int SizeZ { get; }
    }

    /// <summary>
    /// Burst-compatible obstacle map backed by a NativeArray of CellState.
    /// Compatible with BovineLabs.Grid.CellState.
    /// </summary>
    [BurstCompile]
    public struct NativeObstacleMap : IObstacleMap
    {
        private NativeArray<int> grid;
        private int sizeX;
        private int sizeY;
        private int sizeZ;

        /// <summary>
        /// Cell state value indicating an obstacle/blocked cell.
        /// </summary>
        public const int BlockedCellState = 1;

        public NativeObstacleMap(NativeArray<int> grid, int sizeX, int sizeY, int sizeZ)
        {
            this.grid = grid;
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.sizeZ = sizeZ;
        }

        public int SizeX => sizeX;
        public int SizeY => sizeY;
        public int SizeZ => sizeZ;

        public bool IsTraversable(int x, int y, int z)
        {
            if (x < 0 || x >= sizeX || y < 0 || y >= sizeY || z < 0 || z >= sizeZ)
                return false;
            int idx = x + y * sizeX + z * sizeX * sizeY;
            return grid[idx] != BlockedCellState;
        }

        public bool IsSubvolumeTraversable(OctreeIndex sv)
        {
            int s = sv.Size;
            int minX = sv.x * s;
            int minY = sv.y * s;
            int minZ = sv.z * s;
            int maxX = math.min(minX + s, sizeX);
            int maxY = math.min(minY + s, sizeY);
            int maxZ = math.min(minZ + s, sizeZ);

            for (int zz = minZ; zz < maxZ; zz++)
            {
                for (int yy = minY; yy < maxY; yy++)
                {
                    for (int xx = minX; xx < maxX; xx++)
                    {
                        if (!IsTraversable(xx, yy, zz))
                            return false;
                    }
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Priority queue element for the open set.
    /// Stores an OctreeIndex and its f-score for sorting.
    /// </summary>
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

    /// <summary>
    /// Simple min-heap priority queue for Burst compatibility.
    /// </summary>
    public struct NativeMinPQ : IDisposable
    {
        private NativeList<OpenSetElement> heap;

        public NativeMinPQ(Allocator allocator)
        {
            heap = new NativeList<OpenSetElement>(allocator);
        }

        public int Count => heap.Length;

        public bool IsCreated => heap.IsCreated;

        public void Clear() => heap.Clear();

        public void Push(OpenSetElement element)
        {
            heap.Add(element);
            BubbleUp(heap.Length - 1);
        }

        public OpenSetElement Pop()
        {
            var root = heap[0];
            int last = heap.Length - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);
            if (heap.Length > 0)
                SinkDown(0);
            return root;
        }

        public void Dispose()
        {
            if (heap.IsCreated)
                heap.Dispose();
        }

        private void BubbleUp(int idx)
        {
            while (idx > 0)
            {
                int parent = (idx - 1) / 2;
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
            int count = heap.Length;
            while (true)
            {
                int left = 2 * idx + 1;
                int right = 2 * idx + 2;
                int smallest = idx;

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
