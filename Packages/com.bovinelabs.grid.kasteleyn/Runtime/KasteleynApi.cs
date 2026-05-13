using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Kasteleyn
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KasteleynState
    {
        public Grid2D Grid;
        public NativeArray<byte> Region;
        public UnsafeList<int2> Edges;
        public UnsafeList<int2> EdgeCoords;
        public NativeArray<double> Matrix;
        public int VertexCount;
        public NativeArray<int> CellToVertex;
    }

    [BurstCompile]
    public unsafe static class KasteleynApi
    {
        public static KasteleynState Create(int width, int height, int maxEdges, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new KasteleynState
            {
                Grid = g,
                Region = new NativeArray<byte>(g.Length, a),
                Edges = new UnsafeList<int2>(maxEdges, a),
                EdgeCoords = new UnsafeList<int2>(maxEdges, a),
                Matrix = new NativeArray<double>(g.Length * g.Length, a),
                VertexCount = 0,
                CellToVertex = new NativeArray<int>(g.Length, a),
            };
        }

        [BurstCompile]
        public static void SetRegion(ref KasteleynState s, in NativeArray<byte> region)
        {
            UnsafeUtility.MemCpy(s.Region.GetUnsafePtr(), region.GetUnsafeReadOnlyPtr(), s.Grid.Length);
        }

        [BurstCompile]
        public static void BuildPlanarGraph(ref KasteleynState s)
        {
            s.Edges.Clear();
            s.EdgeCoords.Clear();
            byte* region = (byte*)s.Region.GetUnsafePtr();
            int* c2v = (int*)s.CellToVertex.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;

            int count = 0;
            for (int i = 0; i < len; i++)
                c2v[i] = region[i] != 0 ? count++ : -1;
            s.VertexCount = count;

            for (int i = 0; i < len; i++)
            {
                if (c2v[i] < 0) continue;
                int x = i % w;
                int y = i / w;
                // Right
                if (x + 1 < w && c2v[i + 1] >= 0)
                {
                    s.Edges.Add(new int2(c2v[i], c2v[i + 1]));
                    s.EdgeCoords.Add(new int2(i, i + 1));
                }
                // Up
                if (y + 1 < h && c2v[i + w] >= 0)
                {
                    s.Edges.Add(new int2(c2v[i], c2v[i + w]));
                    s.EdgeCoords.Add(new int2(i, i + w));
                }
            }
        }

        [BurstCompile]
        public static void OrientKasteleyn(ref KasteleynState s)
        {
            double* mat = (double*)s.Matrix.GetUnsafePtr();
            int n = s.VertexCount;
            int len = n * n;
            for (int i = 0; i < len; i++) mat[i] = 0.0;

            int2* edges = (int2*)s.Edges.Ptr;
            int2* edgeCoords = (int2*)s.EdgeCoords.Ptr;
            int w = s.Grid.Width;

            for (int i = 0; i < s.Edges.Length; i++)
            {
                int eA = edges[i].x;
                int eB = edges[i].y;
                int cellA = edgeCoords[i].x;
                int cellB = edgeCoords[i].y;
                int ax = cellA % w;
                int bx = cellB % w;

                int sign;
                if (ax == bx) sign = (ax % 2 == 0) ? 1 : -1;
                else sign = 1;

                mat[eA * n + eB] = sign;
                mat[eB * n + eA] = -sign;
            }
        }

        [BurstCompile]
        public static bool CountPerfectMatchings(ref KasteleynState s, out double count)
        {
            count = 0.0;
            if (s.VertexCount == 0) return false;
            if (s.VertexCount % 2 != 0) return true;

            int n = s.VertexCount;
            var mat = new NativeArray<double>(n * n, Allocator.Temp);
            UnsafeUtility.MemCpy(mat.GetUnsafePtr(), s.Matrix.GetUnsafeReadOnlyPtr(), n * n * 8);
            double* m = (double*)mat.GetUnsafePtr();

            double det = 1.0;
            for (int col = 0; col < n; col++)
            {
                int pivot = -1;
                for (int row = col; row < n; row++)
                {
                    if (math.abs(m[row * n + col]) > 1e-10) { pivot = row; break; }
                }
                if (pivot < 0) { det = 0; break; }

                if (pivot != col)
                {
                    det = -det;
                    for (int j = 0; j < n; j++)
                    {
                        double tmp = m[col * n + j];
                        m[col * n + j] = m[pivot * n + j];
                        m[pivot * n + j] = tmp;
                    }
                }

                det *= m[col * n + col];
                double invPivot = 1.0 / m[col * n + col];
                for (int row = col + 1; row < n; row++)
                {
                    double factor = m[row * n + col] * invPivot;
                    for (int j = col; j < n; j++)
                        m[row * n + j] -= factor * m[col * n + j];
                }
            }

            count = math.sqrt(math.abs(det));
            mat.Dispose();
            return true;
        }

        public static void Dispose(ref KasteleynState s)
        {
            if (s.Region.IsCreated) s.Region.Dispose();
            s.Edges.Dispose();
            s.EdgeCoords.Dispose();
            if (s.Matrix.IsCreated) s.Matrix.Dispose();
            if (s.CellToVertex.IsCreated) s.CellToVertex.Dispose();
        }
    }
}
