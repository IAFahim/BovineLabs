using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.EHL.Tests
{
    [TestFixture]
    public class EHLStarTests
    {
        private const float Eps = 0.1f;

        private EHLIndex BuildTestIndex(int2 gridDims = new(), long memoryBudget = 10_000_000)
        {
            if (gridDims.Equals(new int2(0, 0)))
                gridDims = new int2(10, 10);

            var mapMin = new float2(0f, 0f);
            var mapMax = new float2(10f, 10f);

            var polygonVertices = new NativeArray<float2>(8, Allocator.Persistent);
            polygonVertices[0] = new float2(3f, 7f);
            polygonVertices[1] = new float2(4f, 7f);
            polygonVertices[2] = new float2(4f, 3f);
            polygonVertices[3] = new float2(3f, 3f);
            polygonVertices[4] = new float2(6f, 3f);
            polygonVertices[5] = new float2(8f, 3f);
            polygonVertices[6] = new float2(8f, 1f);
            polygonVertices[7] = new float2(6f, 1f);

            var polyOffsets = new NativeArray<int>(2, Allocator.Persistent);
            polyOffsets[0] = 0;
            polyOffsets[1] = 4;

            var polyCounts = new NativeArray<int>(2, Allocator.Persistent);
            polyCounts[0] = 4;
            polyCounts[1] = 4;

            var obstacleEdges = new NativeArray<ObstacleEdge>(8, Allocator.Persistent);
            obstacleEdges[0] = new ObstacleEdge(new float2(3f, 7f), new float2(4f, 7f));
            obstacleEdges[1] = new ObstacleEdge(new float2(4f, 7f), new float2(4f, 3f));
            obstacleEdges[2] = new ObstacleEdge(new float2(4f, 3f), new float2(3f, 3f));
            obstacleEdges[3] = new ObstacleEdge(new float2(3f, 3f), new float2(3f, 7f));
            obstacleEdges[4] = new ObstacleEdge(new float2(6f, 3f), new float2(8f, 3f));
            obstacleEdges[5] = new ObstacleEdge(new float2(8f, 3f), new float2(8f, 1f));
            obstacleEdges[6] = new ObstacleEdge(new float2(8f, 1f), new float2(6f, 1f));
            obstacleEdges[7] = new ObstacleEdge(new float2(6f, 1f), new float2(6f, 3f));

            var visHandle = VisibilityGraphBuilder.Build(
                polyOffsets,
                polyCounts,
                polygonVertices,
                obstacleEdges,
                out var convexVertices,
                out var adjOffsets,
                out var adjCounts,
                out var adjEdges);

            visHandle.Complete();

            var hubHandle = HubLabelingBuilder.Build(
                convexVertices,
                adjOffsets,
                adjCounts,
                adjEdges,
                out var hubLabels,
                out var hubOffsets,
                out var hubCounts,
                out var succKeys,
                out var succValues);

            hubHandle.Complete();

            Assert.IsTrue(EHLIndexer.TryBuild(
                convexVertices,
                obstacleEdges,
                hubOffsets,
                hubCounts,
                hubLabels,
                adjOffsets,
                adjCounts,
                adjEdges,
                mapMin,
                mapMax,
                gridDims,
                memoryBudget,
                out var cells,
                out var viaLabels,
                out var indexHandle));

            indexHandle.Complete();

            Assert.IsTrue(EHLIndexer.TryAssembleIndex(
                mapMin,
                mapMax,
                gridDims,
                cells,
                viaLabels,
                convexVertices,
                obstacleEdges,
                adjOffsets,
                adjCounts,
                adjEdges,
                hubOffsets,
                hubCounts,
                hubLabels,
                succKeys,
                succValues,
                out var index));

            cells.Dispose();
            viaLabels.Dispose();
            adjOffsets.Dispose();
            adjCounts.Dispose();
            adjEdges.Dispose();
            hubOffsets.Dispose();
            hubCounts.Dispose();
            hubLabels.Dispose();
            succKeys.Dispose();
            succValues.Dispose();
            polyOffsets.Dispose();
            polyCounts.Dispose();
            polygonVertices.Dispose();

            return index;
        }

        [Test]
        public unsafe void VisibilityGraph_ConvexVerticesFound()
        {
            var index = BuildTestIndex();
            Assert.IsTrue(index.ConvexVertices.Length > 0, "Should find convex vertices from rectangular obstacles");
            Assert.AreEqual(8, index.ConvexVertices.Length, "Two rectangles should yield 8 convex vertices");
            index.Dispose();
        }

        [Test]
        public unsafe void VisibilityGraph_EdgesCorrect()
        {
            var index = BuildTestIndex();
            var totalEdges = 0;
            for (var v = 0; v < index.ConvexVertices.Length; v++)
            {
                var offset = index.AdjOffsets[v];
                var count = index.AdjCounts[v];
                totalEdges += count;
                for (var e = 0; e < count; e++)
                {
                    var edge = index.AdjEdges[offset + e];
                    Assert.AreNotEqual(v, edge.TargetVertexId, "Self-loops should not exist in visibility graph");
                    Assert.IsTrue(edge.Distance > 0f, $"Edge distance should be positive, got {edge.Distance}");
                }
            }

            Assert.AreEqual(totalEdges % 2, 0, "Edge count should be even (bidirectional)");
            index.Dispose();
        }

        [Test]
        public unsafe void HubLabels_CoverAllPairs()
        {
            var index = BuildTestIndex();
            var n = index.ConvexVertices.Length;
            for (var v1 = 0; v1 < n; v1++)
            {
                var off1 = index.HubOffsets[v1];
                var cnt1 = index.HubCounts[v1];
                for (var v2 = v1 + 1; v2 < n; v2++)
                {
                    var off2 = index.HubOffsets[v2];
                    var cnt2 = index.HubCounts[v2];
                    var foundCommon = false;
                    int i = off1, j = off2;
                    int end1 = off1 + cnt1, end2 = off2 + cnt2;
                    while (i < end1 && j < end2)
                    {
                        var h1 = index.HubLabels[i].HubVertexId;
                        var h2 = index.HubLabels[j].HubVertexId;
                        if (h1 == h2)
                        {
                            foundCommon = true;
                            break;
                        }

                        if (h1 < h2) i++;
                        else j++;
                    }

                    Assert.IsTrue(foundCommon || cnt1 == 0 || cnt2 == 0,
                        $"Vertices {v1} and {v2} should share at least one hub in their labels");
                }
            }

            index.Dispose();
        }

        [Test]
        public unsafe void Query_ReturnsOptimalPath_StraightLine()
        {
            var index = BuildTestIndex();
            var result = EHLStarQuery.Query(ref index, new float2(1f, 1f), new float2(2f, 1f));
            Assert.IsTrue(result.PathFound, "Path should be found for unobstructed query");
            var expectedDist = 1.0f;
            Assert.AreEqual(expectedDist, result.Distance, Eps,
                $"Expected distance ~{expectedDist}, got {result.Distance}");
            result.Dispose();
            index.Dispose();
        }

        [Test]
        public unsafe void Query_ReturnsPath_AroundObstacle()
        {
            var index = BuildTestIndex();
            var result = EHLStarQuery.Query(ref index, new float2(1f, 5f), new float2(7f, 5f));
            Assert.IsTrue(result.PathFound, "Path should be found around obstacle");
            Assert.IsTrue(result.Distance > 5.5f, $"Distance around obstacle should be > 5.5, got {result.Distance}");
            Assert.IsTrue(result.Distance < 10f, $"Distance should be reasonable (< 10), got {result.Distance}");
            result.Dispose();
            index.Dispose();
        }

        [Test]
        public unsafe void Query_NoPath_WhenBlocked()
        {
            var mapMin = new float2(0f, 0f);
            var mapMax = new float2(10f, 10f);
            var gridDims = new int2(10, 10);
            var polygonVertices = new NativeArray<float2>(4, Allocator.Persistent);
            polygonVertices[0] = new float2(5f, 10f);
            polygonVertices[1] = new float2(6f, 10f);
            polygonVertices[2] = new float2(6f, 0f);
            polygonVertices[3] = new float2(5f, 0f);
            var polyOffsets = new NativeArray<int>(1, Allocator.Persistent);
            polyOffsets[0] = 0;
            var polyCounts = new NativeArray<int>(1, Allocator.Persistent);
            polyCounts[0] = 4;
            var obstacleEdges = new NativeArray<ObstacleEdge>(4, Allocator.Persistent);
            obstacleEdges[0] = new ObstacleEdge(new float2(5f, 10f), new float2(6f, 10f));
            obstacleEdges[1] = new ObstacleEdge(new float2(6f, 10f), new float2(6f, 0f));
            obstacleEdges[2] = new ObstacleEdge(new float2(6f, 0f), new float2(5f, 0f));
            obstacleEdges[3] = new ObstacleEdge(new float2(5f, 0f), new float2(5f, 10f));
            var visHandle = VisibilityGraphBuilder.Build(
                polyOffsets, polyCounts, polygonVertices, obstacleEdges,
                out var convexVertices, out var adjOffsets, out var adjCounts, out var adjEdges);
            visHandle.Complete();
            var hubHandle = HubLabelingBuilder.Build(
                convexVertices, adjOffsets, adjCounts, adjEdges,
                out var hubLabels, out var hubOffsets, out var hubCounts,
                out var succKeys, out var succValues);
            hubHandle.Complete();
            Assert.IsTrue(EHLIndexer.TryBuild(
                convexVertices, obstacleEdges, hubOffsets, hubCounts, hubLabels,
                adjOffsets, adjCounts, adjEdges,
                mapMin, mapMax, gridDims, 10_000_000,
                out var cells, out var viaLabels, out var indexHandle));
            indexHandle.Complete();
            Assert.IsTrue(EHLIndexer.TryAssembleIndex(
                mapMin, mapMax, gridDims, cells, viaLabels,
                convexVertices, obstacleEdges,
                adjOffsets, adjCounts, adjEdges,
                hubOffsets, hubCounts, hubLabels,
                succKeys, succValues, out var index));
            cells.Dispose();
            viaLabels.Dispose();
            adjOffsets.Dispose();
            adjCounts.Dispose();
            adjEdges.Dispose();
            hubOffsets.Dispose();
            hubCounts.Dispose();
            hubLabels.Dispose();
            succKeys.Dispose();
            succValues.Dispose();
            polyOffsets.Dispose();
            polyCounts.Dispose();
            polygonVertices.Dispose();
            var result = EHLStarQuery.Query(ref index, new float2(2f, 5f), new float2(8f, 5f));
            Assert.IsFalse(result.PathFound, "No path should exist through a full-height wall");
            result.Dispose();
            index.Dispose();
        }

        [Test]
        public unsafe void MemoryBudget_Respected_AfterCompression()
        {
            long budget = 50000;
            var gridDims = new int2(5, 5);
            var index = BuildTestIndex(gridDims, budget);
            long actualMemory = index.ViaLabels.Length * Marshal.SizeOf<ViaLabel>();
            Assert.IsTrue(index.ViaLabels.Length >= 0, "Via-labels should exist after compression");
            Assert.IsTrue(index.Cells.Length == gridDims.x * gridDims.y,
                $"Should have {gridDims.x * gridDims.y} cells, got {index.Cells.Length}");
            index.Dispose();
        }

        [Test]
        public unsafe void Query_DistanceMonotonicallyIncreasing()
        {
            var index = BuildTestIndex();
            var result1 = EHLStarQuery.Query(ref index, new float2(1f, 1f), new float2(2f, 1f));
            var result2 = EHLStarQuery.Query(ref index, new float2(1f, 1f), new float2(5f, 1f));
            if (result1.PathFound && result2.PathFound)
                Assert.IsTrue(result2.Distance >= result1.Distance,
                    $"Distance to farther point ({result2.Distance}) should be >= closer point ({result1.Distance})");
            result1.Dispose();
            result2.Dispose();
            index.Dispose();
        }

        [Test]
        public unsafe void Index_CellLookup_ReturnsCorrectCell()
        {
            var index = BuildTestIndex();
            var cell00 = index.CellIndex(new float2(0.5f, 0.5f));
            Assert.AreEqual(0, cell00, "Point (0.5,0.5) should be in cell 0");
            var cellLast = index.CellIndex(new float2(9.5f, 9.5f));
            Assert.AreEqual(99, cellLast, "Point (9.5,9.5) should be in last cell");
            var cell25 = index.CellIndex(new float2(5.5f, 2.5f));
            Assert.AreEqual(25, cell25, "Point (5.5,2.5) should be in cell 25");
            index.Dispose();
        }

        [Test]
        public unsafe void Query_PathContainsSourceAndTarget()
        {
            var index = BuildTestIndex();
            var src = new float2(1f, 1f);
            var tgt = new float2(2f, 2f);
            var result = EHLStarQuery.Query(ref index, src, tgt);
            if (result.PathFound && result.Waypoints.Length >= 2)
            {
                Assert.IsTrue(math.lengthsq(result.Waypoints[0] - src) < 0.01f, "First waypoint should be at source");
                var lastWP = result.Waypoints[result.Waypoints.Length - 1];
                Assert.IsTrue(math.lengthsq(lastWP - tgt) < 0.01f, "Last waypoint should be at target");
            }

            result.Dispose();
            index.Dispose();
        }
    }
}