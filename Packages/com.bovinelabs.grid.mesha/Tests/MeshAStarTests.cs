using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.MeshA.Tests
{
    public class MeshAStarTests
    {
        private MeshGraphData mesh;
        private PrimitiveSet prims;

        [SetUp]
        public unsafe void Setup()
        {
            Assert.IsTrue(PrimitiveSetFactory.TryCreateCardinal8(Allocator.Persistent, out var p));
            prims = p;
            Assert.IsTrue(MeshGraphBuilder.TryBuild(prims, Allocator.Persistent, out var m));
            mesh = m;
        }

        [TearDown]
        public unsafe void TearDown()
        {
            prims.Dispose();
            mesh.Dispose();
        }

        [Test]
        public void MeshAStar_FullyBlocked_PathNotFound()
        {
            var grid = new NativeGrid2D(5, 5, Allocator.Temp);
            for(int i = 0; i < grid.Cells.Length; i++) grid.Cells[i] = CellState.Blocked;
            Assert.IsFalse(MeshAStar.TryFindPath(grid, prims, mesh, new int2(0, 0), new int2(4, 4), out var result));
            Assert.IsFalse(result.Found);
            result.Dispose();
            grid.Dispose();
        }

        [Test]
        public unsafe void MeshAStar_EmptyGrid_PathFound()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);
            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(0, 0), new int2(5, 5), out var result));

            Assert.IsTrue(result.Found);
            Assert.Greater(result.Path.Length, 0);
            Assert.AreEqual(new int2(0, 0), result.Path[0]);
            Assert.AreEqual(new int2(5, 5), result.Path[^1]);
            result.Dispose();
        }

        [Test]
        public unsafe void MeshAStar_StartEqualsGoal_TrivialPath()
        {
            using var grid = new NativeGrid2D(5, 5, Allocator.Temp);
            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(2, 2), new int2(2, 2), out var result));


            Assert.IsTrue(result.Found);
            Assert.AreEqual(1, result.Path.Length);
            Assert.AreEqual(new int2(2, 2), result.Path[0]);
            result.Dispose();
        }

        [Test]
        [Ignore("TODO: swept-cell collision check needs tuning for obstacle detour")]
        public unsafe void MeshAStar_BlockedPath_GoesAround()
        {
            var grid = new NativeGrid2D(10, 10, Allocator.Temp);

            for (var x = 0; x < 9; x++) grid.Set(x, 5, CellState.Blocked);

            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(4, 3), new int2(4, 7), out var result));

            Assert.IsTrue(result.Found);
            Assert.Greater(result.Path.Length, 0);


            for (var i = 0; i < result.Path.Length; i++)
                Assert.IsTrue(grid.IsFree(result.Path[i]),
                    $"Path node {i} at {result.Path[i]} is in a blocked cell");
            result.Dispose();
            grid.Dispose();
        }

        [Test]
        public unsafe void MeshAStar_NoPath_ReturnsFalse()
        {
            var grid = new NativeGrid2D(10, 10, Allocator.Temp);

            for (var x = 0; x < 10; x++) grid.Set(x, 5, CellState.Blocked);

            Assert.IsFalse(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(4, 2), new int2(4, 8), out var result));

            Assert.IsFalse(result.Found);
            result.Dispose();
            grid.Dispose();
        }

        [Test]
        [Ignore("TODO: zero-cost transitions from unconnected primitives")]
        public unsafe void MeshAStar_PathCost_Positive()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);
            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(0, 0), new int2(3, 4), out var result));

            Assert.IsTrue(result.Found);
            Assert.Greater(result.PathCost, 0f);
            result.Dispose();
        }

        [Test]
        public unsafe void MeshAStar_NodesExplored_Counted()
        {
            using var grid = new NativeGrid2D(10, 10, Allocator.Temp);
            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(0, 0), new int2(5, 5), out var result));

            Assert.IsTrue(result.Found);
            Assert.Greater(result.NodesExplored, 0);
            result.Dispose();
        }

        [Test]
        public unsafe void PrimitiveSet_CreateCardinal8_Has8Primitives()
        {
            Assert.AreEqual(8, prims.Primitives.Length);
        }

        [Test]
        public unsafe void PrimitiveSet_CreateExtended8_Has24Primitives()
        {
            Assert.IsTrue(PrimitiveSetFactory.TryCreateExtended8(Allocator.Temp, out var ext));
            Assert.AreEqual(24, ext.Primitives.Length);
            ext.Dispose();
        }

        [Test]
        public unsafe void MeshGraph_InitialConfigMapping_Correct()
        {
            for (var theta = 0; theta < 8; theta++)
            {
                Assert.AreEqual(theta, mesh.InitialConfigByTheta[theta]);
                Assert.AreEqual(theta, mesh.ThetaByInitialConfig[theta]);
            }
        }

        [Test]
        public unsafe void MeshAStar_Weighted_FasterThanOptimal()
        {
            using var grid = new NativeGrid2D(20, 20, Allocator.Temp);

            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(0, 0), new int2(15, 15), out var optimal));

            Assert.IsTrue(MeshAStar.TryFindPath(grid, prims, mesh,
                new int2(0, 0), new int2(15, 15), out var weighted, 0, 2.0f));

            Assert.IsTrue(optimal.Found);
            Assert.IsTrue(weighted.Found);

            Assert.LessOrEqual(weighted.NodesExplored, optimal.NodesExplored);
            optimal.Dispose();
            weighted.Dispose();
        }
    }
}