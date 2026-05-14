using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.EHL
{
    public static class EHLIndexer
    {
        public static bool TryBuild(
            NativeArray<ConvexVertex> convexVertices,
            NativeArray<ObstacleEdge> obstacleEdges,
            NativeArray<int> hubOffsets,
            NativeArray<int> hubCounts,
            NativeArray<VisibilityLabel> hubLabels,
            NativeArray<int> adjOffsets,
            NativeArray<int> adjCounts,
            NativeArray<AdjEdge> adjEdges,
            float2 mapMin,
            float2 mapMax,
            int2 gridDims,
            long memoryBudgetBytes,
            out NativeList<GridCell> cellsOut,
            out NativeList<ViaLabel> viaLabelsOut,
            out JobHandle jobHandle)
        {
            cellsOut = new NativeList<GridCell>(Allocator.Persistent);
            viaLabelsOut = new NativeList<ViaLabel>(Allocator.Persistent);

            var job = new EHLIndexerJob
            {
                ConvexVertices = convexVertices,
                ObstacleEdges = obstacleEdges,
                HubOffsets = hubOffsets,
                HubCounts = hubCounts,
                HubLabels = hubLabels,
                AdjOffsets = adjOffsets,
                AdjCounts = adjCounts,
                AdjEdges = adjEdges,
                MapMin = mapMin,
                MapMax = mapMax,
                GridDims = gridDims,
                CellSize = (mapMax - mapMin) / new float2(gridDims.x, gridDims.y),
                MemoryBudgetBytes = memoryBudgetBytes,
                CellsOut = cellsOut,
                ViaLabelsOut = viaLabelsOut
            };

            jobHandle = job.Schedule();
            return true;
        }

        public static bool TryAssembleIndex(
            float2 mapMin,
            float2 mapMax,
            int2 gridDims,
            NativeList<GridCell> cells,
            NativeList<ViaLabel> viaLabels,
            NativeArray<ConvexVertex> convexVertices,
            NativeArray<ObstacleEdge> obstacleEdges,
            NativeArray<int> adjOffsets,
            NativeArray<int> adjCounts,
            NativeArray<AdjEdge> adjEdges,
            NativeArray<int> hubOffsets,
            NativeArray<int> hubCounts,
            NativeArray<VisibilityLabel> hubLabels,
            NativeList<long> succKeys,
            NativeList<int> succValues,
            out EHLIndex index)
        {
            var successorMap = new NativeHashMap<long, int>(succKeys.Length, Allocator.Persistent);
            for (var i = 0; i < succKeys.Length; i++)
                successorMap.TryAdd(succKeys[i], succValues[i]);

            index = new EHLIndex
            {
                MapMin = mapMin,
                MapMax = mapMax,
                GridDims = gridDims,
                CellSize = (mapMax - mapMin) / new float2(gridDims.x, gridDims.y),
                Cells = new NativeArray<GridCell>(cells.AsArray(), Allocator.Persistent),
                ViaLabels = new NativeArray<ViaLabel>(viaLabels.AsArray(), Allocator.Persistent),
                ConvexVertices = new NativeArray<ConvexVertex>(convexVertices, Allocator.Persistent),
                ObstacleEdges = new NativeArray<ObstacleEdge>(obstacleEdges, Allocator.Persistent),
                AdjOffsets = new NativeArray<int>(adjOffsets, Allocator.Persistent),
                AdjCounts = new NativeArray<int>(adjCounts, Allocator.Persistent),
                AdjEdges = new NativeArray<AdjEdge>(adjEdges, Allocator.Persistent),
                HubOffsets = new NativeArray<int>(hubOffsets, Allocator.Persistent),
                HubCounts = new NativeArray<int>(hubCounts, Allocator.Persistent),
                HubLabels = new NativeArray<VisibilityLabel>(hubLabels, Allocator.Persistent),
                SuccessorMap = successorMap
            };

            return true;
        }
    }

    [BurstCompile]
    public struct EHLIndexerJob : IJob
    {
        public NativeArray<ConvexVertex> ConvexVertices;
        public NativeArray<ObstacleEdge> ObstacleEdges;

        public NativeArray<int> HubOffsets;
        public NativeArray<int> HubCounts;
        public NativeArray<VisibilityLabel> HubLabels;

        public NativeArray<int> AdjOffsets;
        public NativeArray<int> AdjCounts;
        public NativeArray<AdjEdge> AdjEdges;

        public float2 MapMin;
        public float2 MapMax;
        public int2 GridDims;
        public float2 CellSize;

        public long MemoryBudgetBytes;

        public NativeList<GridCell> CellsOut;
        public NativeList<ViaLabel> ViaLabelsOut;

        public unsafe void Execute()
        {
            var numCells = GridDims.x * GridDims.y;
            var numVertices = ConvexVertices.Length;

            // Maintain an array of pointers to ViaLabels UnsafeLists for merging
            var cellLabels = (UnsafeList<ViaLabel>**)UnsafeUtility.Malloc(sizeof(IntPtr) * numCells,
                UnsafeUtility.AlignOf<IntPtr>(), Allocator.Temp);

            var labelSet = new NativeHashMap<int, ViaLabel>(256, Allocator.Temp);
            var samplePoints = new NativeList<float2>(5, Allocator.Temp);
            var visibleVertices = new NativeList<int>(128, Allocator.Temp);

            for (var cy = 0; cy < GridDims.y; cy++)
            for (var cx = 0; cx < GridDims.x; cx++)
            {
                var cellIdx = cy * GridDims.x + cx;
                var localLabels = new UnsafeList<ViaLabel>(64, Allocator.Temp);

                labelSet.Clear();
                samplePoints.Clear();
                visibleVertices.Clear();

                var cellMin = MapMin + new float2(cx * CellSize.x, cy * CellSize.y);
                var cellMax = cellMin + CellSize;
                var cellCenter = (cellMin + cellMax) * 0.5f;

                samplePoints.Add(cellCenter);
                samplePoints.Add(cellMin + CellSize * new float2(0.25f, 0.25f));
                samplePoints.Add(cellMin + CellSize * new float2(0.75f, 0.25f));
                samplePoints.Add(cellMin + CellSize * new float2(0.25f, 0.75f));
                samplePoints.Add(cellMin + CellSize * new float2(0.75f, 0.75f));

                for (var v = 0; v < numVertices; v++)
                {
                    var vPos = ConvexVertices[v].Position;
                    var canSee = false;

                    for (var s = 0; s < samplePoints.Length; s++)
                        if (IsVisible(vPos, samplePoints[s], ObstacleEdges))
                        {
                            canSee = true;
                            break;
                        }

                    if (canSee) visibleVertices.Add(v);
                }

                for (var vi = 0; vi < visibleVertices.Length; vi++)
                {
                    var v = visibleVertices[vi];
                    var vPos = ConvexVertices[v].Position;
                    var distToCell = math.distance(cellCenter, vPos);

                    var hubStart = HubOffsets[v];
                    var hubCount = HubCounts[v];

                    for (var h = 0; h < hubCount; h++)
                    {
                        var label = HubLabels[hubStart + h];
                        var totalDist = distToCell + label.Distance;

                        var viaLabel = new ViaLabel(
                            label.HubVertexId,
                            totalDist,
                            label.ViaVertexId,
                            v
                        );

                        if (labelSet.TryGetValue(label.HubVertexId, out var existing))
                        {
                            if (totalDist < existing.HubDistance)
                                labelSet[label.HubVertexId] = viaLabel;
                        }
                        else
                        {
                            labelSet.TryAdd(label.HubVertexId, viaLabel);
                        }
                    }
                }

                var values = labelSet.GetValueArray(Allocator.Temp);
                values.Sort();

                for (var i = 0; i < values.Length; i++)
                    localLabels.Add(values[i]);

                var persistentList = (UnsafeList<ViaLabel>*)UnsafeUtility.Malloc(sizeof(UnsafeList<ViaLabel>),
                    UnsafeUtility.AlignOf<UnsafeList<ViaLabel>>(), Allocator.Temp);
                *persistentList = localLabels;
                cellLabels[cellIdx] = persistentList;

                values.Dispose();
            }

            labelSet.Dispose();
            samplePoints.Dispose();
            visibleVertices.Dispose();

            long currentMemory = 0;
            for (var i = 0; i < numCells; i++)
                currentMemory += cellLabels[i]->Length * UnsafeUtility.SizeOf<ViaLabel>();

            var activeCells = new NativeArray<bool>(numCells, Allocator.Temp);
            var cellMergedInto = new NativeArray<int>(numCells, Allocator.Temp);
            for (var i = 0; i < numCells; i++)
            {
                activeCells[i] = true;
                cellMergedInto[i] = i;
            }

            if (currentMemory > MemoryBudgetBytes)
            {
                var maxIterations = numCells;
                var iter = 0;

                while (currentMemory > MemoryBudgetBytes && iter < maxIterations)
                {
                    int bestA = -1, bestB = -1;
                    var bestOverlap = -1f;

                    for (var cy = 0; cy < GridDims.y; cy++)
                    for (var cx = 0; cx < GridDims.x; cx++)
                    {
                        var idx = cy * GridDims.x + cx;
                        if (!activeCells[idx]) continue;

                        if (cx + 1 < GridDims.x)
                        {
                            var right = cy * GridDims.x + cx + 1;
                            if (activeCells[right])
                            {
                                var overlap = ComputeOverlap(cellLabels[idx], cellLabels[right]);
                                if (overlap > bestOverlap)
                                {
                                    bestOverlap = overlap;
                                    bestA = idx;
                                    bestB = right;
                                }
                            }
                        }

                        if (cy + 1 < GridDims.y)
                        {
                            var top = (cy + 1) * GridDims.x + cx;
                            if (activeCells[top])
                            {
                                var overlap = ComputeOverlap(cellLabels[idx], cellLabels[top]);
                                if (overlap > bestOverlap)
                                {
                                    bestOverlap = overlap;
                                    bestA = idx;
                                    bestB = top;
                                }
                            }
                        }
                    }

                    if (bestA < 0 || bestOverlap <= 0f) break;

                    var merged = MergeLabels(cellLabels[bestA], cellLabels[bestB]);

                    currentMemory -= cellLabels[bestA]->Length * UnsafeUtility.SizeOf<ViaLabel>();
                    currentMemory -= cellLabels[bestB]->Length * UnsafeUtility.SizeOf<ViaLabel>();

                    cellLabels[bestA]->Dispose();
                    cellLabels[bestB]->Dispose();

                    *cellLabels[bestA] = merged;
                    currentMemory += merged.Length * UnsafeUtility.SizeOf<ViaLabel>();

                    activeCells[bestB] = false;
                    cellMergedInto[bestB] = bestA;

                    iter++;
                }
            }

            var labelOffset = 0;
            for (var cy = 0; cy < GridDims.y; cy++)
            for (var cx = 0; cx < GridDims.x; cx++)
            {
                var cellIdx = cy * GridDims.x + cx;

                var actualCell = cellIdx;
                while (!activeCells[actualCell])
                    actualCell = cellMergedInto[actualCell];

                var cellMin = MapMin + new float2(cx * CellSize.x, cy * CellSize.y);
                var cellMax = cellMin + CellSize;

                var labels = cellLabels[actualCell];
                var count = labels->Length;

                CellsOut.Add(new GridCell(cellMin, cellMax, labelOffset, count));
                labelOffset += count;

                for (var i = 0; i < count; i++)
                    ViaLabelsOut.Add(labels->Ptr[i]);
            }

            for (var i = 0; i < numCells; i++)
            {
                if (activeCells[i])
                    cellLabels[i]->Dispose();
                UnsafeUtility.Free(cellLabels[i], Allocator.Temp);
            }

            UnsafeUtility.Free(cellLabels, Allocator.Temp);
            activeCells.Dispose();
            cellMergedInto.Dispose();
        }

        private bool IsVisible(float2 a, float2 b, NativeArray<ObstacleEdge> edges)
        {
            var ab = b - a;
            var lenSq = math.lengthsq(ab);
            if (lenSq < 1e-10f) return true;

            for (var e = 0; e < edges.Length; e++)
            {
                var c = edges[e].A;
                var d = edges[e].B;

                var d1 = ab;
                var d2 = d - c;
                var cross = d1.x * d2.y - d1.y * d2.x;

                const float eps = 1e-10f;
                if (math.abs(cross) < eps) continue;

                var d3 = c - a;
                var t = (d3.x * d2.y - d3.y * d2.x) / cross;
                var u = (d3.x * d1.y - d3.y * d1.x) / cross;

                const float margin = 1e-5f;
                if (t > margin && t < 1.0f - margin && u > margin && u < 1.0f - margin)
                    return false;
            }

            return true;
        }

        private unsafe float ComputeOverlap(UnsafeList<ViaLabel>* a, UnsafeList<ViaLabel>* b)
        {
            if (a->Length == 0 || b->Length == 0) return 0f;

            var maxHubId = math.max(a->Ptr[a->Length - 1].HubVertexId, b->Ptr[b->Length - 1].HubVertexId);
            if (maxHubId < 64)
            {
                ulong maskA = 0;
                for (var i = 0; i < a->Length; i++) maskA |= 1UL << a->Ptr[i].HubVertexId;

                ulong maskB = 0;
                for (var j = 0; j < b->Length; j++) maskB |= 1UL << b->Ptr[j].HubVertexId;

                var intersection = maskA & maskB;
                var union = maskA | maskB;

                var sharedBits = math.countbits(intersection);
                var unionBits = math.countbits(union);

                return unionBits > 0 ? (float)sharedBits / unionBits : 0f;
            }

            var numWords = (maxHubId >> 6) + 1;
            var maskA2 = (ulong*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ulong>() * numWords,
                UnsafeUtility.AlignOf<ulong>(), Allocator.Temp);
            var maskB2 = (ulong*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ulong>() * numWords,
                UnsafeUtility.AlignOf<ulong>(), Allocator.Temp);
            UnsafeUtility.MemSet(maskA2, 0, (long)numWords * UnsafeUtility.SizeOf<ulong>());
            UnsafeUtility.MemSet(maskB2, 0, (long)numWords * UnsafeUtility.SizeOf<ulong>());

            for (var i = 0; i < a->Length; i++)
            {
                var id = a->Ptr[i].HubVertexId;
                maskA2[id >> 6] |= 1UL << (id & 63);
            }

            for (var j = 0; j < b->Length; j++)
            {
                var id = b->Ptr[j].HubVertexId;
                maskB2[id >> 6] |= 1UL << (id & 63);
            }

            var shared = 0;
            var totalUnion = 0;
            for (var w = 0; w < numWords; w++)
            {
                shared += math.countbits(maskA2[w] & maskB2[w]);
                totalUnion += math.countbits(maskA2[w] | maskB2[w]);
            }

            UnsafeUtility.Free(maskA2, Allocator.Temp);
            UnsafeUtility.Free(maskB2, Allocator.Temp);

            return totalUnion > 0 ? (float)shared / totalUnion : 0f;
        }

        private unsafe UnsafeList<ViaLabel> MergeLabels(UnsafeList<ViaLabel>* a, UnsafeList<ViaLabel>* b)
        {
            var result = new UnsafeList<ViaLabel>(a->Length + b->Length, Allocator.Temp);

            int i = 0, j = 0;
            while (i < a->Length && j < b->Length)
                if (a->Ptr[i].HubVertexId == b->Ptr[j].HubVertexId)
                {
                    result.Add(a->Ptr[i].HubDistance <= b->Ptr[j].HubDistance ? a->Ptr[i] : b->Ptr[j]);
                    i++;
                    j++;
                }
                else if (a->Ptr[i].HubVertexId < b->Ptr[j].HubVertexId)
                {
                    result.Add(a->Ptr[i]);
                    i++;
                }
                else
                {
                    result.Add(b->Ptr[j]);
                    j++;
                }

            while (i < a->Length)
            {
                result.Add(a->Ptr[i]);
                i++;
            }

            while (j < b->Length)
            {
                result.Add(b->Ptr[j]);
                j++;
            }

            return result;
        }
    }
}