using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.Wavestar
{
    [BurstCompile]
    public struct MultiResThetaStarJob : IJob
    {
        public int3 startPos;


        public int3 goalPos;


        public float epsilon;


        public int maxHeight;


        public int minHeight;


        public int sizeX;
        public int sizeY;
        public int sizeZ;


        public NativeArray<int> obstacleGrid;


        public NativeParallelHashMap<int, SubvolumeData> costField;


        public NativeArray<bool> pathFound;


        public NativeArray<float> goalGCost;


        private NativeObstacleMap obstacleMap;

        public unsafe void Execute()
        {
            obstacleMap = new NativeObstacleMap(obstacleGrid, sizeX, sizeY, sizeZ);
            pathFound[0] = false;
            goalGCost[0] = float.PositiveInfinity;


            var open = new NativeMinPQ(Allocator.Temp);

            var closed = new NativeHashSet<int>(sizeX * sizeY, Allocator.Temp);

            var fScores = new NativeHashMap<int, float>(sizeX * sizeY, Allocator.Temp);


            var startSV = FindFinestSubvolume(startPos, maxHeight);
            var goalSV = FindFinestSubvolume(goalPos, maxHeight);


            var startCenter = startSV.Center;
            var startG = 0f;
            var startH = math.distance(startCenter, goalPos);
            var startF = startG + startH;

            var startData = new SubvolumeData(startPos.x, startPos.y, startPos.z, startG);
            costField.TryAdd(startSV.MortonCode, startData);
            open.Push(new OpenSetElement(startSV, startF));
            fScores[startSV.MortonCode] = startF;

            var iterations = 0;
            var maxIterations = sizeX * sizeY * sizeZ * 4;

            while (open.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var current = open.Pop();
                var currentIdx = current.index;


                if (closed.Contains(currentIdx.MortonCode))
                    continue;


                closed.Add(currentIdx.MortonCode);


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


                if (!costField.TryGetValue(currentIdx.MortonCode, out var currentData))
                    continue;


                var neighbors = CollectNeighbors(currentIdx, closed);
                for (var i = 0; i < neighbors.Length; i++)
                {
                    var neighborIdx = neighbors[i];
                    UpdateSubvolume(ref open, ref fScores, ref closed, currentIdx, currentData, neighborIdx, goalSV);
                }

                neighbors.Dispose();
            }


            if (!pathFound[0] && costField.TryGetValue(goalSV.MortonCode, out var gData))
            {
                pathFound[0] = true;
                goalGCost[0] = gData.gCost;
            }

            open.Dispose();
            closed.Dispose();
            fScores.Dispose();
        }


        private OctreeIndex FindFinestSubvolume(int3 pos, int startHeight)
        {
            for (var h = startHeight; h >= 0; h--)
            {
                var s = 1 << h;
                var sx = pos.x >> h;
                var sy = pos.y >> h;
                var sz = pos.z >> h;
                var sv = new OctreeIndex(sx, sy, sz, h);
                if (obstacleMap.IsSubvolumeTraversable(sv))
                    return sv;
            }


            return new OctreeIndex(pos.x, pos.y, pos.z, 0);
        }


        private NativeList<OctreeIndex> CollectNeighbors(OctreeIndex idx, NativeHashSet<int> closed)
        {
            var neighbors = new NativeList<OctreeIndex>(Allocator.Temp);


            var s = idx.Size;


            for (var dz = -1; dz <= 1; dz++)
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0 && dz == 0)
                    continue;

                var nx = idx.x + dx;
                var ny = idx.y + dy;
                var nz = idx.z + dz;


                for (var nh = math.max(idx.height - 1, minHeight); nh <= math.min(idx.height + 1, maxHeight); nh++)
                {
                    var deltaH = nh - idx.height;
                    int cnx, cny, cnz;
                    if (deltaH >= 0)
                    {
                        cnx = nx >> deltaH;
                        cny = ny >> deltaH;
                        cnz = nz >> deltaH;
                    }
                    else
                    {
                        cnx = nx << -deltaH;
                        cny = ny << -deltaH;
                        cnz = nz << -deltaH;
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


                    var alreadyAdded = false;
                    for (var j = 0; j < neighbors.Length; j++)
                        if (neighbors[j].MortonCode == nIdx.MortonCode)
                        {
                            alreadyAdded = true;
                            break;
                        }

                    if (!alreadyAdded)
                        neighbors.Add(nIdx);
                }
            }

            return neighbors;
        }


        private void UpdateSubvolume(
            ref NativeMinPQ open,
            ref NativeHashMap<int, float> fScores,
            ref NativeHashSet<int> closed,
            OctreeIndex currentIdx,
            SubvolumeData currentData,
            OctreeIndex neighborIdx,
            OctreeIndex goalSV)
        {
            var currentCenter = currentIdx.Center;
            var neighborCenter = neighborIdx.Center;


            var existingG = float.PositiveInfinity;
            if (costField.TryGetValue(neighborIdx.MortonCode, out var existingData)) existingG = existingData.gCost;


            var directG = currentData.gCost + math.distance(currentCenter, neighborCenter);


            var losG = float.PositiveInfinity;
            var losPred = currentData.Predecessor;
            var predCenter = currentData.PredecessorCenter;

            if (HasLineOfSight(predCenter, neighborCenter))
            {
                losG = currentData.gCost - math.distance(currentCenter, predCenter)
                       + math.distance(predCenter, neighborCenter);


                losG = currentData.gCost
                       - math.distance(predCenter, currentCenter)
                       + math.distance(predCenter, neighborCenter);
            }


            var candidateG = math.min(directG, losG);
            int3 candidatePred;
            if (candidateG == losG && losG < directG)
                candidatePred = losPred;
            else
                candidatePred = new int3((int)currentCenter.x, (int)currentCenter.y, (int)currentCenter.z);


            var cmp = CompareCosts(existingG, candidateG);

            switch (cmp)
            {
                case ComparisonResult.StrictlyBetter:

                    var newData = new SubvolumeData(candidatePred.x, candidatePred.y, candidatePred.z, candidateG);
                    costField[neighborIdx.MortonCode] = newData;


                    var h = math.distance(neighborCenter, goalPos);
                    var f = candidateG + h;

                    fScores[neighborIdx.MortonCode] = f;
                    open.Push(new OpenSetElement(neighborIdx, f));
                    break;

                case ComparisonResult.Ambiguous:

                    if (neighborIdx.height > minHeight)
                        SubdivideAndRepropagate(
                            ref open, ref fScores, ref closed,
                            currentIdx, currentData, neighborIdx, goalSV);
                    else
                        goto case ComparisonResult.StrictlyBetter;
                    break;

                case ComparisonResult.NotBetter:
                default:

                    break;
            }
        }


        private ComparisonResult CompareCosts(float existing, float candidate)
        {
            if (float.IsInfinity(existing))
                return ComparisonResult.StrictlyBetter;

            var threshold = epsilon * math.max(math.abs(existing), math.abs(candidate));
            threshold = math.max(threshold, 1e-6f);

            var diff = existing - candidate;

            if (diff > threshold)
                return ComparisonResult.StrictlyBetter;
            if (diff > -threshold)
                return ComparisonResult.Ambiguous;
            return ComparisonResult.NotBetter;
        }


        private void SubdivideAndRepropagate(
            ref NativeMinPQ open,
            ref NativeHashMap<int, float> fScores,
            ref NativeHashSet<int> closed,
            OctreeIndex currentIdx,
            SubvolumeData currentData,
            OctreeIndex neighborIdx,
            OctreeIndex goalSV)
        {
            var childCount = sizeY > 1 ? 8 : 4;
            for (var c = 0; c < childCount; c++)
            {
                var child = neighborIdx.Child(c);


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


                UpdateSubvolume(ref open, ref fScores, ref closed, currentIdx, currentData, child, goalSV);
            }
        }


        private bool HasLineOfSight(float3 from, float3 to)
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

            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var sz = z0 < z1 ? 1 : -1;


            if (dx >= dy && dx >= dz)
            {
                var errY = 2 * dy - dx;
                var errZ = 2 * dz - dx;
                for (var i = 0; i <= dx; i++)
                {
                    if (!obstacleMap.IsTraversable(x0, y0, z0))
                        return false;
                    if (errY > 0)
                    {
                        y0 += sy;
                        errY -= 2 * dx;
                    }

                    if (errZ > 0)
                    {
                        z0 += sz;
                        errZ -= 2 * dx;
                    }

                    errY += 2 * dy;
                    errZ += 2 * dz;
                    x0 += sx;
                }
            }
            else if (dy >= dx && dy >= dz)
            {
                var errX = 2 * dx - dy;
                var errZ = 2 * dz - dy;
                for (var i = 0; i <= dy; i++)
                {
                    if (!obstacleMap.IsTraversable(x0, y0, z0))
                        return false;
                    if (errX > 0)
                    {
                        x0 += sx;
                        errX -= 2 * dy;
                    }

                    if (errZ > 0)
                    {
                        z0 += sz;
                        errZ -= 2 * dy;
                    }

                    errX += 2 * dx;
                    errZ += 2 * dz;
                    y0 += sy;
                }
            }
            else
            {
                var errX = 2 * dx - dz;
                var errY = 2 * dy - dz;
                for (var i = 0; i <= dz; i++)
                {
                    if (!obstacleMap.IsTraversable(x0, y0, z0))
                        return false;
                    if (errX > 0)
                    {
                        x0 += sx;
                        errX -= 2 * dz;
                    }

                    if (errY > 0)
                    {
                        y0 += sy;
                        errY -= 2 * dz;
                    }

                    errX += 2 * dx;
                    errY += 2 * dy;
                    z0 += sz;
                }
            }

            return true;
        }
    }
}