using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wavestar
{
    /// <summary>
    /// Builds the initial multi-resolution occupancy map from a flat grid.
    ///
    /// Strategy: Start at coarsest level and subdivide only near obstacles.
    /// Far from obstacles, large subvolumes are all-traversable → coarse resolution.
    /// Near obstacles, we refine to capture the boundary accurately.
    ///
    /// This implements the key insight from Wavestar: inflection points of optimal
    /// paths only occur near obstacles, so fine resolution is only needed there.
    /// </summary>
    [BurstCompile]
    public struct WavestarBuilderJob : IJob
    {
        // ─── Inputs ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Flat obstacle grid: 0 = free, 1 = blocked.
        /// Size must be sizeX * sizeY * sizeZ.
        /// </summary>
        public NativeArray<int> obstacleGrid;

        public int sizeX;
        public int sizeY;
        public int sizeZ;

        /// <summary>
        /// Maximum height (coarsest level). The grid should be padded to a power of 2
        /// matching 2^maxHeight, or maxHeight should be set such that 2^maxHeight >= max dimension.
        /// </summary>
        public int maxHeight;

        /// <summary>
        /// Distance (in cells) from obstacles within which to use finest resolution.
        /// </summary>
        public int refinementRadius;

        // ─── Outputs ────────────────────────────────────────────────────────────

        /// <summary>
        /// Output: set of morton codes of traversable leaf subvolumes.
        /// </summary>
        public NativeHashSet<int> traversableSubvolumes;

        /// <summary>
        /// Output: pre-computed distance-to-nearest-obstacle for each cell.
        /// Used to determine refinement levels.
        /// </summary>
        public NativeArray<int> distanceToObstacle;

        public void Execute()
        {
            var obstacleMap = new NativeObstacleMap(obstacleGrid, sizeX, sizeY, sizeZ);

            // Step 1: Compute distance to nearest obstacle for each cell using BFS
            ComputeDistanceField();

            // Step 2: Build multi-resolution decomposition
            BuildMultiResDecomposition(obstacleMap);
        }

        /// <summary>
        /// BFS-based distance field computation.
        /// Cells that are obstacles have distance 0; free cells get their
        /// Manhattan distance to the nearest obstacle.
        /// </summary>
        private void ComputeDistanceField()
        {
            int totalCells = sizeX * sizeY * sizeZ;

            // Initialize: obstacles = 0, free = large number
            for (int i = 0; i < totalCells; i++)
            {
                distanceToObstacle[i] = obstacleGrid[i] != 0 ? 0 : totalCells;
            }

            // BFS from obstacle cells
            var queue = new NativeList<int3>(Allocator.Temp);
            for (int z = 0; z < sizeZ; z++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        if (obstacleGrid[x + y * sizeX + z * sizeX * sizeY] != 0)
                        {
                            queue.Add(new int3(x, y, z));
                        }
                    }
                }
            }

            // BFS propagation
            int head = 0;
            while (head < queue.Length)
            {
                int3 pos = queue[head];
                head++;
                int currentDist = distanceToObstacle[pos.x + pos.y * sizeX + pos.z * sizeX * sizeY];

                // 6-connected neighbors (or 4 in 2D)
                TryPropagate(queue, pos.x + 1, pos.y, pos.z, currentDist);
                TryPropagate(queue, pos.x - 1, pos.y, pos.z, currentDist);
                if (sizeY > 1)
                {
                    TryPropagate(queue, pos.x, pos.y + 1, pos.z, currentDist);
                    TryPropagate(queue, pos.x, pos.y - 1, pos.z, currentDist);
                }
                if (sizeZ > 1)
                {
                    TryPropagate(queue, pos.x, pos.y, pos.z + 1, currentDist);
                    TryPropagate(queue, pos.x, pos.y, pos.z - 1, currentDist);
                }
            }

            queue.Dispose();
        }

        private void TryPropagate(NativeList<int3> queue, int x, int y, int z, int parentDist)
        {
            if (x < 0 || x >= sizeX || y < 0 || y >= sizeY || z < 0 || z >= sizeZ)
                return;
            int idx = x + y * sizeX + z * sizeX * sizeY;
            if (distanceToObstacle[idx] > parentDist + 1)
            {
                distanceToObstacle[idx] = parentDist + 1;
                queue.Add(new int3(x, y, z));
            }
        }

        /// <summary>
        /// Build the multi-resolution octree decomposition.
        /// Recursively subdivide starting from the root (coarsest level).
        /// Stop subdividing when a subvolume is: fully blocked, or fully free and
        /// sufficiently far from obstacles.
        /// </summary>
        private void BuildMultiResDecomposition(NativeObstacleMap obstacleMap)
        {
            // Start from the root subvolume covering the entire grid
            // We need the root to cover the grid: compute root coords at maxHeight
            int rootSize = 1 << maxHeight;
            int rootsX = (sizeX + rootSize - 1) / rootSize;
            int rootsY = (sizeY + rootSize - 1) / rootSize;
            int rootsZ = (sizeZ + rootSize - 1) / rootSize;

            for (int rz = 0; rz < rootsZ; rz++)
            {
                for (int ry = 0; ry < rootsY; ry++)
                {
                    for (int rx = 0; rx < rootsX; rx++)
                    {
                        var rootIdx = new OctreeIndex(rx, ry, rz, maxHeight);
                        DecomposeRecursive(rootIdx, obstacleMap);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively decompose a subvolume. If it's fully blocked → skip.
        /// If fully free and far from obstacles → add as coarse leaf.
        /// Otherwise → subdivide into children.
        /// </summary>
        private void DecomposeRecursive(OctreeIndex idx, NativeObstacleMap obstacleMap)
        {
            // Check if subvolume is fully blocked
            bool allBlocked = true;
            bool allFree = true;

            int s = idx.Size;
            int minX = idx.x * s;
            int minY = idx.y * s;
            int minZ = idx.z * s;
            int maxX = math.min(minX + s, sizeX);
            int maxY = math.min(minY + s, sizeY);
            int maxZ = math.min(minZ + s, sizeZ);

            // Sample a few points to determine status
            // For efficiency, check boundary and center cells
            int step = math.max(1, s / 4);

            for (int zz = minZ; zz < maxZ; zz += step)
            {
                for (int yy = minY; yy < maxY; yy += step)
                {
                    for (int xx = minX; xx < maxX; xx += step)
                    {
                        int cellIdx = xx + yy * sizeX + zz * sizeX * sizeY;
                        bool blocked = obstacleGrid[cellIdx] != 0;
                        if (blocked) allFree = false;
                        else allBlocked = false;
                    }
                }
            }

            // Also check exact corners and edges
            if (allFree || allBlocked)
            {
                // Verify with full check for small subvolumes
                if (s <= 4)
                {
                    allBlocked = true;
                    allFree = true;
                    for (int zz = minZ; zz < maxZ; zz++)
                    {
                        for (int yy = minY; yy < maxY; yy++)
                        {
                            for (int xx = minX; xx < maxX; xx++)
                            {
                                int cellIdx = xx + yy * sizeX + zz * sizeX * sizeY;
                                bool blocked = obstacleGrid[cellIdx] != 0;
                                if (blocked) allFree = false;
                                else allBlocked = false;
                            }
                        }
                    }
                }
            }

            // Fully blocked → don't add
            if (allBlocked)
                return;

            // Fully free: check minimum distance to obstacle to decide resolution
            if (allFree)
            {
                int minDist = int.MaxValue;
                for (int zz = minZ; zz < maxZ && minDist > refinementRadius; zz++)
                {
                    for (int yy = minY; yy < maxY && minDist > refinementRadius; yy++)
                    {
                        for (int xx = minX; xx < maxX && minDist > refinementRadius; xx++)
                        {
                            int d = distanceToObstacle[xx + yy * sizeX + zz * sizeX * sizeY];
                            minDist = math.min(minDist, d);
                        }
                    }
                }

                // If the entire subvolume is far enough from obstacles, keep it coarse
                if (minDist > refinementRadius)
                {
                    traversableSubvolumes.Add(idx.MortonCode);
                    return;
                }
            }

            // Need to subdivide (either mixed, or too close to obstacles)
            if (idx.height > 0)
            {
                int childCount = (sizeY > 1) ? 8 : 4;
                for (int c = 0; c < childCount; c++)
                {
                    var child = idx.Child(c);

                    // Bounds check
                    int cs = child.Size;
                    if (child.x * cs >= sizeX || child.y * cs >= sizeY || child.z * cs >= sizeZ)
                        continue;

                    DecomposeRecursive(child, obstacleMap);
                }
            }
            else
            {
                // At finest resolution: add individual free cells as leaves
                for (int zz = minZ; zz < maxZ; zz++)
                {
                    for (int yy = minY; yy < maxY; yy++)
                    {
                        for (int xx = minX; xx < maxX; xx++)
                        {
                            int cellIdx = xx + yy * sizeX + zz * sizeX * sizeY;
                            if (obstacleGrid[cellIdx] == 0)
                            {
                                var leaf = new OctreeIndex(xx, yy, zz, 0);
                                traversableSubvolumes.Add(leaf.MortonCode);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// High-level builder API for constructing the multi-resolution occupancy map.
    /// </summary>
    public static class WavestarBuilder
    {
        /// <summary>
        /// Compute the maximum octree height for a grid of the given dimensions.
        /// The height is ceil(log2(max(sizeX, sizeY, sizeZ))).
        /// </summary>
        public static int ComputeMaxHeight(int sizeX, int sizeY, int sizeZ)
        {
            int maxDim = math.max(math.max(sizeX, sizeY), sizeZ);
            int h = 0;
            while ((1 << h) < maxDim)
                h++;
            return h;
        }

        /// <summary>
        /// Build the multi-resolution occupancy map from a flat grid.
        /// Returns a NativeHashSet of morton codes for traversable leaf subvolumes.
        /// Also outputs the distance field.
        /// </summary>
        public static NativeHashSet<int> Build(
            NativeArray<int> obstacleGrid,
            int sizeX, int sizeY, int sizeZ,
            int refinementRadius,
            out NativeArray<int> distanceToObstacle)
        {
            int maxHeight = ComputeMaxHeight(sizeX, sizeY, sizeZ);
            int totalCells = sizeX * sizeY * sizeZ;

            distanceToObstacle = new NativeArray<int>(totalCells, Allocator.Persistent);
            var traversable = new NativeHashSet<int>(totalCells / 4, Allocator.Persistent);

            var job = new WavestarBuilderJob
            {
                obstacleGrid = obstacleGrid,
                sizeX = sizeX,
                sizeY = sizeY,
                sizeZ = sizeZ,
                maxHeight = maxHeight,
                refinementRadius = refinementRadius,
                traversableSubvolumes = traversable,
                distanceToObstacle = distanceToObstacle,
            };

            var handle = job.Schedule();
            handle.Complete();

            return traversable;
        }
    }
}
