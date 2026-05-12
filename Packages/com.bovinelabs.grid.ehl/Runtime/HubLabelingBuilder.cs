using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.EHL
{
    /// <summary>
    /// Phase 2 of EHL*: Compute hub labels on the visibility graph using the greedy
    /// hub cover algorithm.
    ///
    /// For each convex vertex v, compute H(v) = set of (hub, distance, viaVertex) pairs
    /// such that for any pair of vertices (v1, v2), H(v1) ∩ H(v2) contains at least one
    /// hub on their shortest path.
    ///
    /// Algorithm:
    /// 1. Run Dijkstra from every vertex to get all-pairs shortest distances and successors.
    /// 2. Use greedy cover: iteratively pick the vertex that covers the most uncovered
    ///    shortest-path pairs, add it as a hub to the label sets of all vertices it covers.
    /// 3. Store labels sorted by hub ID for efficient intersection queries.
    /// </summary>
    [BurstCompile]
    public struct HubLabelingBuilderJob : IJob
    {
        public NativeArray<ConvexVertex> ConvexVertices;
        public NativeArray<int> AdjOffsets;
        public NativeArray<int> AdjCounts;
        public NativeArray<AdjEdge> AdjEdges;

        /// <summary>Number of convex vertices.</summary>
        public int VertexCount;

        /// <summary>Output: hub labels for each vertex (flat array).</summary>
        public NativeList<VisibilityLabel> HubLabelsOut;

        /// <summary>Output: offset/count into HubLabelsOut for each vertex.</summary>
        public NativeList<int> HubOffsetsOut;
        public NativeList<int> HubCountsOut;

        /// <summary>Output: successor map entries (key=vertexId*VertexCount+hubId, value=next vertex toward hub).</summary>
        public NativeList<long> SuccKeysOut;
        public NativeList<int> SuccValuesOut;

        // Working arrays for all-pairs shortest paths
        // dist[vertexCount * vertexCount]: dist[i * VertexCount + j] = shortest distance from i to j
        // succ[vertexCount * vertexCount]: succ[i * VertexCount + j] = first successor from i toward j

        public void Execute()
        {
            int n = VertexCount;
            if (n == 0) return;

            // All-pairs shortest paths via Dijkstra from each vertex
            var dist = new NativeArray<float>(n * n, Allocator.Temp);
            var succ = new NativeArray<int>(n * n, Allocator.Temp);

            // Initialize distances
            for (int i = 0; i < n * n; i++)
            {
                dist[i] = float.MaxValue;
                succ[i] = -1;
            }
            for (int i = 0; i < n; i++)
            {
                dist[i * n + i] = 0f;
                succ[i * n + i] = i;
            }

            // Run Dijkstra from each vertex
            for (int src = 0; src < n; src++)
            {
                RunDijkstra(src, n, ref dist, ref succ);
            }

            // Greedy hub cover
            // covered[i * n + j] = true if the pair (i,j) is covered by some hub
            var covered = new NativeArray<bool>(n * n, Allocator.Temp);
            for (int i = 0; i < n * n; i++)
                covered[i] = false;

            // For each vertex, we need to cover all pairs (v, u) where there is a path.
            // A hub h covers pair (v, u) if h is on a shortest path from v to u,
            // i.e., dist[v][h] + dist[h][u] == dist[v][u] and both distances are finite.

            // hubLabels[v] = list of (hubId, dist[v][hub], succ[v][hub])
            var hubLabels = new NativeArray<NativeList<VisibilityLabel>>(n, Allocator.Temp);
            for (int i = 0; i < n; i++)
                hubLabels[i] = new NativeList<VisibilityLabel>(Allocator.Temp);

            // Count how many uncovered pairs each potential hub would cover
            // We iterate greedily: pick best hub, add to labels, mark covered, repeat.

            int totalPairs = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (dist[i * n + j] < float.MaxValue)
                        totalPairs++;
                }
            }

            int coveredCount = 0;
            int iterations = 0;

            while (coveredCount < totalPairs && iterations < n)
            {
                // Find vertex that covers the most uncovered pairs
                int bestHub = -1;
                int bestCoverage = 0;

                for (int h = 0; h < n; h++)
                {
                    int coverage = 0;
                    for (int i = 0; i < n; i++)
                    {
                        if (dist[i * n + h] >= float.MaxValue) continue;
                        for (int j = i + 1; j < n; j++)
                        {
                            if (covered[i * n + j]) continue;
                            if (dist[h * n + j] >= float.MaxValue) continue;

                            // Check if h is on shortest path from i to j
                            if (math.abs(dist[i * n + h] + dist[h * n + j] - dist[i * n + j]) < 1e-4f)
                            {
                                coverage++;
                            }
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

                // Add bestHub to the label of every vertex it can reach
                for (int v = 0; v < n; v++)
                {
                    if (dist[v * n + bestHub] < float.MaxValue)
                    {
                        // Check if this hub is already in v's label set
                        bool alreadyPresent = false;
                        for (int k = 0; k < hubLabels[v].Length; k++)
                        {
                            if (hubLabels[v][k].HubVertexId == bestHub)
                            {
                                alreadyPresent = true;
                                break;
                            }
                        }

                        if (!alreadyPresent)
                        {
                            hubLabels[v].Add(new VisibilityLabel(
                                bestHub,
                                dist[v * n + bestHub],
                                succ[v * n + bestHub]
                            ));

                            // Record successor
                            SuccKeysOut.Add((long)v * n + bestHub);
                            SuccValuesOut.Add(succ[v * n + bestHub]);
                        }
                    }
                }

                // Mark covered pairs
                for (int i = 0; i < n; i++)
                {
                    if (dist[i * n + bestHub] >= float.MaxValue) continue;
                    for (int j = i + 1; j < n; j++)
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

            // Sort labels by hub ID for each vertex, flatten into output
            int offset = 0;
            for (int v = 0; v < n; v++)
            {
                hubLabels[v].Sort();
                HubOffsetsOut.Add(offset);
                HubCountsOut.Add(hubLabels[v].Length);
                offset += hubLabels[v].Length;

                for (int k = 0; k < hubLabels[v].Length; k++)
                {
                    HubLabelsOut.Add(hubLabels[v][k]);
                }

                hubLabels[v].Dispose();
            }

            hubLabels.Dispose();
            covered.Dispose();
            dist.Dispose();
            succ.Dispose();
        }

        /// <summary>
        /// Run Dijkstra from source vertex, filling dist and succ arrays.
        /// </summary>
        private void RunDijkstra(int src, int n, ref NativeArray<float> dist, ref NativeArray<int> succ)
        {
            var visited = new NativeArray<bool>(n, Allocator.Temp);
            var queue = new NativeList<int>(Allocator.Temp);

            // Simple O(n^2) Dijkstra (sufficient for small-medium graphs)
            visited[src] = false;
            dist[src * n + src] = 0f;
            succ[src * n + src] = src;

            for (int iter = 0; iter < n; iter++)
            {
                // Find unvisited vertex with minimum distance
                float minDist = float.MaxValue;
                int u = -1;
                for (int i = 0; i < n; i++)
                {
                    if (!visited[i] && dist[src * n + i] < minDist)
                    {
                        minDist = dist[src * n + i];
                        u = i;
                    }
                }

                if (u < 0) break;
                visited[u] = true;

                // Relax neighbors
                int adjStart = AdjOffsets[u];
                int adjCount = AdjCounts[u];
                for (int e = 0; e < adjCount; e++)
                {
                    var edge = AdjEdges[adjStart + e];
                    int v = edge.TargetVertexId;
                    float newDist = dist[src * n + u] + edge.Distance;

                    if (newDist < dist[src * n + v])
                    {
                        dist[src * n + v] = newDist;
                        // Update successor: if src == u, successor is v (direct neighbor)
                        // Otherwise, same successor as toward u
                        if (src == u)
                        {
                            succ[src * n + v] = v;
                        }
                        else
                        {
                            succ[src * n + v] = succ[src * n + u];
                        }
                    }
                }
            }

            visited.Dispose();
            queue.Dispose();
        }
    }

    /// <summary>
    /// High-level wrapper for hub label computation.
    /// </summary>
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
                SuccValuesOut = succValues,
            };

            return job.Schedule(dependency);
        }
    }
}
