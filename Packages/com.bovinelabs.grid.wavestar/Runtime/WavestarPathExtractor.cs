using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wavestar
{
    [BurstCompile]
    public struct WavestarPathExtractorJob : IJob
    {
        public NativeParallelHashMap<int, SubvolumeData> costField;
        public int3 startPos;
        public int3 goalPos;
        public NativeArray<int> obstacleGrid;
        public int sizeX;
        public int sizeY;
        public int sizeZ;
        public NativeList<float3> path;
        public NativeArray<bool> pathFound;
        public NativeArray<float> pathLength;

        public unsafe void Execute()
        {
            pathFound[0] = false;
            pathLength[0] = 0f;

            var obstacleMap = new NativeObstacleMap(obstacleGrid, sizeX, sizeY, sizeZ);

            SubvolumeData goalData;
            if (!FindGoalSubvolume(out goalData)) return;

            var rawPath = new NativeList<float3>(Allocator.Temp);
            var visited = new NativeHashSet<int>(256, Allocator.Temp);

            var currentPred = goalData.PredecessorCenter;
            var goalCenter = goalPos + new float3(0.5f, 0.5f, 0.5f);

            rawPath.Add(goalCenter);

            var safety = 0;
            var maxSteps = costField.Count() + 10;

            while (safety < maxSteps)
            {
                safety++;

                var distToStart = math.distance(currentPred, startPos + new float3(0.5f, 0.5f, 0.5f));
                if (distToStart < 0.5f) break;

                var predGrid = new int3(
                    (int)math.floor(currentPred.x),
                    (int)math.floor(currentPred.y),
                    (int)math.floor(currentPred.z));

                var found = false;
                using (var keys = costField.GetKeyArray(Allocator.Temp))
                using (var values = costField.GetValueArray(Allocator.Temp))
                {
                    for (var i = 0; i < keys.Length; i++)
                    {
                        var sv = DecodeMortonCode(keys[i]);
                        var center = sv.Center;

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

            rawPath.Add(startPos + new float3(0.5f, 0.5f, 0.5f));

            var forwardPath = new NativeList<float3>(rawPath.Length, Allocator.Temp);
            for (var i = rawPath.Length - 1; i >= 0; i--) forwardPath.Add(rawPath[i]);
            rawPath.Dispose();

            var smoothed = SmoothPath(forwardPath, obstacleMap);
            forwardPath.Dispose();

            for (var i = 0; i < smoothed.Length; i++) path.Add(smoothed[i]);

            if (smoothed.Length >= 2)
            {
                pathFound[0] = true;
                var totalLen = 0f;
                for (var i = 1; i < smoothed.Length; i++) totalLen += math.distance(smoothed[i - 1], smoothed[i]);
                pathLength[0] = totalLen;
            }

            smoothed.Dispose();
            visited.Dispose();
        }

        private bool FindGoalSubvolume(out SubvolumeData goalData)
        {
            goalData = default;

            using (var keys = costField.GetKeyArray(Allocator.Temp))
            using (var values = costField.GetValueArray(Allocator.Temp))
            {
                for (var i = 0; i < keys.Length; i++)
                {
                    var sv = DecodeMortonCode(keys[i]);
                    if (sv.Contains(goalPos))
                        if (values[i].gCost < float.PositiveInfinity)
                        {
                            goalData = values[i];
                            return true;
                        }
                }
            }

            return false;
        }

        private OctreeIndex DecodeMortonCode(int mortonCode)
        {
            var m = (uint)mortonCode;
            var height = (int)(m >> 24);
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

            var x = compact(m);
            var y = compact(m >> 1);
            var z = compact(m >> 2);

            return new OctreeIndex((int)x, (int)y, (int)z, height);
        }

        private NativeList<float3> SmoothPath(NativeList<float3> inputPath, NativeObstacleMap obstacleMap)
        {
            if (inputPath.Length <= 2)
            {
                var result = new NativeList<float3>(inputPath.Length, Allocator.Temp);
                for (var i = 0; i < inputPath.Length; i++)
                    result.Add(inputPath[i]);
                return result;
            }

            var smoothed = new NativeList<float3>(Allocator.Temp);
            smoothed.Add(inputPath[0]);

            var current = 0;
            while (current < inputPath.Length - 1)
            {
                var furthest = current + 1;

                for (var candidate = inputPath.Length - 1; candidate > current + 1; candidate--)
                    if (HasLineOfSight(obstacleMap, inputPath[current], inputPath[candidate]))
                    {
                        furthest = candidate;
                        break;
                    }

                smoothed.Add(inputPath[furthest]);
                current = furthest;
            }

            return smoothed;
        }

        private bool HasLineOfSight(NativeObstacleMap obstacleMap, float3 from, float3 to)
        {
            var x0 = (int)math.floor(from.x);
            var y0 = (int)math.floor(from.y);
            var z0 = (int)math.floor(from.z);
            var x1 = (int)math.floor(to.x);
            var y1 = (int)math.floor(to.y);
            var z1 = (int)math.floor(to.z);

            var dx = math.abs(x1 - x0);
            var dy = math.abs(y1 - y0);
            var dz = math.abs(z1 - z0);

            var sx = x0 < x1 ? 1 : x0 > x1 ? -1 : 0;
            var sy = y0 < y1 ? 1 : y0 > y1 ? -1 : 0;
            var sz = z0 < z1 ? 1 : z0 > z1 ? -1 : 0;

            float fx0 = from.x, fy0 = from.y, fz0 = from.z;
            float fx1 = to.x, fy1 = to.y, fz1 = to.z;

            var dist = math.distance(from, to);
            var steps = (int)math.ceil(dist * 2f);
            steps = math.max(steps, 1);

            for (var i = 0; i <= steps; i++)
            {
                var t = (float)i / steps;
                var px = math.lerp(fx0, fx1, t);
                var py = math.lerp(fy0, fy1, t);
                var pz = math.lerp(fz0, fz1, t);

                var cx = (int)math.floor(px);
                var cy = (int)math.floor(py);
                var cz = (int)math.floor(pz);

                if (!obstacleMap.IsTraversable(cx, cy, cz))
                    return false;
            }

            return true;
        }
    }

    public static class WavestarPathExtractor
    {
        public static bool TryExtract(
            NativeParallelHashMap<int, SubvolumeData> costField,
            int3 startPos, int3 goalPos,
            NativeArray<int> obstacleGrid,
            int sizeX, int sizeY, int sizeZ,
            ref NativeList<float3> path,
            out float length)
        {
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
                pathLength = lengthArr
            };

            var handle = job.Schedule();
            handle.Complete();

            var found = foundArr[0];
            length = lengthArr[0];

            foundArr.Dispose();
            lengthArr.Dispose();

            return found;
        }
    }
}