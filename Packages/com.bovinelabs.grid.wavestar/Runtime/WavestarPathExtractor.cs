using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wavestar
{
    /// <summary>
    /// Extracts a smooth any-angle path from the Wavestar cost field.
    ///
    /// Steps:
    /// 1. Find the goal subvolume in the cost field and trace predecessors back to start.
    /// 2. Apply Theta*-style line-of-sight shortcutting to remove unnecessary waypoints.
    /// 3. Output a NativeList of float3 waypoints.
    ///
    /// The result is a taut any-angle path: straight lines between waypoints that
    /// cut corners wherever line-of-sight permits.
    /// </summary>
    [BurstCompile]
    public struct WavestarPathExtractorJob : IJob
    {
        // ─── Inputs ─────────────────────────────────────────────────────────────

        /// <summary>
        /// The cost field produced by MultiResThetaStarJob.
        /// Maps morton codes to SubvolumeData.
        /// </summary>
        public NativeParallelHashMap<int, SubvolumeData> costField;

        /// <summary>
        /// Start position in grid coordinates.
        /// </summary>
        public int3 startPos;

        /// <summary>
        /// Goal position in grid coordinates.
        /// </summary>
        public int3 goalPos;

        /// <summary>
        /// Obstacle grid for line-of-sight checks during smoothing.
        /// </summary>
        public NativeArray<int> obstacleGrid;

        public int sizeX;
        public int sizeY;
        public int sizeZ;

        // ─── Outputs ────────────────────────────────────────────────────────────

        /// <summary>
        /// Output path waypoints (float3 positions).
        /// </summary>
        public NativeList<float3> path;

        /// <summary>
        /// Whether a valid path was found.
        /// </summary>
        public NativeArray<bool> pathFound;

        /// <summary>
        /// Total path length.
        /// </summary>
        public NativeArray<float> pathLength;

        public void Execute()
        {
            pathFound[0] = false;
            pathLength[0] = 0f;

            var obstacleMap = new NativeObstacleMap(obstacleGrid, sizeX, sizeY, sizeZ);

            // Step 1: Find the subvolume containing the goal and get its data
            SubvolumeData goalData;
            if (!FindGoalSubvolume(out goalData))
            {
                return;
            }

            // Step 2: Trace predecessor chain from goal back to start
            var rawPath = new NativeList<float3>(Allocator.Temp);
            var visited = new NativeHashSet<int>(256, Allocator.Temp);

            float3 currentPred = goalData.PredecessorCenter;
            float3 goalCenter = (float3)goalPos + new float3(0.5f, 0.5f, 0.5f);

            rawPath.Add(goalCenter);

            int safety = 0;
            int maxSteps = costField.Count() + 10;

            while (safety < maxSteps)
            {
                safety++;

                // Check if we've reached the start
                float distToStart = math.distance(currentPred, (float3)startPos + new float3(0.5f, 0.5f, 0.5f));
                if (distToStart < 0.5f)
                {
                    break;
                }

                // Find the subvolume containing currentPred and get its predecessor
                int3 predGrid = new int3(
                    (int)math.floor(currentPred.x),
                    (int)math.floor(currentPred.y),
                    (int)math.floor(currentPred.z));

                bool found = false;
                using (var keys = costField.GetKeyArray(Allocator.Temp))
                using (var values = costField.GetValueArray(Allocator.Temp))
                {
                    for (int i = 0; i < keys.Length; i++)
                    {
                        var sv = DecodeMortonCode(keys[i]);
                        float3 center = sv.Center;

                        if (math.distance(center, currentPred) < sv.Size * 0.75f)
                        {
                            var data = values[i];
                            currentPred = data.PredecessorCenter;
                            rawPath.Add(currentPred);
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                    break;
            }

            // Add start position
            rawPath.Add((float3)startPos + new float3(0.5f, 0.5f, 0.5f));

            // Step 3: Reverse so path goes from start to goal
            var forwardPath = new NativeList<float3>(rawPath.Length, Allocator.Temp);
            for (int i = rawPath.Length - 1; i >= 0; i--)
            {
                forwardPath.Add(rawPath[i]);
            }
            rawPath.Dispose();

            // Step 4: Apply Theta*-style line-of-sight shortcutting (taut path smoothing)
            var smoothed = SmoothPath(forwardPath, obstacleMap);
            forwardPath.Dispose();

            // Step 5: Output final path
            for (int i = 0; i < smoothed.Length; i++)
            {
                path.Add(smoothed[i]);
            }

            if (smoothed.Length >= 2)
            {
                pathFound[0] = true;
                float totalLen = 0f;
                for (int i = 1; i < smoothed.Length; i++)
                {
                    totalLen += math.distance(smoothed[i - 1], smoothed[i]);
                }
                pathLength[0] = totalLen;
            }

            smoothed.Dispose();
            visited.Dispose();
        }

        /// <summary>
        /// Find the goal subvolume by searching the cost field for a subvolume
        /// containing the goal position.
        /// </summary>
        private bool FindGoalSubvolume(out SubvolumeData goalData)
        {
            goalData = default;

            using (var keys = costField.GetKeyArray(Allocator.Temp))
            using (var values = costField.GetValueArray(Allocator.Temp))
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    // Reconstruct the octree index from morton code
                    var sv = DecodeMortonCode(keys[i]);
                    if (sv.Contains(goalPos))
                    {
                        if (values[i].gCost < float.PositiveInfinity)
                        {
                            goalData = values[i];
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Decode a morton code back into an OctreeIndex.
        /// </summary>
        private OctreeIndex DecodeMortonCode(int mortonCode)
        {
            uint m = (uint)mortonCode;
            int height = (int)(m >> 24);
            m &= 0x00FFFFFF;

            uint compact(uint v)
            {
                v &= 0x09249249;
                v = (v ^ (v >> 2)) & 0x030C30C3;
                v = (v ^ (v >> 4)) & 0x0300F00F;
                v = (v ^ (v >> 8)) & 0x030000FF;
                v = (v ^ (v >> 16)) & 0x000003FF;
                return v;
            }

            uint x = compact(m);
            uint y = compact(m >> 1);
            uint z = compact(m >> 2);

            return new OctreeIndex((int)x, (int)y, (int)z, height);
        }

        /// <summary>
        /// Theta*-style path smoothing: iteratively remove waypoints that have
        /// line-of-sight to a further waypoint (shortcutting).
        ///
        /// This produces a taut any-angle path by removing unnecessary turns.
        /// Algorithm: for each waypoint, try to skip ahead to the furthest waypoint
        /// that has line-of-sight.
        /// </summary>
        private NativeList<float3> SmoothPath(NativeList<float3> inputPath, NativeObstacleMap obstacleMap)
        {
            if (inputPath.Length <= 2)
            {
                var result = new NativeList<float3>(inputPath.Length, Allocator.Temp);
                for (int i = 0; i < inputPath.Length; i++)
                    result.Add(inputPath[i]);
                return result;
            }

            var smoothed = new NativeList<float3>(Allocator.Temp);
            smoothed.Add(inputPath[0]);

            int current = 0;
            while (current < inputPath.Length - 1)
            {
                // Try to find the furthest visible waypoint from current
                int furthest = current + 1;

                for (int candidate = inputPath.Length - 1; candidate > current + 1; candidate--)
                {
                    if (HasLineOfSight(obstacleMap, inputPath[current], inputPath[candidate]))
                    {
                        furthest = candidate;
                        break;
                    }
                }

                smoothed.Add(inputPath[furthest]);
                current = furthest;
            }

            return smoothed;
        }

        /// <summary>
        /// 3D line-of-sight check between two continuous points.
        /// Uses Bresenham-style ray traversal through grid cells.
        /// </summary>
        private bool HasLineOfSight(NativeObstacleMap obstacleMap, float3 from, float3 to)
        {
            // Use a supercover line algorithm that visits all cells the line passes through
            int x0 = (int)math.floor(from.x);
            int y0 = (int)math.floor(from.y);
            int z0 = (int)math.floor(from.z);
            int x1 = (int)math.floor(to.x);
            int y1 = (int)math.floor(to.y);
            int z1 = (int)math.floor(to.z);

            int dx = math.abs(x1 - x0);
            int dy = math.abs(y1 - y0);
            int dz = math.abs(z1 - z0);

            int sx = x0 < x1 ? 1 : (x0 > x1 ? -1 : 0);
            int sy = y0 < y1 ? 1 : (y0 > y1 ? -1 : 0);
            int sz = z0 < z1 ? 1 : (z0 > z1 ? -1 : 0);

            // Supercover 3D: visit all cells touched by the line
            // Use the 3D digital differential analyzer approach
            float fx0 = from.x, fy0 = from.y, fz0 = from.z;
            float fx1 = to.x, fy1 = to.y, fz1 = to.z;

            // Simple approach: sample at small intervals along the line
            float dist = math.distance(from, to);
            int steps = (int)math.ceil(dist * 2f); // 2 samples per unit distance
            steps = math.max(steps, 1);

            int prevX = x0, prevY = y0, prevZ = z0;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float px = math.lerp(fx0, fx1, t);
                float py = math.lerp(fy0, fy1, t);
                float pz = math.lerp(fz0, fz1, t);

                int cx = (int)math.floor(px);
                int cy = (int)math.floor(py);
                int cz = (int)math.floor(pz);

                if (!obstacleMap.IsTraversable(cx, cy, cz))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// High-level API for extracting paths from a Wavestar cost field.
    /// </summary>
    public static class WavestarPathExtractor
    {
        /// <summary>
        /// Extract a smooth any-angle path from the cost field.
        /// </summary>
        public static NativeList<float3> Extract(
            NativeParallelHashMap<int, SubvolumeData> costField,
            int3 startPos, int3 goalPos,
            NativeArray<int> obstacleGrid,
            int sizeX, int sizeY, int sizeZ,
            out bool found, out float length)
        {
            var path = new NativeList<float3>(Allocator.Persistent);
            var foundArr = new NativeArray<bool>(1, Allocator.TempJob);
            var lengthArr = new NativeArray<float>(1, Allocator.TempJob);

            var job = new WavestarPathExtractorJob
            {
                costField = costField,
                startPos = startPos,
                goalPos = goalPos,
                obstacleGrid = obstacleGrid,
                sizeX = sizeX,
                sizeY = sizeY,
                sizeZ = sizeZ,
                path = path,
                pathFound = foundArr,
                pathLength = lengthArr,
            };

            var handle = job.Schedule();
            handle.Complete();

            found = foundArr[0];
            length = lengthArr[0];

            foundArr.Dispose();
            lengthArr.Dispose();

            return path;
        }
    }
}
