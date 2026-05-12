using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.EHL
{
    /// <summary>
    /// Phase 3 and 4 of EHL*: Build the EHL grid overlay and apply memory-budgeted compression.
    ///
    /// Phase 3: Overlay a uniform grid on the map. For each grid cell c, determine which
    /// convex vertices can "see into" c (visibility list Lc). Compute via-labels VL(c) from
    /// the hub labels of visible vertices.
    ///
    /// Phase 4 (EHL* innovation): Memory-budgeted compression. Compute label overlap between
    /// adjacent cells. Use a max-heap to greedily merge cells with highest overlap until
    /// total memory usage falls within budget B.
    /// </summary>
    [BurstCompile]
    public struct EHLIndexerJob : IJob
    {
        public NativeArray<ConvexVertex> ConvexVertices;
        public NativeArray<ObstacleEdge> ObstacleEdges;

        // Hub labels
        public NativeArray<int> HubOffsets;
        public NativeArray<int> HubCounts;
        public NativeArray<VisibilityLabel> HubLabels;

        // Visibility graph adjacency
        public NativeArray<int> AdjOffsets;
        public NativeArray<int> AdjCounts;
        public NativeArray<AdjEdge> AdjEdges;

        // Grid parameters
        public float2 MapMin;
        public float2 MapMax;
        public int2 GridDims;
        public float2 CellSize;

        /// <summary>Memory budget in bytes for the via-label array.</summary>
        public long MemoryBudgetBytes;

        /// <summary>Output: grid cells.</summary>
        public NativeList<GridCell> CellsOut;

        /// <summary>Output: via-labels (concatenated for all cells).</summary>
        public NativeList<ViaLabel> ViaLabelsOut;

        public void Execute()
        {
            int numCells = GridDims.x * GridDims.y;
            int numVertices = ConvexVertices.Length;

            // Phase 3: Compute via-labels for each cell
            // For each cell, determine which convex vertices can see into it
            var cellLabels = new NativeArray<NativeList<ViaLabel>>(numCells, Allocator.Temp);

            for (int cy = 0; cy < GridDims.y; cy++)
            {
                for (int cx = 0; cx < GridDims.x; cx++)
                {
                    int cellIdx = cy * GridDims.x + cx;
                    cellLabels[cellIdx] = new NativeList<ViaLabel>(Allocator.Temp);

                    float2 cellMin = MapMin + new float2(cx * CellSize.x, cy * CellSize.y);
                    float2 cellMax = cellMin + CellSize;
                    float2 cellCenter = (cellMin + cellMax) * 0.5f;

                    // Sample points within the cell for visibility checking
                    // Use cell center as primary sample
                    var samplePoints = new NativeList<float2>(Allocator.Temp);
                    samplePoints.Add(cellCenter);
                    // Add corner samples for robustness
                    samplePoints.Add(cellMin + CellSize * new float2(0.25f, 0.25f));
                    samplePoints.Add(cellMin + CellSize * new float2(0.75f, 0.25f));
                    samplePoints.Add(cellMin + CellSize * new float2(0.25f, 0.75f));
                    samplePoints.Add(cellMin + CellSize * new float2(0.75f, 0.75f));

                    // For each convex vertex, check if it can see into this cell
                    var visibleVertices = new NativeList<int>(Allocator.Temp);

                    for (int v = 0; v < numVertices; v++)
                    {
                        float2 vPos = ConvexVertices[v].Position;
                        bool canSee = false;

                        for (int s = 0; s < samplePoints.Length; s++)
                        {
                            if (IsVisible(vPos, samplePoints[s], ObstacleEdges))
                            {
                                canSee = true;
                                break;
                            }
                        }

                        if (canSee)
                        {
                            visibleVertices.Add(v);
                        }
                    }

                    // Compute via-labels from visible vertices' hub labels
                    // For each visible vertex v, add all of v's hub labels as via-labels
                    // vdist(cellPoint, hub) = |cellCenter - v| + dist(v, hub)
                    var labelSet = new NativeHashMap<int, ViaLabel>(64, Allocator.Temp);

                    for (int vi = 0; vi < visibleVertices.Length; vi++)
                    {
                        int v = visibleVertices[vi];
                        float2 vPos = ConvexVertices[v].Position;
                        float distToCell = math.distance(cellCenter, vPos);

                        int hubStart = HubOffsets[v];
                        int hubCount = HubCounts[v];

                        for (int h = 0; h < hubCount; h++)
                        {
                            var label = HubLabels[hubStart + h];
                            float totalDist = distToCell + label.Distance;

                            // Keep the via-label with minimum total distance for each hub
                            var viaLabel = new ViaLabel(
                                label.HubVertexId,
                                totalDist,
                                label.ViaVertexId,
                                v  // the visible vertex
                            );

                            if (labelSet.TryGetValue(label.HubVertexId, out var existing))
                            {
                                if (totalDist < existing.HubDistance)
                                {
                                    labelSet[label.HubVertexId] = viaLabel;
                                }
                            }
                            else
                            {
                                labelSet.TryAdd(label.HubVertexId, viaLabel);
                            }
                        }
                    }

                    // Extract and sort by hub ID
                    var values = labelSet.GetValueArray(Allocator.Temp);
                    values.Sort();

                    for (int i = 0; i < values.Length; i++)
                    {
                        cellLabels[cellIdx].Add(values[i]);
                    }

                    values.Dispose();
                    labelSet.Dispose();
                    visibleVertices.Dispose();
                    samplePoints.Dispose();
                }
            }

            // Phase 4: Memory-budgeted compression
            // Calculate current memory usage
            long currentMemory = 0;
            for (int i = 0; i < numCells; i++)
            {
                currentMemory += cellLabels[i].Length * UnsafeUtility.SizeOf<ViaLabel>();
            }

            // Track which cells are still active (not merged into a neighbor)
            var activeCells = new NativeArray<bool>(numCells, Allocator.Temp);
            var cellMergedInto = new NativeArray<int>(numCells, Allocator.Temp);
            for (int i = 0; i < numCells; i++)
            {
                activeCells[i] = true;
                cellMergedInto[i] = i;
            }

            // Build merge candidate heap (pairs of adjacent cells with overlap scores)
            if (currentMemory > MemoryBudgetBytes)
            {
                int maxIterations = numCells; // safety bound
                int iter = 0;

                while (currentMemory > MemoryBudgetBytes && iter < maxIterations)
                {
                    // Find the pair of adjacent active cells with highest label overlap
                    int bestA = -1, bestB = -1;
                    float bestOverlap = -1f;

                    for (int cy = 0; cy < GridDims.y; cy++)
                    {
                        for (int cx = 0; cx < GridDims.x; cx++)
                        {
                            int idx = cy * GridDims.x + cx;
                            if (!activeCells[idx]) continue;

                            // Check right neighbor
                            if (cx + 1 < GridDims.x)
                            {
                                int right = cy * GridDims.x + (cx + 1);
                                if (activeCells[right])
                                {
                                    float overlap = ComputeOverlap(cellLabels[idx], cellLabels[right]);
                                    if (overlap > bestOverlap)
                                    {
                                        bestOverlap = overlap;
                                        bestA = idx;
                                        bestB = right;
                                    }
                                }
                            }

                            // Check top neighbor
                            if (cy + 1 < GridDims.y)
                            {
                                int top = (cy + 1) * GridDims.x + cx;
                                if (activeCells[top])
                                {
                                    float overlap = ComputeOverlap(cellLabels[idx], cellLabels[top]);
                                    if (overlap > bestOverlap)
                                    {
                                        bestOverlap = overlap;
                                        bestA = idx;
                                        bestB = top;
                                    }
                                }
                            }
                        }
                    }

                    if (bestA < 0 || bestOverlap <= 0f)
                        break;

                    // Merge bestB into bestA: union of labels, keeping minimum distance per hub
                    var merged = MergeLabels(cellLabels[bestA], cellLabels[bestB]);

                    // Update memory accounting
                    currentMemory -= cellLabels[bestA].Length * UnsafeUtility.SizeOf<ViaLabel>();
                    currentMemory -= cellLabels[bestB].Length * UnsafeUtility.SizeOf<ViaLabel>();

                    cellLabels[bestA].Dispose();
                    cellLabels[bestB].Dispose();
                    cellLabels[bestA] = merged;

                    currentMemory += merged.Length * UnsafeUtility.SizeOf<ViaLabel>();

                    // Expand bestA's cell bounds to encompass bestB
                    activeCells[bestB] = false;
                    cellMergedInto[bestB] = bestA;

                    iter++;
                }
            }

            // Write output: flatten cell labels into output arrays
            int labelOffset = 0;
            for (int cy = 0; cy < GridDims.y; cy++)
            {
                for (int cx = 0; cx < GridDims.x; cx++)
                {
                    int cellIdx = cy * GridDims.x + cx;

                    // Find the actual active cell (follow merge chain)
                    int actualCell = cellIdx;
                    while (!activeCells[actualCell])
                    {
                        actualCell = cellMergedInto[actualCell];
                    }

                    // Compute merged cell bounds
                    float2 cellMin = MapMin + new float2(cx * CellSize.x, cy * CellSize.y);
                    float2 cellMax = cellMin + CellSize;

                    var labels = cellLabels[actualCell];
                    int count = labels.Length;

                    CellsOut.Add(new GridCell(cellMin, cellMax, labelOffset, count));
                    labelOffset += count;

                    for (int i = 0; i < count; i++)
                    {
                        ViaLabelsOut.Add(labels[i]);
                    }

                    // Only dispose the list from the actual active cell owner
                    // (cellIdx == actualCell means this is the owner)
                }
            }

            // Cleanup
            for (int i = 0; i < numCells; i++)
            {
                if (cellLabels[i].IsCreated)
                    cellLabels[i].Dispose();
            }

            cellLabels.Dispose();
            activeCells.Dispose();
            cellMergedInto.Dispose();
        }

        /// <summary>
        /// Check if point b is visible from point a (no obstacle edge blocks line of sight).
        /// </summary>
        private bool IsVisible(float2 a, float2 b, NativeArray<ObstacleEdge> edges)
        {
            float2 ab = b - a;
            float lenSq = math.lengthsq(ab);
            if (lenSq < 1e-10f) return true;

            for (int e = 0; e < edges.Length; e++)
            {
                float2 c = edges[e].A;
                float2 d = edges[e].B;

                float2 d1 = ab;
                float2 d2 = d - c;
                float cross = d1.x * d2.y - d1.y * d2.x;

                const float eps = 1e-10f;
                if (math.abs(cross) < eps) continue;

                float2 d3 = c - a;
                float t = (d3.x * d2.y - d3.y * d2.x) / cross;
                float u = (d3.x * d1.y - d3.y * d1.x) / cross;

                const float margin = 1e-5f;
                if (t > margin && t < 1.0f - margin && u > margin && u < 1.0f - margin)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compute overlap score between two sorted via-label lists.
        /// Score = fraction of shared hub IDs (Jaccard-like).
        /// </summary>
        private float ComputeOverlap(NativeList<ViaLabel> a, NativeList<ViaLabel> b)
        {
            if (a.Length == 0 || b.Length == 0) return 0f;

            int shared = 0;
            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                if (a[i].HubVertexId == b[j].HubVertexId)
                {
                    shared++;
                    i++;
                    j++;
                }
                else if (a[i].HubVertexId < b[j].HubVertexId)
                {
                    i++;
                }
                else
                {
                    j++;
                }
            }

            int total = a.Length + b.Length - shared;
            return total > 0 ? (float)shared / total : 0f;
        }

        /// <summary>
        /// Merge two sorted via-label lists, keeping the minimum distance per hub ID.
        /// </summary>
        private NativeList<ViaLabel> MergeLabels(NativeList<ViaLabel> a, NativeList<ViaLabel> b)
        {
            var result = new NativeList<ViaLabel>(Allocator.Temp);

            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                if (a[i].HubVertexId == b[j].HubVertexId)
                {
                    // Keep the one with smaller distance
                    result.Add(a[i].HubDistance <= b[j].HubDistance ? a[i] : b[j]);
                    i++;
                    j++;
                }
                else if (a[i].HubVertexId < b[j].HubVertexId)
                {
                    result.Add(a[i]);
                    i++;
                }
                else
                {
                    result.Add(b[j]);
                    j++;
                }
            }

            while (i < a.Length)
            {
                result.Add(a[i]);
                i++;
            }

            while (j < b.Length)
            {
                result.Add(b[j]);
                j++;
            }

            return result;
        }
    }

    /// <summary>
    /// High-level wrapper for EHL index construction.
    /// </summary>
    public static class EHLIndexer
    {
        public static JobHandle Build(
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
            out NativeList<GridCell> cells,
            out NativeList<ViaLabel> viaLabels,
            JobHandle dependency = default)
        {
            cells = new NativeList<GridCell>(Allocator.Persistent);
            viaLabels = new NativeList<ViaLabel>(Allocator.Persistent);

            float2 cellSize = (mapMax - mapMin) / new float2(gridDims.x, gridDims.y);

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
                CellSize = cellSize,
                MemoryBudgetBytes = memoryBudgetBytes,
                CellsOut = cells,
                ViaLabelsOut = viaLabels,
            };

            return job.Schedule(dependency);
        }

        /// <summary>
        /// Assemble the final EHLIndex struct from all computed data.
        /// </summary>
        public static EHLIndex AssembleIndex(
            float2 mapMin,
            float2 mapMax,
            int2 gridDims,
            NativeList<GridCell> cells,
            NativeList<ViaLabel> viaLabels,
            NativeArray<ConvexVertex> convexVertices,
            NativeArray<ObstacleEdge> obstacleEdges,
            NativeList<int> adjOffsets,
            NativeList<int> adjCounts,
            NativeList<AdjEdge> adjEdges,
            NativeList<int> hubOffsets,
            NativeList<int> hubCounts,
            NativeList<VisibilityLabel> hubLabels,
            NativeList<long> succKeys,
            NativeList<int> succValues)
        {
            float2 cellSize = (mapMax - mapMin) / new float2(gridDims.x, gridDims.y);

            var successorMap = new NativeHashMap<long, int>(succKeys.Length, Allocator.Persistent);
            for (int i = 0; i < succKeys.Length; i++)
            {
                successorMap.TryAdd(succKeys[i], succValues[i]);
            }

            return new EHLIndex
            {
                MapMin = mapMin,
                MapMax = mapMax,
                GridDims = gridDims,
                CellSize = cellSize,
                Cells = cells.ToNativeArray(Allocator.Persistent),
                ViaLabels = viaLabels.ToNativeArray(Allocator.Persistent),
                ConvexVertices = convexVertices,
                ObstacleEdges = obstacleEdges,
                AdjOffsets = adjOffsets.ToNativeArray(Allocator.Persistent),
                AdjCounts = adjCounts.ToNativeArray(Allocator.Persistent),
                AdjEdges = adjEdges.ToNativeArray(Allocator.Persistent),
                HubOffsets = hubOffsets.ToNativeArray(Allocator.Persistent),
                HubCounts = hubCounts.ToNativeArray(Allocator.Persistent),
                HubLabels = hubLabels.ToNativeArray(Allocator.Persistent),
                SuccessorMap = successorMap,
            };
        }
    }

    internal static class NativeListExtensions
    {
        public static NativeArray<T> ToNativeArray<T>(this NativeList<T> list, Allocator allocator)
            where T : unmanaged
        {
            var arr = new NativeArray<T>(list.Length, allocator);
            for (int i = 0; i < list.Length; i++)
                arr[i] = list[i];
            return arr;
        }
    }
}
