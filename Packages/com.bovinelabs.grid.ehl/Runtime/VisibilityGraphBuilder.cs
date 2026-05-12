using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.EHL
{
    /// <summary>
    /// Builds the visibility graph from polygonal obstacles.
    /// Phase 1 of EHL*: identify convex vertices, compute visibility edges between
    /// co-visible convex vertices.
    /// </summary>
    [BurstCompile]
    public struct VisibilityGraphBuilderJob : IJob
    {
        /// <summary>Obstacle polygons as flat arrays: PolyOffsets[polyIdx] is the start index in PolygonVertices, PolyCounts[polyIdx] is the vertex count.</summary>
        public NativeArray<int> PolyOffsets;
        public NativeArray<int> PolyCounts;
        public NativeArray<float2> PolygonVertices;

        /// <summary>Obstacle edges (all polygon edges).</summary>
        public NativeArray<ObstacleEdge> ObstacleEdges;

        /// <summary>Output: convex vertices found.</summary>
        public NativeList<ConvexVertex> ConvexVertices;

        /// <summary>Output: adjacency edges (visibility edges).</summary>
        public NativeList<AdjEdge> AdjEdgesOut;

        /// <summary>Output: for each convex vertex, the offset and count into AdjEdgesOut.</summary>
        public NativeList<int> AdjOffsetsOut;
        public NativeList<int> AdjCountsOut;

        public void Execute()
        {
            // Step 1: Identify convex vertices and remember which polygon they belong to
            int totalPolygons = PolyOffsets.Length;
            var convexVerts = new NativeList<ConvexVertex>(Allocator.Temp);
            var vertexPolyId = new NativeList<int>(Allocator.Temp); // polygon index for each convex vertex

            for (int p = 0; p < totalPolygons; p++)
            {
                int offset = PolyOffsets[p];
                int count = PolyCounts[p];

                // Compute signed area using the standard shoelace formula to determine winding direction.
                // Standard: signedArea = sum_{i}( x_i * y_{i+1} - x_{i+1} * y_i ).
                // signedArea > 0 => CCW winding, signedArea < 0 => CW winding.
                float signedArea = 0f;
                for (int i = 0; i < count; i++)
                {
                    float2 a = PolygonVertices[offset + i];
                    float2 b = PolygonVertices[offset + (i + 1) % count];
                    signedArea += a.x * b.y - b.x * a.y;
                }
                bool isCCW = signedArea > 0f;
                bool isCW = !isCCW;

                for (int i = 0; i < count; i++)
                {
                    float2 prev = PolygonVertices[offset + (i - 1 + count) % count];
                    float2 curr = PolygonVertices[offset + i];
                    float2 next = PolygonVertices[offset + (i + 1) % count];

                    if (IsConvexVertex(prev, curr, next, isCW))
                    {
                        convexVerts.Add(new ConvexVertex(curr, convexVerts.Length));
                        vertexPolyId.Add(p);
                    }
                }
            }

            int n = convexVerts.Length;

            // Step 2: Compute visibility between convex vertices
            // Store adjacency temporarily in a list per vertex, then flatten
            var adjLists = new NativeArray<NativeList<AdjEdge>>(n, Allocator.Temp);
            for (int i = 0; i < n; i++)
                adjLists[i] = new NativeList<AdjEdge>(Allocator.Temp);

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    // Skip non-adjacent vertices from the same polygon:
                    // Their connecting line passes through the polygon interior.
                    if (vertexPolyId[i] == vertexPolyId[j])
                        continue;

                    float2 a = convexVerts[i].Position;
                    float2 b = convexVerts[j].Position;

                    if (AreCoVisible(a, b, i, j, convexVerts, ObstacleEdges))
                    {
                        float dist = math.distance(a, b);
                        adjLists[i].Add(new AdjEdge(j, dist));
                        adjLists[j].Add(new AdjEdge(i, dist));
                    }
                }
            }

            // Step 3: Flatten into output arrays
            int edgeOffset = 0;
            for (int i = 0; i < n; i++)
            {
                var list = adjLists[i];

                // Sort neighbors by ID for deterministic output
                list.Sort();

                AdjOffsetsOut.Add(edgeOffset);
                AdjCountsOut.Add(list.Length);
                edgeOffset += list.Length;

                for (int e = 0; e < list.Length; e++)
                {
                    AdjEdgesOut.Add(list[e]);
                }

                ConvexVertices.Add(convexVerts[i]);
                list.Dispose();
            }

            adjLists.Dispose();
            convexVerts.Dispose();
            vertexPolyId.Dispose();
        }

        /// <summary>
        /// Determine if vertex at `curr` is convex given prev and next vertices.
        /// For a polygon wound clockwise, the cross product should be negative at convex corners.
        /// For a polygon wound counterclockwise, the cross product should be positive at convex corners.
        /// </summary>
        private bool IsConvexVertex(float2 prev, float2 curr, float2 next, bool isCW)
        {
            float2 edge1 = curr - prev;
            float2 edge2 = next - curr;
            float cross = edge1.x * edge2.y - edge1.y * edge2.x;

            // For CW polygons, convex vertices have cross < 0 (outward-pointing turn).
            // For CCW polygons, convex vertices have cross > 0.
            // We use |cross| > epsilon to avoid degenerate collinear vertices.
            const float eps = 1e-6f;
            return isCW ? (cross < -eps) : (cross > eps);
        }

        /// <summary>
        /// Check if two convex vertices are co-visible: the line segment between them
        /// does not intersect any obstacle edge (except at the shared endpoints).
        /// </summary>
        private bool AreCoVisible(
            float2 a,
            float2 b,
            int idxA,
            int idxB,
            NativeList<ConvexVertex> convexVerts,
            NativeArray<ObstacleEdge> edges)
        {
            float2 ab = b - a;
            float lenSq = math.lengthsq(ab);
            if (lenSq < 1e-10f)
                return false;

            for (int e = 0; e < edges.Length; e++)
            {
                float2 c = edges[e].A;
                float2 d = edges[e].B;

                // Skip edges that have a or b as an endpoint (they share a vertex)
                if (PointsEqual(a, c) || PointsEqual(a, d) || PointsEqual(b, c) || PointsEqual(b, d))
                    continue;

                if (SegmentsIntersect(a, b, c, d))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Test if segments (p1,p2) and (p3,p4) intersect.
        /// </summary>
        private bool SegmentsIntersect(float2 p1, float2 p2, float2 p3, float2 p4)
        {
            float2 d1 = p2 - p1;
            float2 d2 = p4 - p3;

            float cross = d1.x * d2.y - d1.y * d2.x;
            const float eps = 1e-10f;

            if (math.abs(cross) < eps)
            {
                // Parallel - check for overlap (conservative: treat as non-intersecting)
                return false;
            }

            float2 d3 = p3 - p1;
            float t = (d3.x * d2.y - d3.y * d2.x) / cross;
            float u = (d3.x * d1.y - d3.y * d1.x) / cross;

            const float margin = 1e-6f;
            return t > margin && t < 1.0f - margin && u > margin && u < 1.0f - margin;
        }

        private bool PointsEqual(float2 a, float2 b)
        {
            return math.abs(a.x - b.x) < 1e-5f && math.abs(a.y - b.y) < 1e-5f;
        }
    }

    /// <summary>
    /// High-level builder that wraps the Burst job for visibility graph construction.
    /// </summary>
    public static class VisibilityGraphBuilder
    {
        /// <summary>
        /// Build visibility graph from obstacle polygons.
        /// </summary>
        /// <param name="polyOffsets">Start index in polygonVertices for each polygon.</param>
        /// <param name="polyCounts">Vertex count for each polygon.</param>
        /// <param name="polygonVertices">Flat array of all polygon vertex positions.</param>
        /// <param name="obstacleEdges">All edges of the obstacle polygons.</param>
        /// <param name="convexVertices">Output: convex vertices.</param>
        /// <param name="adjOffsets">Output: adjacency offset per vertex.</param>
        /// <param name="adjCounts">Output: adjacency count per vertex.</param>
        /// <param name="adjEdges">Output: adjacency edges (target + distance).</param>
        public static JobHandle Build(
            NativeArray<int> polyOffsets,
            NativeArray<int> polyCounts,
            NativeArray<float2> polygonVertices,
            NativeArray<ObstacleEdge> obstacleEdges,
            out NativeList<ConvexVertex> convexVertices,
            out NativeList<int> adjOffsets,
            out NativeList<int> adjCounts,
            out NativeList<AdjEdge> adjEdges,
            JobHandle dependency = default)
        {
            convexVertices = new NativeList<ConvexVertex>(Allocator.Persistent);
            adjOffsets = new NativeList<int>(Allocator.Persistent);
            adjCounts = new NativeList<int>(Allocator.Persistent);
            adjEdges = new NativeList<AdjEdge>(Allocator.Persistent);

            var job = new VisibilityGraphBuilderJob
            {
                PolyOffsets = polyOffsets,
                PolyCounts = polyCounts,
                PolygonVertices = polygonVertices,
                ObstacleEdges = obstacleEdges,
                ConvexVertices = convexVertices,
                AdjEdgesOut = adjEdges,
                AdjOffsetsOut = adjOffsets,
                AdjCountsOut = adjCounts,
            };

            return job.Schedule(dependency);
        }
    }
}
