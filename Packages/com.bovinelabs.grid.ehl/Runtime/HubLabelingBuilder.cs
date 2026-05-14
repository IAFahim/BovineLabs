using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.EHL
{
    [BurstCompile]
    public struct HubLabelingBuilderJob : IJob
    {
        public NativeArray<ConvexVertex> ConvexVertices;
        public NativeArray<int> AdjOffsets;
        public NativeArray<int> AdjCounts;
        public NativeArray<AdjEdge> AdjEdges;


        public int VertexCount;


        public NativeList<VisibilityLabel> HubLabelsOut;


        public NativeList<int> HubOffsetsOut;
        public NativeList<int> HubCountsOut;


        public NativeList<long> SuccKeysOut;
        public NativeList<int> SuccValuesOut;


        public unsafe void Execute()
        {
            var n = VertexCount;
            if (n == 0) return;


            var dist = new NativeArray<float>(n * n, Allocator.Temp);
            var succ = new NativeArray<int>(n * n, Allocator.Temp);


            for (var i = 0; i < n * n; i++)
            {
                dist[i] = float.MaxValue;
                succ[i] = -1;
            }

            for (var i = 0; i < n; i++)
            {
                dist[i * n + i] = 0f;
                succ[i * n + i] = i;
            }


            for (var src = 0; src < n; src++) RunDijkstra(src, n, ref dist, ref succ);


            var covered = new NativeArray<bool>(n * n, Allocator.Temp);
            for (var i = 0; i < n * n; i++)
                covered[i] = false;


            var hubLabels = (UnsafeList<VisibilityLabel>**)UnsafeUtility.Malloc(n * sizeof(System.IntPtr), UnsafeUtility.AlignOf<System.IntPtr>(), Allocator.Temp);
            for (var i = 0; i < n; i++)
            {
                var list = (UnsafeList<VisibilityLabel>*)UnsafeUtility.Malloc(sizeof(UnsafeList<VisibilityLabel>), UnsafeUtility.AlignOf<UnsafeList<VisibilityLabel>>(), Allocator.Temp);
                *list = new UnsafeList<VisibilityLabel>(16, Allocator.Temp);
                hubLabels[i] = list;
            }


            var totalPairs = 0;
            for (var i = 0; i < n; i++)
            for (var j = i + 1; j < n; j++)
                if (dist[i * n + j] < float.MaxValue)
                    totalPairs++;

            var coveredCount = 0;
            var iterations = 0;

            while (coveredCount < totalPairs && iterations < n)
            {
                var bestHub = -1;
                var bestCoverage = 0;

                for (var h = 0; h < n; h++)
                {
                    var coverage = 0;
                    for (var i = 0; i < n; i++)
                    {
                        if (dist[i * n + h] >= float.MaxValue) continue;
                        for (var j = i + 1; j < n; j++)
                        {
                            if (covered[i * n + j]) continue;
                            if (dist[h * n + j] >= float.MaxValue) continue;


                            if (math.abs(dist[i * n + h] + dist[h * n + j] - dist[i * n + j]) < 1e-4f) coverage++;
                        }
                    }

                    if (coverage > bestCoverage)
                    {
                        bestCoverage = coverage;
                        bestHub = h;
                    }
                }

                if (bestHub < 0 || bestCoverage == 0)
                    break;


                for (var v = 0; v < n; v++)
                    if (dist[v * n + bestHub] < float.MaxValue)
                    {
                        var alreadyPresent = false;
                        for (var k = 0; k < hubLabels[v]->Length; k++)
                            if (hubLabels[v]->Ptr[k].HubVertexId == bestHub)
                            {
                                alreadyPresent = true;
                                break;
                            }

                        if (!alreadyPresent)
                        {
                            hubLabels[v]->Add(new VisibilityLabel(
                                bestHub,
                                dist[v * n + bestHub],
                                succ[v * n + bestHub]
                            ));


                            SuccKeysOut.Add((long)v * n + bestHub);
                            SuccValuesOut.Add(succ[v * n + bestHub]);
                        }
                    }


                for (var i = 0; i < n; i++)
                {
                    if (dist[i * n + bestHub] >= float.MaxValue) continue;
                    for (var j = i + 1; j < n; j++)
                    {
                        if (covered[i * n + j]) continue;
                        if (dist[bestHub * n + j] >= float.MaxValue) continue;

                        if (math.abs(dist[i * n + bestHub] + dist[bestHub * n + j] - dist[i * n + j]) < 1e-4f)
                        {
                            covered[i * n + j] = true;
                            covered[j * n + i] = true;
                            coveredCount++;
                        }
                    }
                }

                iterations++;
            }


            var offset = 0;
            for (var v = 0; v < n; v++)
            {
                hubLabels[v]->Sort();
                HubOffsetsOut.Add(offset);
                HubCountsOut.Add(hubLabels[v]->Length);
                offset += hubLabels[v]->Length;

                for (var k = 0; k < hubLabels[v]->Length; k++) HubLabelsOut.Add(hubLabels[v]->Ptr[k]);

                hubLabels[v]->Dispose();
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Free(hubLabels[v], Unity.Collections.Allocator.Temp);
            }

            Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Free(hubLabels, Unity.Collections.Allocator.Temp);
            covered.Dispose();
            dist.Dispose();
            succ.Dispose();
        }


        private void RunDijkstra(int src, int n, ref NativeArray<float> dist, ref NativeArray<int> succ)
        {
            var visited = new NativeArray<bool>(n, Allocator.Temp);
            var queue = new NativeList<int>(Allocator.Temp);


            visited[src] = false;
            dist[src * n + src] = 0f;
            succ[src * n + src] = src;

            for (var iter = 0; iter < n; iter++)
            {
                var minDist = float.MaxValue;
                var u = -1;
                for (var i = 0; i < n; i++)
                    if (!visited[i] && dist[src * n + i] < minDist)
                    {
                        minDist = dist[src * n + i];
                        u = i;
                    }

                if (u < 0) break;
                visited[u] = true;


                var adjStart = AdjOffsets[u];
                var adjCount = AdjCounts[u];
                for (var e = 0; e < adjCount; e++)
                {
                    var edge = AdjEdges[adjStart + e];
                    var v = edge.TargetVertexId;
                    var newDist = dist[src * n + u] + edge.Distance;

                    if (newDist < dist[src * n + v])
                    {
                        dist[src * n + v] = newDist;


                        if (src == u)
                            succ[src * n + v] = v;
                        else
                            succ[src * n + v] = succ[src * n + u];
                    }
                }
            }

            visited.Dispose();
            queue.Dispose();
        }
    }


    public static class HubLabelingBuilder
    {
        public static JobHandle Build(
            NativeArray<ConvexVertex> convexVertices,
            NativeArray<int> adjOffsets,
            NativeArray<int> adjCounts,
            NativeArray<AdjEdge> adjEdges,
            out NativeList<VisibilityLabel> hubLabels,
            out NativeList<int> hubOffsets,
            out NativeList<int> hubCounts,
            out NativeList<long> succKeys,
            out NativeList<int> succValues,
            JobHandle dependency = default)
        {
            hubLabels = new NativeList<VisibilityLabel>(Allocator.Persistent);
            hubOffsets = new NativeList<int>(Allocator.Persistent);
            hubCounts = new NativeList<int>(Allocator.Persistent);
            succKeys = new NativeList<long>(Allocator.Persistent);
            succValues = new NativeList<int>(Allocator.Persistent);

            var job = new HubLabelingBuilderJob
            {
                ConvexVertices = convexVertices,
                AdjOffsets = adjOffsets,
                AdjCounts = adjCounts,
                AdjEdges = adjEdges,
                VertexCount = convexVertices.Length,
                HubLabelsOut = hubLabels,
                HubOffsetsOut = hubOffsets,
                HubCountsOut = hubCounts,
                SuccKeysOut = succKeys,
                SuccValuesOut = succValues
            };

            return job.Schedule(dependency);
        }
    }
}