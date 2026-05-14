using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.EHL
{
    [BurstCompile]
    public struct VisibilityGraphBuilderJob : IJob
    {
        public NativeArray<int> PolyOffsets;
        public NativeArray<int> PolyCounts;
        public NativeArray<float2> PolygonVertices;
        public NativeArray<ObstacleEdge> ObstacleEdges;
        public NativeList<ConvexVertex> ConvexVertices;
        public NativeList<AdjEdge> AdjEdgesOut;
        public NativeList<int> AdjOffsetsOut;
        public NativeList<int> AdjCountsOut;

        public unsafe void Execute()
        {
            var totalPolygons = PolyOffsets.Length;
            var convexVerts = new NativeList<ConvexVertex>(Allocator.Temp);
            var vertexPolyId = new NativeList<int>(Allocator.Temp);

            for (var p = 0; p < totalPolygons; p++)
            {
                var offset = PolyOffsets[p];
                var count = PolyCounts[p];


                var signedArea = 0f;
                for (var i = 0; i < count; i++)
                {
                    var a = PolygonVertices[offset + i];
                    var b = PolygonVertices[offset + (i + 1) % count];
                    signedArea += a.x * b.y - b.x * a.y;
                }

                var isCCW = signedArea > 0f;
                var isCW = !isCCW;

                for (var i = 0; i < count; i++)
                {
                    var prev = PolygonVertices[offset + (i - 1 + count) % count];
                    var curr = PolygonVertices[offset + i];
                    var next = PolygonVertices[offset + (i + 1) % count];

                    if (IsConvexVertex(prev, curr, next, isCW))
                    {
                        convexVerts.Add(new ConvexVertex(curr, convexVerts.Length));
                        vertexPolyId.Add(p);
                    }
                }
            }

            var n = convexVerts.Length;


            var adjLists = (UnsafeList<AdjEdge>**)UnsafeUtility.Malloc(n * sizeof(System.IntPtr), UnsafeUtility.AlignOf<System.IntPtr>(), Allocator.Temp);
            for (var i = 0; i < n; i++)
            {
                var list = (UnsafeList<AdjEdge>*)UnsafeUtility.Malloc(sizeof(UnsafeList<AdjEdge>), UnsafeUtility.AlignOf<UnsafeList<AdjEdge>>(), Allocator.Temp);
                *list = new UnsafeList<AdjEdge>(16, Allocator.Temp);
                adjLists[i] = list;
            }

            for (var i = 0; i < n; i++)
            for (var j = i + 1; j < n; j++)
            {
                if (vertexPolyId[i] == vertexPolyId[j])
                    continue;

                var a = convexVerts[i].Position;
                var b = convexVerts[j].Position;

                if (AreCoVisible(a, b, i, j, convexVerts, ObstacleEdges))
                {
                    var dist = math.distance(a, b);
                    adjLists[i]->Add(new AdjEdge(j, dist));
                    adjLists[j]->Add(new AdjEdge(i, dist));
                }
            }


            var edgeOffset = 0;
            for (var i = 0; i < n; i++)
            {
                var list = adjLists[i];


                list->Sort();

                AdjOffsetsOut.Add(edgeOffset);
                AdjCountsOut.Add(list->Length);
                edgeOffset += list->Length;

                for (var e = 0; e < list->Length; e++) AdjEdgesOut.Add(list->Ptr[e]);

                ConvexVertices.Add(convexVerts[i]);
                list->Dispose();
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Free(list, Unity.Collections.Allocator.Temp);
            }

            Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Free(adjLists, Unity.Collections.Allocator.Temp);
            convexVerts.Dispose();
            vertexPolyId.Dispose();
        }
        
        private bool IsConvexVertex(float2 prev, float2 curr, float2 next, bool isCW)
        {
            var edge1 = curr - prev;
            var edge2 = next - curr;
            var cross = edge1.x * edge2.y - edge1.y * edge2.x;


            const float eps = 1e-6f;
            return isCW ? cross < -eps : cross > eps;
        }

        private bool AreCoVisible(
            float2 a,
            float2 b,
            int idxA,
            int idxB,
            NativeList<ConvexVertex> convexVerts,
            NativeArray<ObstacleEdge> edges)
        {
            var ab = b - a;
            var lenSq = math.lengthsq(ab);
            if (lenSq < 1e-10f)
                return false;

            for (var e = 0; e < edges.Length; e++)
            {
                var c = edges[e].A;
                var d = edges[e].B;


                if (PointsEqual(a, c) || PointsEqual(a, d) || PointsEqual(b, c) || PointsEqual(b, d))
                    continue;

                if (SegmentsIntersect(a, b, c, d))
                    return false;
            }

            return true;
        }


        private bool SegmentsIntersect(float2 p1, float2 p2, float2 p3, float2 p4)
        {
            var d1 = p2 - p1;
            var d2 = p4 - p3;

            var cross = d1.x * d2.y - d1.y * d2.x;
            const float eps = 1e-10f;

            if (math.abs(cross) < eps) return false;

            var d3 = p3 - p1;
            var t = (d3.x * d2.y - d3.y * d2.x) / cross;
            var u = (d3.x * d1.y - d3.y * d1.x) / cross;

            const float margin = 1e-6f;
            return t > margin && t < 1.0f - margin && u > margin && u < 1.0f - margin;
        }

        private bool PointsEqual(float2 a, float2 b)
        {
            return math.abs(a.x - b.x) < 1e-5f && math.abs(a.y - b.y) < 1e-5f;
        }
    }


    public static class VisibilityGraphBuilder
    {
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
                AdjCountsOut = adjCounts
            };

            return job.Schedule(dependency);
        }
    }
}