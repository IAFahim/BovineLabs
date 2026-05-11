using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Kasteleyn
{
    public struct KasteleynState
    {
        public Grid2D Grid;
        public NativeArray<byte> Region;
        public NativeList<int2> Edges;       // (vertexA, vertexB) in compact IDs
        public NativeList<int2> EdgeCoords;   // original grid coords of endpoints for orientation
        public NativeArray<double> Matrix;
        public int VertexCount;
        public NativeArray<int> CellToVertex;
    }

    public static class KasteleynApi
    {
        public static KasteleynState Create(int width, int height, int maxEdges, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new KasteleynState
            {
                Grid = g,
                Region = new NativeArray<byte>(g.Length, a),
                Edges = new NativeList<int2>(maxEdges, a),
                EdgeCoords = new NativeList<int2>(maxEdges, a),
                Matrix = new NativeArray<double>(g.Length * g.Length, a),
                VertexCount = 0,
                CellToVertex = new NativeArray<int>(g.Length, a),
            };
        }

        public static void SetRegion(ref KasteleynState s, NativeArray<byte> region)
        {
            NativeArray<byte>.Copy(region, s.Region);
        }

        public static void BuildPlanarGraph(ref KasteleynState s)
        {
            s.Edges.Clear();
            s.EdgeCoords.Clear();
            s.CellToVertex.Fill(-1);

            int count = 0;
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.Region[i] != 0)
                    s.CellToVertex[i] = count++;
            }
            s.VertexCount = count;

            // Add edges (right=0 and down=1 only to avoid duplicates)
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.CellToVertex[i] < 0) continue;
                int2 p = s.Grid.ToCoord(i);
                for (int d = 0; d < 2; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (s.CellToVertex[ni] < 0) continue;
                    s.Edges.Add(new int2(s.CellToVertex[i], s.CellToVertex[ni]));
                    s.EdgeCoords.Add(new int2(i, ni)); // store original cell indices
                }
            }
        }

        public static void OrientKasteleyn(ref KasteleynState s)
        {
            s.Matrix.Fill(0.0);

            for (int i = 0; i < s.Edges.Length; i++)
            {
                int2 e = s.Edges[i];
                int cellA = s.EdgeCoords[i].x;
                int cellB = s.EdgeCoords[i].y;
                int2 coordA = s.Grid.ToCoord(cellA);
                int2 coordB = s.Grid.ToCoord(cellB);

                int sign;
                if (coordA.x == coordB.x)
                {
                    // Vertical edge: alternate sign by column
                    sign = (coordA.x % 2 == 0) ? 1 : -1;
                }
                else
                {
                    // Horizontal edge: always +1
                    sign = 1;
                }

                s.Matrix[e.x * s.VertexCount + e.y] = sign;
                s.Matrix[e.y * s.VertexCount + e.x] = -sign;
            }
        }

        public static bool CountPerfectMatchings(ref KasteleynState s, out double count)
        {
            count = 0.0;
            if (s.VertexCount == 0) return false;
            if (s.VertexCount % 2 != 0) { count = 0; return true; } // odd vertices can't have perfect matching
            if (s.VertexCount > 20) return false;

            int n = s.VertexCount;
            var mat = new NativeArray<double>(n * n, Allocator.Temp);
            NativeArray<double>.Copy(s.Matrix, mat);

            double det = 1.0;
            for (int col = 0; col < n; col++)
            {
                int pivot = -1;
                for (int row = col; row < n; row++)
                {
                    if (math.abs(mat[row * n + col]) > 1e-10) { pivot = row; break; }
                }
                if (pivot < 0) { det = 0; break; }

                if (pivot != col)
                {
                    det = -det;
                    for (int j = 0; j < n; j++)
                    {
                        double tmp = mat[col * n + j];
                        mat[col * n + j] = mat[pivot * n + j];
                        mat[pivot * n + j] = tmp;
                    }
                }

                det *= mat[col * n + col];

                for (int row = col + 1; row < n; row++)
                {
                    double factor = mat[row * n + col] / mat[col * n + col];
                    for (int j = col; j < n; j++)
                        mat[row * n + j] -= factor * mat[col * n + j];
                }
            }

            count = math.sqrt(math.abs(det));
            mat.Dispose();
            return true;
        }

        public static void Dispose(ref KasteleynState s)
        {
            if (s.Region.IsCreated) s.Region.Dispose();
            if (s.Edges.IsCreated) s.Edges.Dispose();
            if (s.EdgeCoords.IsCreated) s.EdgeCoords.Dispose();
            if (s.Matrix.IsCreated) s.Matrix.Dispose();
            if (s.CellToVertex.IsCreated) s.CellToVertex.Dispose();
        }
    }
}
