using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wavestar
{
    /// <summary>
    /// The core Wavestar (Multi-Resolution Theta*) planner.
    ///
    /// Implements any-angle pathfinding on a multi-resolution octree decomposition.
    /// Key ideas from the RSS 2025 paper:
    /// - Operates on subvolumes (octree leaves), not individual vertices
    /// - Each subvolume stores one predecessor + g-cost (lossless compression)
    /// - When updating, compare direct cost vs line-of-sight cost from predecessor
    /// - StrictlyBetter → update; Ambiguous → refine (subdivide); NotBetter → skip
    /// - Epsilon-suboptimality for bounded speedup
    ///
    /// Usage: Schedule the job with the required inputs, then call WavestarPathExtractor
    /// on the resulting cost field.
    /// </summary>
    [BurstCompile]
    public struct MultiResThetaStarJob : IJob
    {
        // ─── Inputs ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Start position in grid coordinates.
        /// </summary>
        public int3 startPos;

        /// <summary>
        /// Goal position in grid coordinates.
        /// </summary>
        public int3 goalPos;

        /// <summary>
        /// Epsilon for bounded suboptimality. epsilon=0 → optimal.
        /// Larger values allow (1+epsilon) relative error per edge for speed.
        /// </summary>
        public float epsilon;

        /// <summary>
        /// Maximum height (coarsest level) in the octree.
        /// If 0, all subvolumes are at finest resolution.
        /// </summary>
        public int maxHeight;

        /// <summary>
        /// Minimum height allowed during refinement.
        /// </summary>
        public int minHeight;

        /// <summary>
        /// Grid dimensions.
        /// </summary>
        public int sizeX;
        public int sizeY;
        public int sizeZ;

        /// <summary>
        /// Flat obstacle grid: 0 = free, 1 = blocked.
        /// </summary>
        public NativeArray<int> obstacleGrid;

        // ─── Outputs ────────────────────────────────────────────────────────────

        /// <summary>
        /// Output cost field: maps morton codes of traversable subvolumes to SubvolumeData.
        /// Must be pre-allocated with sufficient capacity.
        /// </summary>
        public NativeParallelHashMap<int, SubvolumeData> costField;

        /// <summary>
        /// Output: whether a path was found.
        /// </summary>
        public NativeArray<bool> pathFound;

        /// <summary>
        /// Output: final g-cost to goal. float.PositiveInfinity if not found.
        /// </summary>
        public NativeArray<float> goalGCost;

        // ─── Internal ───────────────────────────────────────────────────────────

        private NativeObstacleMap obstacleMap;

        public void Execute()
        {
            obstacleMap = new NativeObstacleMap(obstacleGrid, sizeX, sizeY, sizeZ);
            pathFound[0] = false;
            goalGCost[0] = float.PositiveInfinity;

            // Open set: min-priority queue sorted by f-score
            var open = new NativeMinPQ(Allocator.Temp);
            // Closed set: morton codes of expanded subvolumes
            var closed = new NativeHashSet<int>(sizeX * sizeY, Allocator.Temp);
            // f-score map for duplicate detection
            var fScores = new NativeHashMap<int, float>(sizeX * sizeY, Allocator.Temp);

            // Find the finest-resolution subvolume containing the start
            OctreeIndex startSV = FindFinestSubvolume(startPos, maxHeight);
            OctreeIndex goalSV = FindFinestSubvolume(goalPos, maxHeight);

            // Initialize start subvolume
            float3 startCenter = startSV.Center;
            float startG = 0f;
            float startH = math.distance(startCenter, (float3)goalPos);
            float startF = startG + startH;

            var startData = new SubvolumeData(startPos.x, startPos.y, startPos.z, startG);
            costField.TryAdd(startSV.MortonCode, startData);
            open.Push(new OpenSetElement(startSV, startF));
            fScores[startSV.MortonCode] = startF;

            int iterations = 0;
            int maxIterations = sizeX * sizeY * sizeZ * 4; // safety limit

            while (open.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var current = open.Pop();
                var currentIdx = current.index;

                // Skip if already closed
                if (closed.Contains(currentIdx.MortonCode))
                    continue;

                // Close this subvolume
                closed.Add(currentIdx.MortonCode);

                // Check if goal is inside current subvolume
                if (currentIdx.Contains(goalPos))
                {
                    pathFound[0] = true;
                    if (costField.TryGetValue(currentIdx.MortonCode, out var goalData))
                        goalGCost[0] = goalData.gCost;
                    open.Dispose();
                    closed.Dispose();
                    fScores.Dispose();
                    return;
                }

                // Get current subvolume data
                if (!costField.TryGetValue(currentIdx.MortonCode, out var currentData))
                    continue;

                // Expand neighbors
                var neighbors = CollectNeighbors(currentIdx, closed);
                for (int i = 0; i < neighbors.Length; i++)
                {
                    var neighborIdx = neighbors[i];
                    UpdateSubvolume(ref open, ref fScores, ref closed, currentIdx, currentData, neighborIdx, goalSV);
                }
                neighbors.Dispose();
            }

            // Also try expanding from the goal side by checking goal subvolume
            if (!pathFound[0] && costField.TryGetValue(goalSV.MortonCode, out var gData))
            {
                pathFound[0] = true;
                goalGCost[0] = gData.gCost;
            }

            open.Dispose();
            closed.Dispose();
            fScores.Dispose();
        }

        /// <summary>
        /// Find the finest subvolume containing the given point at or below maxHeight.
        /// Walks from maxHeight down, finding the smallest traversable subvolume.
        /// </summary>
        private OctreeIndex FindFinestSubvolume(int3 pos, int startHeight)
        {
            // Start at the coarsest level that contains the point
            for (int h = startHeight; h >= 0; h--)
            {
                int s = 1 << h;
                int sx = pos.x >> h;
                int sy = pos.y >> h;
                int sz = pos.z >> h;
                var sv = new OctreeIndex(sx, sy, sz, h);
                if (obstacleMap.IsSubvolumeTraversable(sv))
                    return sv;
            }

            // Fallback: finest level
            return new OctreeIndex(pos.x, pos.y, pos.z, 0);
        }

        /// <summary>
        /// Collect all neighboring subvolumes of the given subvolume at the same
        /// or adjacent resolution levels.
        /// </summary>
        private NativeList<OctreeIndex> CollectNeighbors(OctreeIndex idx, NativeHashSet<int> closed)
        {
            var neighbors = new NativeList<OctreeIndex>(Allocator.Temp);

            // For 2D (y=0), we check 8-connected neighbors
            // For 3D, 26-connected
            int s = idx.Size;

            // Check same-resolution neighbors
            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0)
                            continue;

                        int nx = idx.x + dx;
                        int ny = idx.y + dy;
                        int nz = idx.z + dz;

                        // Also check neighbors at different resolutions
                        for (int nh = math.max(idx.height - 1, minHeight); nh <= math.min(idx.height + 1, maxHeight); nh++)
                        {
                            // Convert neighbor coords from current height to target height nh
                            int deltaH = nh - idx.height;
                            int cnx, cny, cnz;
                            if (deltaH >= 0)
                            {
                                // Coarser or same: shift right
                                cnx = nx >> deltaH;
                                cny = ny >> deltaH;
                                cnz = nz >> deltaH;
                            }
                            else
                            {
                                // Finer: shift left
                                cnx = nx << (-deltaH);
                                cny = ny << (-deltaH);
                                cnz = nz << (-deltaH);
                            }

                            var nIdx = new OctreeIndex(cnx, cny, cnz, nh);

                            if (nIdx.x < 0 || nIdx.y < 0 || nIdx.z < 0)
                                continue;
                            if ((nIdx.x + 1) * nIdx.Size > sizeX ||
                                (nIdx.y + 1) * nIdx.Size > sizeY ||
                                (nIdx.z + 1) * nIdx.Size > sizeZ)
                                continue;
                            if (closed.Contains(nIdx.MortonCode))
                                continue;
                            if (!obstacleMap.IsSubvolumeTraversable(nIdx))
                                continue;

                            // Avoid duplicate neighbors
                            bool alreadyAdded = false;
                            for (int j = 0; j < neighbors.Length; j++)
                            {
                                if (neighbors[j].MortonCode == nIdx.MortonCode)
                                {
                                    alreadyAdded = true;
                                    break;
                                }
                            }

                            if (!alreadyAdded)
                                neighbors.Add(nIdx);
                        }
                    }
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Core update step: try to improve neighbor's g-cost via current subvolume V.
        ///
        /// Two candidates:
        /// 1. Direct: g(V) + dist(center(V), center(neighbor))
        /// 2. Line-of-sight: g(V) + dist(predecessor(V), center(neighbor)) [if LOS exists]
        ///
        /// If strictly better → update.
        /// If ambiguous (within epsilon) → refine (subdivide).
        /// If not better → skip.
        /// </summary>
        private void UpdateSubvolume(
            ref NativeMinPQ open,
            ref NativeHashMap<int, float> fScores,
            ref NativeHashSet<int> closed,
            OctreeIndex currentIdx,
            SubvolumeData currentData,
            OctreeIndex neighborIdx,
            OctreeIndex goalSV)
        {
            float3 currentCenter = currentIdx.Center;
            float3 neighborCenter = neighborIdx.Center;

            // Get existing neighbor g-cost (infinity if unvisited)
            float existingG = float.PositiveInfinity;
            if (costField.TryGetValue(neighborIdx.MortonCode, out var existingData))
            {
                existingG = existingData.gCost;
            }

            // Candidate 1: Direct path through current's center
            float directG = currentData.gCost + math.distance(currentCenter, neighborCenter);

            // Candidate 2: Line-of-sight from current's predecessor
            float losG = float.PositiveInfinity;
            int3 losPred = currentData.Predecessor;
            float3 predCenter = currentData.PredecessorCenter;

            if (HasLineOfSight(predCenter, neighborCenter))
            {
                losG = currentData.gCost - math.distance(currentCenter, predCenter)
                       + math.distance(predCenter, neighborCenter);
                // Actually: g from predecessor directly = g(V) - dist(pred, center(V)) + dist(pred, center(neighbor))
                // But g(V) already includes the cost to center(V), so:
                // losG = g(pred) + dist(pred, center(neighbor))
                // g(pred) = g(V) - dist(pred, center(V)) ... but only if V was reached from pred
                // Simpler: we store g(V) which is cost to center(V).
                // Cost from pred center to neighbor center directly:
                // gPred = g(V) - dist(predCenter, currentCenter)  [if V was reached via pred]
                // losG = gPred + dist(predCenter, neighborCenter)
                losG = currentData.gCost
                       - math.distance(predCenter, currentCenter)
                       + math.distance(predCenter, neighborCenter);
            }

            // Pick the better candidate
            float candidateG = math.min(directG, losG);
            int3 candidatePred;
            if (candidateG == losG && losG < directG)
            {
                candidatePred = losPred;
            }
            else
            {
                candidatePred = new int3((int)currentCenter.x, (int)currentCenter.y, (int)currentCenter.z);
            }

            // Compare with existing
            var cmp = CompareCosts(existingG, candidateG);

            switch (cmp)
            {
                case ComparisonResult.StrictlyBetter:
                    // Update the neighbor
                    var newData = new SubvolumeData(candidatePred.x, candidatePred.y, candidatePred.z, candidateG);
                    costField[neighborIdx.MortonCode] = newData;

                    // Compute f-score: f(V) = min_s in V [g(pred) + c(pred, s) + h(s)]
                    // Approximate: use center point
                    float h = math.distance(neighborCenter, (float3)goalPos);
                    float f = candidateG + h;

                    fScores[neighborIdx.MortonCode] = f;
                    open.Push(new OpenSetElement(neighborIdx, f));
                    break;

                case ComparisonResult.Ambiguous:
                    // Try to refine (subdivide) the neighbor
                    if (neighborIdx.height > minHeight)
                {
                    SubdivideAndRepropagate(
                        ref open, ref fScores, ref closed,
                        currentIdx, currentData, neighborIdx, goalSV);
                }
                else
                {
                    // At minimum resolution; just update with the better cost
                    goto case ComparisonResult.StrictlyBetter;
                }
                break;

                case ComparisonResult.NotBetter:
                default:
                    // Skip
                    break;
            }
        }

        /// <summary>
        /// Compare candidate g-cost against existing using epsilon threshold.
        /// </summary>
        private ComparisonResult CompareCosts(float existing, float candidate)
        {
            if (float.IsInfinity(existing))
                return ComparisonResult.StrictlyBetter;

            float threshold = epsilon * math.max(math.abs(existing), math.abs(candidate));
            threshold = math.max(threshold, 1e-6f); // minimum threshold to avoid floating point issues

            float diff = existing - candidate; // positive means candidate is better

            if (diff > threshold)
                return ComparisonResult.StrictlyBetter;
            else if (diff > -threshold)
                return ComparisonResult.Ambiguous;
            else
                return ComparisonResult.NotBetter;
        }

        /// <summary>
        /// Subdivide a subvolume into its 4 (2D) or 8 (3D) children and attempt
        /// to propagate costs to each child independently.
        /// </summary>
        private void SubdivideAndRepropagate(
            ref NativeMinPQ open,
            ref NativeHashMap<int, float> fScores,
            ref NativeHashSet<int> closed,
            OctreeIndex currentIdx,
            SubvolumeData currentData,
            OctreeIndex neighborIdx,
            OctreeIndex goalSV)
        {
            int childCount = (sizeY > 1) ? 8 : 4;
            for (int c = 0; c < childCount; c++)
            {
                var child = neighborIdx.Child(c);

                // Bounds check
                if (child.x < 0 || child.y < 0 || child.z < 0)
                    continue;
                if ((child.x + 1) * child.Size > sizeX ||
                    (child.y + 1) * child.Size > sizeY ||
                    (child.z + 1) * child.Size > sizeZ)
                    continue;

                if (!obstacleMap.IsSubvolumeTraversable(child))
                    continue;
                if (closed.Contains(child.MortonCode))
                    continue;

                // Recursively try to update child
                UpdateSubvolume(ref open, ref fScores, ref closed, currentIdx, currentData, child, goalSV);
            }
        }

        /// <summary>
        /// Line-of-sight check using Bresenham-style 3D ray traversal.
        /// Walks from start to end checking that all traversed cells are free.
        /// </summary>
        private bool HasLineOfSight(float3 from, float3 to)
        {
            // Convert to grid coordinates
            int x0 = (int)math.floor(from.x);
            int y0 = (int)math.floor(from.y);
            int z0 = (int)math.floor(from.z);
            int x1 = (int)math.floor(to.x);
            int y1 = (int)math.floor(to.y);
            int z1 = (int)math.floor(to.z);

            int dx = math.abs(x1 - x0);
            int dy = math.abs(y1 - y0);
            int dz = math.abs(z1 - z0);

            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int sz = z0 < z1 ? 1 : -1;

            // 3D Bresenham
            if (dx >= dy && dx >= dz)
            {
                int errY = 2 * dy - dx;
                int errZ = 2 * dz - dx;
                for (int i = 0; i <= dx; i++)
                {
                    if (!obstacleMap.IsTraversable(x0, y0, z0))
                        return false;
                    if (errY > 0) { y0 += sy; errY -= 2 * dx; }
                    if (errZ > 0) { z0 += sz; errZ -= 2 * dx; }
                    errY += 2 * dy;
                    errZ += 2 * dz;
                    x0 += sx;
                }
            }
            else if (dy >= dx && dy >= dz)
            {
                int errX = 2 * dx - dy;
                int errZ = 2 * dz - dy;
                for (int i = 0; i <= dy; i++)
                {
                    if (!obstacleMap.IsTraversable(x0, y0, z0))
                        return false;
                    if (errX > 0) { x0 += sx; errX -= 2 * dy; }
                    if (errZ > 0) { z0 += sz; errZ -= 2 * dy; }
                    errX += 2 * dx;
                    errZ += 2 * dz;
                    y0 += sy;
                }
            }
            else
            {
                int errX = 2 * dx - dz;
                int errY = 2 * dy - dz;
                for (int i = 0; i <= dz; i++)
                {
                    if (!obstacleMap.IsTraversable(x0, y0, z0))
                        return false;
                    if (errX > 0) { x0 += sx; errX -= 2 * dz; }
                    if (errY > 0) { y0 += sy; errY -= 2 * dz; }
                    errX += 2 * dx;
                    errY += 2 * dy;
                    z0 += sz;
                }
            }

            return true;
        }
    }
}
