using BovineLabs.Grid;
using BovineLabs.Grid.MeshA;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.MeshA.Tests
{
    public class MeshAStarTests
    {
        private PrimitiveSet prims;
        private MeshGraphData mesh;

        [SetUp]
        public void Setup()
        {
            prims = PrimitiveSetFactory.CreateCardinal8(Allocator.Persistent);
            mesh = MeshGraphBuilder.Build(prims, Allocator.Persistent);
        }

        [TearDown]
        public void TearDown()
        {
            prims.Dispose();
            mesh.Dispose();
        }

        [Test]
        public void MeshAStar_EmptyGrid_PathFound()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);
            using var result = MeshAStar.FindPath(grid, prims, mesh,
                new int2(0, 0), new int2(5, 5), 0, 1.0f, Allocator.Temp);

            Assert.IsTrue(result.Found);
            Assert.Greater(result.Path.Length, 0);
            Assert.AreEqual(new int2(0, 0), result.Path[0]);
            Assert.AreEqual(new int2(5, 5), result.Path[result.Path.Length - 1]);
        }

        [Test]
        public void MeshAStar_StartEqualsGoal_TrivialPath()
        {
            using var grid = new NativeGrid2D(5, 5, Allocator.Temp);
            using var result = MeshAStar.FindPath(grid, prims, mesh,
                new int2(2, 2), new int2(2, 2), 0, 1.0f, Allocator.Temp);

            // Start = goal, path should be found with just the start node
            Assert.IsTrue(result.Found);
            Assert.AreEqual(1, result.Path.Length);
            Assert.AreEqual(new int2(2, 2), result.Path[0]);
        }

        [Test]
        public void MeshAStar_BlockedPath_GoesAround()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);
            // Create a horizontal wall at y=5, x=0..8
            for (int x = 0; x < 9; x++) grid.Set(x, 5, CellState.Blocked);

            using var result = MeshAStar.FindPath(grid, prims, mesh,
                new int2(4, 3), new int2(4, 7), 0, 1.0f, Allocator.Temp);

            Assert.IsTrue(result.Found);
            Assert.Greater(result.Path.Length, 0);

            // Verify no path node is in a blocked cell
            for (int i = 0; i < result.Path.Length; i++)
            {
                Assert.IsTrue(grid.IsFree(result.Path[i]),
                    $"Path node {i} at {result.Path[i]} is in a blocked cell");
            }
        }

        [Test]
        public void MeshAStar_NoPath_ReturnsFalse()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);
            // Block the entire row y=5
            for (int x = 0; x < 10; x++) grid.Set(x, 5, CellState.Blocked);

            using var result = MeshAStar.FindPath(grid, prims, mesh,
                new int2(4, 2), new int2(4, 8), 0, 1.0f, Allocator.Temp);

            Assert.IsFalse(result.Found);
        }

        [Test]
        public void MeshAStar_PathCost_Positive()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);
            using var result = MeshAStar.FindPath(grid, prims, mesh,
                new int2(0, 0), new int2(3, 4), 0, 1.0f, Allocator.Temp);

            Assert.IsTrue(result.Found);
            Assert.Greater(result.PathCost, 0f);
        }

        [Test]
        public void MeshAStar_NodesExplored_Counted()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);
            using var result = MeshAStar.FindPath(grid, prims, mesh,
                new int2(0, 0), new int2(5, 5), 0, 1.0f, Allocator.Temp);

            Assert.IsTrue(result.Found);
            Assert.Greater(result.NodesExplored, 0);
        }

        [Test]
        public void PrimitiveSet_CreateCardinal8_Has8Primitives()
        {
            Assert.AreEqual(8, prims.Primitives.Length);
        }

        [Test]
        public void PrimitiveSet_CreateExtended8_Has24Primitives()
        {
            using var ext = PrimitiveSetFactory.CreateExtended8(Allocator.Temp);
            Assert.AreEqual(24, ext.Primitives.Length);
            ext.Dispose();
        }

        [Test]
        public void MeshGraph_InitialConfigMapping_Correct()
        {
            for (int theta = 0; theta < 8; theta++)
            {
                Assert.AreEqual(theta, mesh.InitialConfigByTheta[theta]);
                Assert.AreEqual(theta, mesh.ThetaByInitialConfig[theta]);
            }
        }

        [Test]
        public void MeshAStar_Weighted_FasterThanOptimal()
        {
            using var grid = new NativeGrid2D(20, 20, Allocator.Temp);

            using var optimal = MeshAStar.FindPath(grid, prims, mesh,
                new int2(0, 0), new int2(15, 15), 0, 1.0f, Allocator.Temp);

            using var weighted = MeshAStar.FindPath(grid, prims, mesh,
                new int2(0, 0), new int2(15, 15), 0, 2.0f, Allocator.Temp);

            Assert.IsTrue(optimal.Found);
            Assert.IsTrue(weighted.Found);
            // Weighted should explore fewer nodes (but path cost >= optimal)
            Assert.LessOrEqual(weighted.NodesExplored, optimal.NodesExplored);
        }
    }
}
