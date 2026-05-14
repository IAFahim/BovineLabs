using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wavestar
{
    [BurstCompile]
    public struct WavestarBuilderJob : IJob
    {
        public NativeArray<int> obstacleGrid;
        public int sizeX;
        public int sizeY;
        public int sizeZ;
        public int maxHeight;
        public int refinementRadius;
        public NativeHashSet<int> traversableSubvolumes;
        public NativeArray<int> distanceToObstacle;

        public unsafe void Execute()
        {
            var obstacleMap = new NativeObstacleMap(obstacleGrid, sizeX, sizeY, sizeZ);
            ComputeDistanceField();
            BuildMultiResDecomposition(obstacleMap);
        }

        private void ComputeDistanceField()
        {
            var totalCells = sizeX * sizeY * sizeZ;
            for (var i = 0; i < totalCells; i++) distanceToObstacle[i] = obstacleGrid[i] != 0 ? 0 : totalCells;
            var queue = new NativeList<int3>(Allocator.Temp);
            for (var z = 0; z < sizeZ; z++)
            for (var y = 0; y < sizeY; y++)
            for (var x = 0; x < sizeX; x++)
                if (obstacleGrid[x + y * sizeX + z * sizeX * sizeY] != 0)
                    queue.Add(new int3(x, y, z));

            var head = 0;
            while (head < queue.Length)
            {
                var pos = queue[head];
                head++;
                var currentDist = distanceToObstacle[pos.x + pos.y * sizeX + pos.z * sizeX * sizeY];
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
            var idx = x + y * sizeX + z * sizeX * sizeY;
            if (distanceToObstacle[idx] > parentDist + 1)
            {
                distanceToObstacle[idx] = parentDist + 1;
                queue.Add(new int3(x, y, z));
            }
        }

        private void BuildMultiResDecomposition(NativeObstacleMap obstacleMap)
        {
            var rootSize = 1 << maxHeight;
            var rootsX = (sizeX + rootSize - 1) / rootSize;
            var rootsY = (sizeY + rootSize - 1) / rootSize;
            var rootsZ = (sizeZ + rootSize - 1) / rootSize;
            for (var rz = 0; rz < rootsZ; rz++)
            for (var ry = 0; ry < rootsY; ry++)
            for (var rx = 0; rx < rootsX; rx++)
            {
                var rootIdx = new OctreeIndex(rx, ry, rz, maxHeight);
                DecomposeRecursive(rootIdx, obstacleMap);
            }
        }

        private void DecomposeRecursive(OctreeIndex idx, NativeObstacleMap obstacleMap)
        {
            var allBlocked = true;
            var allFree = true;
            var s = idx.Size;
            var minX = idx.x * s;
            var minY = idx.y * s;
            var minZ = idx.z * s;
            var maxX = math.min(minX + s, sizeX);
            var maxY = math.min(minY + s, sizeY);
            var maxZ = math.min(minZ + s, sizeZ);
            var step = math.max(1, s / 4);
            for (var zz = minZ; zz < maxZ; zz += step)
            for (var yy = minY; yy < maxY; yy += step)
            for (var xx = minX; xx < maxX; xx += step)
            {
                var cellIdx = xx + yy * sizeX + zz * sizeX * sizeY;
                var blocked = obstacleGrid[cellIdx] != 0;
                if (blocked) allFree = false;
                else allBlocked = false;
            }

            if (allFree || allBlocked)
                if (s <= 4)
                {
                    allBlocked = true;
                    allFree = true;
                    for (var zz = minZ; zz < maxZ; zz++)
                    for (var yy = minY; yy < maxY; yy++)
                    for (var xx = minX; xx < maxX; xx++)
                    {
                        var cellIdx = xx + yy * sizeX + zz * sizeX * sizeY;
                        var blocked = obstacleGrid[cellIdx] != 0;
                        if (blocked) allFree = false;
                        else allBlocked = false;
                    }
                }

            if (allBlocked)
                return;
            if (allFree)
            {
                var minDist = int.MaxValue;
                for (var zz = minZ; zz < maxZ && minDist > refinementRadius; zz++)
                for (var yy = minY; yy < maxY && minDist > refinementRadius; yy++)
                for (var xx = minX; xx < maxX && minDist > refinementRadius; xx++)
                {
                    var d = distanceToObstacle[xx + yy * sizeX + zz * sizeX * sizeY];
                    minDist = math.min(minDist, d);
                }

                if (minDist > refinementRadius)
                {
                    traversableSubvolumes.Add(idx.MortonCode);
                    return;
                }
            }

            if (idx.height > 0)
            {
                var childCount = sizeY > 1 ? 8 : 4;
                for (var c = 0; c < childCount; c++)
                {
                    var child = idx.Child(c);
                    var cs = child.Size;
                    if (child.x * cs >= sizeX || child.y * cs >= sizeY || child.z * cs >= sizeZ)
                        continue;
                    DecomposeRecursive(child, obstacleMap);
                }
            }
            else
            {
                for (var zz = minZ; zz < maxZ; zz++)
                for (var yy = minY; yy < maxY; yy++)
                for (var xx = minX; xx < maxX; xx++)
                {
                    var cellIdx = xx + yy * sizeX + zz * sizeX * sizeY;
                    if (obstacleGrid[cellIdx] == 0)
                    {
                        var leaf = new OctreeIndex(xx, yy, zz, 0);
                        traversableSubvolumes.Add(leaf.MortonCode);
                    }
                }
            }
        }
    }

    public static class WavestarBuilder
    {
        public static int ComputeMaxHeight(int sizeX, int sizeY, int sizeZ)
        {
            var maxDim = math.max(math.max(sizeX, sizeY), sizeZ);
            var h = 0;
            while (1 << h < maxDim)
                h++;
            return h;
        }

        public static bool TryBuild(
            NativeArray<int> obstacleGrid,
            int sizeX, int sizeY, int sizeZ,
            int refinementRadius,
            out NativeHashSet<int> traversable,
            out NativeArray<int> distanceToObstacle)
        {
            var maxHeight = ComputeMaxHeight(sizeX, sizeY, sizeZ);
            var totalCells = sizeX * sizeY * sizeZ;
            distanceToObstacle = new NativeArray<int>(totalCells, Allocator.Persistent);
            traversable = new NativeHashSet<int>(totalCells / 4, Allocator.Persistent);
            var job = new WavestarBuilderJob
            {
                obstacleGrid = obstacleGrid,
                sizeX = sizeX,
                sizeY = sizeY,
                sizeZ = sizeZ,
                maxHeight = maxHeight,
                refinementRadius = refinementRadius,
                traversableSubvolumes = traversable,
                distanceToObstacle = distanceToObstacle
            };
            var handle = job.Schedule();
            handle.Complete();
            return true;
        }
    }
}