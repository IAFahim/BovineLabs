using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Edt
{
    /// <summary>
    /// Exact Euclidean Distance Transform (squared distance).
    /// O(n) per dimension via Felzenszwalb-Huttenlocher parabola envelope.
    /// </summary>
    public struct EdtState
    {
        public Grid2D Grid;
        public NativeArray<float> Temp;
        public NativeArray<int> V;
        public NativeArray<float> Z;
    }

    public static class EdtApi
    {
        public static EdtState Create(int width, int height, Allocator allocator)
        {
            var s = new EdtState
            {
                Grid = Grid2D.Create(width, height),
                Temp = new NativeArray<float>(width * height, allocator),
                V = new NativeArray<int>(math.max(width, height), allocator),
                Z = new NativeArray<float>(math.max(width, height) + 1, allocator),
            };
            return s;
        }

        /// <summary>Initialize: obstacle cells get 0, free cells get +inf.</summary>
        public static void InitFromBlocked(NativeArray<byte> blocked, NativeArray<float> dist2)
        {
            for (int i = 0; i < blocked.Length; i++)
                dist2[i] = blocked[i] != 0 ? 0f : float.PositiveInfinity;
        }

        /// <summary>1D distance transform on a single row/column segment.</summary>
        public static void Transform1D(
            NativeArray<float> f,
            NativeArray<float> output,
            NativeArray<int> v,
            NativeArray<float> z,
            int length)
        {
            if (length <= 0) return;

            int k = 0;
            v[0] = 0;
            z[0] = float.NegativeInfinity;
            z[1] = float.PositiveInfinity;

            for (int q = 1; q < length; q++)
            {
                float s = Intersect(v[k], q, f[v[k]], f[q]);
                while (s <= z[k] && k > 0)
                {
                    k--;
                    s = Intersect(v[k], q, f[v[k]], f[q]);
                }
                k++;
                v[k] = q;
                z[k] = s;
                z[k + 1] = float.PositiveInfinity;
            }

            k = 0;
            for (int q = 0; q < length; q++)
            {
                while (z[k + 1] < q)
                    k++;
                int dq = q - v[k];
                output[q] = f[v[k]] + dq * dq;
            }
        }

        /// <summary>Full 2D EDT: row pass then column pass.</summary>
        public static void Build(ref EdtState s, NativeArray<byte> blocked, NativeArray<float> dist2)
        {
            InitFromBlocked(blocked, dist2);

            // Row pass
            for (int y = 0; y < s.Grid.Height; y++)
            {
                int rowStart = y * s.Grid.Width;
                // Copy row into Temp[0..width-1]
                for (int x = 0; x < s.Grid.Width; x++)
                    s.Temp[x] = dist2[rowStart + x];

                Transform1D(
                    s.Temp,
                    s.Temp,
                    s.V, s.Z,
                    s.Grid.Width);

                // Copy back
                for (int x = 0; x < s.Grid.Width; x++)
                    dist2[rowStart + x] = s.Temp[x];
            }

            // Column pass
            for (int x = 0; x < s.Grid.Width; x++)
            {
                for (int y = 0; y < s.Grid.Height; y++)
                    s.Temp[y] = dist2[y * s.Grid.Width + x];

                Transform1D(
                    s.Temp,
                    s.Temp,
                    s.V, s.Z,
                    s.Grid.Height);

                for (int y = 0; y < s.Grid.Height; y++)
                    dist2[y * s.Grid.Width + x] = s.Temp[y];
            }
        }

        /// <summary>Extract actual Euclidean distance (sqrt of squared).</summary>
        public static void ToDistance(NativeArray<float> dist2, NativeArray<float> dist)
        {
            for (int i = 0; i < dist2.Length; i++)
                dist[i] = math.sqrt(dist2[i]);
        }

        public static void Dispose(ref EdtState s)
        {
            if (s.Temp.IsCreated) s.Temp.Dispose();
            if (s.V.IsCreated) s.V.Dispose();
            if (s.Z.IsCreated) s.Z.Dispose();
        }

        private static float Intersect(int q1, int q2, float f1, float f2)
        {
            return ((f2 + q2 * q2) - (f1 + q1 * q1)) / (2f * (q2 - q1));
        }
    }
}
