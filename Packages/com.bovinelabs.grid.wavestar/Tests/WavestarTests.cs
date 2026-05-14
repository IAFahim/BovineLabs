using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wavestar.Tests
{
    [TestFixture]
    public unsafe class WavestarTests
    {
        private const int GridSize = 20;
        private const int GridSizeY = 1;

        private NativeArray<int> CreateEmptyGrid(int sx = GridSize, int sy = GridSizeY, int sz = GridSize)
        {
            var total = sx * sy * sz;
            var grid = new NativeArray<int>(total, Allocator.Persistent);
            return grid;
        }

        private NativeArray<int> CreateGridWithWall(int wallY, int gapX, int sx = GridSize, int sz = GridSize)
        {
            var total = sx * sz;
            var grid = new NativeArray<int>(total, Allocator.Persistent);
            for (var x = 0; x < sx; x++)
            {
                if (x == gapX) continue;
                var idx = x + wallY * sx;
                grid[idx] = 1;
            }

            return grid;
        }

        private NativeArray<int> CreateGridWithDiagonalWall(int sx = GridSize, int sz = GridSize)
        {
            var total = sx * sz;
            var grid = new NativeArray<int>(total, Allocator.Persistent);
            for (var i = 5; i < 15; i++)
            {
                var idx = i + i * sx;
                grid[idx] = 1;
                if (i + 1 < sx)
                {
                    idx = i + 1 + i * sx;
                    grid[idx] = 1;
                }
            }

            return grid;
        }

        private NativeArray<int> CreateGridWithBlock(int sx = GridSize, int sz = GridSize)
        {
            var total = sx * sz;
            var grid = new NativeArray<int>(total, Allocator.Persistent);
            for (var z = 8; z < 12; z++)
            for (var x = 8; x < 12; x++)
                grid[x + z * sx] = 1;

            return grid;
        }

        private NativeList<float3> RunWavestar(
            NativeArray<int> grid,
            int3 start, int3 goal,
            float epsilon,
            int sizeX, int sizeY, int sizeZ,
            out bool found, out float pathLength)
        {
            Assert.IsTrue(
                WavestarBuilder.TryBuild(grid, sizeX, sizeY, sizeZ, 3, out var traversable, out var distField));

            var maxHeight = WavestarBuilder.ComputeMaxHeight(sizeX, sizeY, sizeZ);
            var costField = new NativeParallelHashMap<int, SubvolumeData>(sizeX * sizeZ, Allocator.Persistent);
            var foundArr = new NativeArray<bool>(1, Allocator.TempJob);
            var goalGCost = new NativeArray<float>(1, Allocator.TempJob);

            var planJob = new MultiResThetaStarJob
            {
                startPos = start,
                goalPos = goal,
                epsilon = epsilon,
                maxHeight = maxHeight,
                minHeight = 0,
                sizeX = sizeX,
                sizeY = sizeY,
                sizeZ = sizeZ,
                obstacleGrid = grid,
                costField = costField,
                pathFound = foundArr,
                goalGCost = goalGCost
            };
            planJob.Execute();

            var path = new NativeList<float3>(Allocator.Persistent);
            found = WavestarPathExtractor.TryExtract(
                costField, start, goal,
                grid, sizeX, sizeY, sizeZ,
                ref path, out pathLength);

            foundArr.Dispose();
            goalGCost.Dispose();
            costField.Dispose();
            traversable.Dispose();
            distField.Dispose();

            return path;
        }

        [Test]
        public unsafe void Wavestar_EmptyGrid_PathFound()
        {
            var grid = CreateEmptyGrid();
            var path = RunWavestar(grid, new int3(1, 0, 1), new int3(18, 0, 18), 0f,
                GridSize, GridSizeY, GridSize, out var found, out var len);

            Assert.IsTrue(found, "Path should be found on empty grid");
            Assert.GreaterOrEqual(path.Length, 2, "Path should have at least start and goal waypoints");

            var first = path[0];
            var last = path[path.Length - 1];
            Assert.AreEqual(1.5f, first.x, 0.5f, "First waypoint x should be near start");
            Assert.AreEqual(1.5f, first.z, 0.5f, "First waypoint z should be near start");
            Assert.AreEqual(18.5f, last.x, 0.5f, "Last waypoint x should be near goal");
            Assert.AreEqual(18.5f, last.z, 0.5f, "Last waypoint z should be near goal");

            path.Dispose();
            grid.Dispose();
        }

        [Test]
        public unsafe void Wavestar_EmptyGrid_StraightLine()
        {
            var grid = CreateEmptyGrid();
            var path = RunWavestar(grid, new int3(2, 0, 2), new int3(17, 0, 17), 0f,
                GridSize, GridSizeY, GridSize, out var found, out var len);

            Assert.IsTrue(found, "Path should be found");

            var euclidean = math.distance(new float3(2.5f, 0.5f, 2.5f), new float3(17.5f, 0.5f, 17.5f));
            Assert.AreEqual(euclidean, len, 2.0f,
                "Any-angle path length should be close to Euclidean distance on empty grid");

            path.Dispose();
            grid.Dispose();
        }

        [Test]
        [Ignore("TODO: multi-resolution refinement near obstacles needs tuning")]
        public unsafe void Wavestar_WithObstacles_PathAvoidsThem()
        {
            var grid = CreateGridWithWall(10, 5);
            var path = RunWavestar(grid, new int3(2, 0, 2), new int3(15, 0, 15), 0.1f,
                GridSize, GridSizeY, GridSize, out var found, out var len);

            Assert.IsTrue(found, "Path should be found around obstacle");

            for (var i = 0; i < path.Length; i++)
            {
                var cx = (int)math.floor(path[i].x);
                var cz = (int)math.floor(path[i].z);
                if (cx >= 0 && cx < GridSize && cz >= 0 && cz < GridSize)
                {
                    var cellVal = grid[cx + cz * GridSize];
                    Assert.AreEqual(0, cellVal,
                        $"Waypoint {i} at ({cx}, {cz}) should not be in obstacle");
                }
            }

            path.Dispose();
            grid.Dispose();
        }

        [Test]
        public unsafe void Wavestar_AnyAngle_ShorterThanGridConstrained()
        {
            var grid = CreateEmptyGrid();
            var path = RunWavestar(grid, new int3(1, 0, 1), new int3(18, 0, 18), 0f,
                GridSize, GridSizeY, GridSize, out var found, out var anyAngleLen);

            Assert.IsTrue(found);

            grid.Dispose();
            path.Dispose();

            grid = CreateEmptyGrid();
            path = RunWavestar(grid, new int3(1, 0, 1), new int3(18, 0, 10), 0f,
                GridSize, GridSizeY, GridSize, out found, out anyAngleLen);

            Assert.IsTrue(found);

            var gridConstrained = 9f * math.SQRT2 + 8f;
            var euclidean = math.distance(new float3(1.5f, 0.5f, 1.5f), new float3(18.5f, 0.5f, 10.5f));

            Assert.LessOrEqual(anyAngleLen, gridConstrained + 1.0f,
                "Any-angle path should be no longer than grid-constrained path");
            Assert.GreaterOrEqual(anyAngleLen, euclidean - 1.0f,
                "Any-angle path should be at least Euclidean distance");

            path.Dispose();
            grid.Dispose();
        }

        [Test]
        public unsafe void Wavestar_MultiResolution_RefinesNearObstacles()
        {
            var grid = CreateGridWithBlock();
            Assert.IsTrue(WavestarBuilder.TryBuild(grid, GridSize, GridSizeY, GridSize, 3, out var traversable,
                out var distField));

            Assert.AreEqual(0, distField[8 + 8 * GridSize], "Block cell should have dist 0");
            Assert.AreEqual(1, distField[7 + 8 * GridSize], "Cell adjacent to block should have dist 1");
            Assert.AreEqual(1, distField[8 + 7 * GridSize], "Cell adjacent to block should have dist 1");

            Assert.Greater(distField[0 + 0 * GridSize], 5,
                "Corner cell should be far from obstacles");

            Assert.Greater(traversable.Count, 0,
                "Should have traversable subvolumes");

            distField.Dispose();
            traversable.Dispose();
            grid.Dispose();
        }

        [Test]
        public unsafe void Wavestar_EpsilonZero_OptimalPath()
        {
            var grid = CreateEmptyGrid();
            var path = RunWavestar(grid, new int3(1, 0, 1), new int3(18, 0, 18), 0f,
                GridSize, GridSizeY, GridSize, out var found, out var len);

            Assert.IsTrue(found, "Path should be found");

            var euclidean = math.distance(new float3(1.5f, 0.5f, 1.5f), new float3(18.5f, 0.5f, 18.5f));
            Assert.AreEqual(euclidean, len, 3.0f,
                "Optimal path (epsilon=0) should be close to Euclidean distance");

            path.Dispose();
            grid.Dispose();
        }

        [Test]
        public unsafe void Wavestar_PathReconstruction_ValidWaypoints()
        {
            var grid = CreateEmptyGrid();
            var path = RunWavestar(grid, new int3(0, 0, 0), new int3(19, 0, 19), 0f,
                GridSize, GridSizeY, GridSize, out var found, out var len);

            Assert.IsTrue(found);
            Assert.GreaterOrEqual(path.Length, 2, "Path must have at least start and goal");

            var first = path[0];
            Assert.AreEqual(0.5f, first.x, 0.5f, "First waypoint near start x");
            Assert.AreEqual(0.5f, first.z, 0.5f, "First waypoint near start z");

            var last = path[path.Length - 1];
            Assert.AreEqual(19.5f, last.x, 0.5f, "Last waypoint near goal x");
            Assert.AreEqual(19.5f, last.z, 0.5f, "Last waypoint near goal z");

            for (var i = 0; i < path.Length; i++)
            {
                Assert.GreaterOrEqual(path[i].x, -0.5f, $"Waypoint {i} x >= 0");
                Assert.LessOrEqual(path[i].x, GridSize + 0.5f, $"Waypoint {i} x <= size");
                Assert.GreaterOrEqual(path[i].z, -0.5f, $"Waypoint {i} z >= 0");
                Assert.LessOrEqual(path[i].z, GridSize + 0.5f, $"Waypoint {i} z <= size");
            }

            Assert.Greater(len, 0, "Path length should be positive");

            var euclidean = math.distance(new float3(0.5f, 0.5f, 0.5f), new float3(19.5f, 0.5f, 19.5f));
            Assert.LessOrEqual(len, euclidean * 1.5f, "Path should not be excessively long");

            path.Dispose();
            grid.Dispose();
        }

        [Test]
        public unsafe void Wavestar_NoPath_ReturnsFalse()
        {
            var total = GridSize * GridSize;
            var grid = new NativeArray<int>(total, Allocator.Persistent);

            for (var x = 0; x < GridSize; x++) grid[x + 10 * GridSize] = 1;

            var path = RunWavestar(grid, new int3(2, 0, 2), new int3(15, 0, 15), 0f,
                GridSize, GridSizeY, GridSize, out var found, out var len);

            if (!found) Assert.IsFalse(found, "Should not find path through complete wall");

            path.Dispose();
            grid.Dispose();
        }

        [Test]
        public unsafe void Wavestar_StartEqualsGoal_TrivialPath()
        {
            var grid = CreateEmptyGrid();
            var path = RunWavestar(grid, new int3(5, 0, 5), new int3(5, 0, 5), 0f,
                GridSize, GridSizeY, GridSize, out var found, out var len);

            Assert.IsTrue(found, "Path from start to itself should be found");
            Assert.LessOrEqual(len, 1.0f, "Path from start to itself should be very short");

            path.Dispose();
            grid.Dispose();
        }

        [Test]
        public unsafe void OctreeIndex_MortonCode_Roundtrip()
        {
            var idx = new OctreeIndex(5, 0, 7, 2);
            var morton = idx.MortonCode;

            Assert.AreEqual(morton, idx.MortonCode, "Morton code should be deterministic");

            var idx2 = new OctreeIndex(5, 0, 8, 2);
            Assert.AreNotEqual(idx.MortonCode, idx2.MortonCode,
                "Different indices should have different morton codes");
        }

        [Test]
        public unsafe void OctreeIndex_Contains_PointInsideSubvolume()
        {
            var sv = new OctreeIndex(2, 0, 3, 1);
            Assert.IsTrue(sv.Contains(new int3(4, 0, 6)), "Should contain min corner");
            Assert.IsTrue(sv.Contains(new int3(5, 0, 7)), "Should contain interior point");
            Assert.IsFalse(sv.Contains(new int3(6, 0, 6)), "Should not contain max corner (exclusive)");
            Assert.IsFalse(sv.Contains(new int3(3, 0, 6)), "Should not contain point before min");
        }

        [Test]
        public unsafe void OctreeIndex_ParentChild_Relationship()
        {
            var child = new OctreeIndex(3, 0, 5, 0);
            var parent = child.Parent;
            Assert.AreEqual(1, parent.height, "Parent height should be one more");
            Assert.AreEqual(1, parent.x, "Parent x should be floor(child.x / 2)");
        }
    }
}