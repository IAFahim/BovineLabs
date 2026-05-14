using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Kasteleyn
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KasteleynState
    {
        public Grid2D Grid;
        public byte* Region;
        public UnsafeList<int2> Edges;
        public UnsafeList<int2> EdgeCoords;
        public double* Matrix;
        public int VertexCount;
        public int* CellToVertex;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class KasteleynApi
    {
        public static bool TryCreate(int width, int height, int maxEdges, Allocator a, out KasteleynState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new KasteleynState
            {
                Allocator = a,
                Grid = g,
                Region = (byte*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), g.Length),
                Edges = new UnsafeList<int2>(maxEdges, a),
                EdgeCoords = new UnsafeList<int2>(maxEdges, a),
                Matrix = (double*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(double), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<double>(), g.Length * g.Length),
                VertexCount = 0,
                CellToVertex = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length)
            };
            return true;
        }

        [BurstCompile]
        public static void SetRegion(ref KasteleynState s, in NativeArray<byte> region)
        {
            UnsafeUtility.MemCpy(s.Region, region.GetUnsafeReadOnlyPtr(), s.Grid.Length);
        }

        [BurstCompile]
        public static bool TryBuildPlanarGraph(ref KasteleynState s)
        {
            s.Edges.Clear();
            s.EdgeCoords.Clear();
            var region = s.Region;
            var c2v = s.CellToVertex;
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var len = s.Grid.Length;

            var count = 0;
            for (var i = 0; i < len; i++)
                c2v[i] = region[i] != 0 ? count++ : -1;
            s.VertexCount = count;

            for (var i = 0; i < len; i++)
            {
                if (c2v[i] < 0) continue;
                var x = i % w;
                var y = i / w;
                if (x + 1 < w && c2v[i + 1] >= 0)
                {
                    s.Edges.Add(new int2(c2v[i], c2v[i + 1]));
                    s.EdgeCoords.Add(new int2(i, i + 1));
                }

                if (y + 1 < h && c2v[i + w] >= 0)
                {
                    s.Edges.Add(new int2(c2v[i], c2v[i + w]));
                    s.EdgeCoords.Add(new int2(i, i + w));
                }
            }

            return true;
        }

        [BurstCompile]
        public static bool TryOrientKasteleyn(ref KasteleynState s)
        {
            var mat = s.Matrix;
            var n = s.VertexCount;
            var len = n * n;
            for (var i = 0; i < len; i++) mat[i] = 0.0;

            var edges = s.Edges.Ptr;
            var edgeCoords = s.EdgeCoords.Ptr;
            var w = s.Grid.Width;

            for (var i = 0; i < s.Edges.Length; i++)
            {
                var eA = edges[i].x;
                var eB = edges[i].y;
                var cellA = edgeCoords[i].x;
                var cellB = edgeCoords[i].y;
                var ax = cellA % w;
                var bx = cellB % w;

                int sign;
                if (ax == bx) sign = ax % 2 == 0 ? 1 : -1;
                else sign = 1;

                mat[eA * n + eB] = sign;
                mat[eB * n + eA] = -sign;
            }

            return true;
        }

        [BurstCompile]
        public static bool TryCountPerfectMatchings(ref KasteleynState s, out double count)
        {
            count = 0.0;
            if (s.VertexCount == 0) return false;
            if (s.VertexCount % 2 != 0) return true;

            var n = s.VertexCount;
            var mat = new Unity.Collections.NativeArray<double>(n * n, Allocator.Temp);
            UnsafeUtility.MemCpy(mat.GetUnsafePtr(), s.Matrix, n * n * 8);
            var m = (double*)mat.GetUnsafePtr();

            var det = 1.0;
            for (var col = 0; col < n; col++)
            {
                var pivot = -1;
                for (var row = col; row < n; row++)
                    if (math.abs(m[row * n + col]) > 1e-10)
                    {
                        pivot = row;
                        break;
                    }

                if (pivot < 0)
                {
                    det = 0;
                    break;
                }

                if (pivot != col)
                {
                    det = -det;
                    for (var j = 0; j < n; j++)
                    {
                        var tmp = m[col * n + j];
                        m[col * n + j] = m[pivot * n + j];
                        m[pivot * n + j] = tmp;
                    }
                }

                det *= m[col * n + col];
                var invPivot = 1.0 / m[col * n + col];
                for (var row = col + 1; row < n; row++)
                {
                    var factor = m[row * n + col] * invPivot;
                    for (var j = col; j < n; j++)
                        m[row * n + j] -= factor * m[col * n + j];
                }
            }

            count = math.sqrt(math.abs(det));
            mat.Dispose();
            return true;
        }

        public static void Dispose(ref KasteleynState s)
        {
            if (s.Region != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Region); s.Region = null; }
            if (s.Edges.IsCreated) s.Edges.Dispose();
            if (s.EdgeCoords.IsCreated) s.EdgeCoords.Dispose();
            if (s.Matrix != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Matrix); s.Matrix = null; }
            if (s.CellToVertex != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.CellToVertex); s.CellToVertex = null; }
        }
    }
}