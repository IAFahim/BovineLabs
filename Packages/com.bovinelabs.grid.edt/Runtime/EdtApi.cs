using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Edt
{
    [StructLayout(LayoutKind.Sequential)]
    public struct EdtState
    {
        public Grid2D Grid;
        public NativeArray<float> Temp;
        public NativeArray<int> V;
        public NativeArray<float> Z;
    }

    [BurstCompile]
    public unsafe static class EdtApi
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

        [BurstCompile]
        public static void InitFromBlocked(NativeArray<byte> blocked, NativeArray<float> dist2)
        {
            byte* b = (byte*)blocked.GetUnsafeReadOnlyPtr();
            float* d = (float*)dist2.GetUnsafePtr();
            int len = blocked.Length;
            for (int i = 0; i < len; i++)
                d[i] = b[i] != 0 ? 0f : float.PositiveInfinity;
        }

        public static void Transform1D(
            NativeArray<float> f,
            NativeArray<float> output,
            NativeArray<int> v,
            NativeArray<float> z,
            int length)
        {
            Transform1D(
                (float*)f.GetUnsafeReadOnlyPtr(),
                (float*)output.GetUnsafePtr(),
                (int*)v.GetUnsafePtr(),
                (float*)z.GetUnsafePtr(),
                length);
        }

        [BurstCompile]
        public static void Transform1D(
            float* f,
            float* output,
            int* v,
            float* z,
            int length)
        {
            if (Hint.Unlikely(length <= 0)) return;

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

        [BurstCompile]
        public static void Build(ref EdtState s, NativeArray<byte> blocked, NativeArray<float> dist2)
        {
            InitFromBlocked(blocked, dist2);

            float* dist2Ptr = (float*)dist2.GetUnsafePtr();
            float* temp = (float*)s.Temp.GetUnsafePtr();
            int* v = (int*)s.V.GetUnsafePtr();
            float* z = (float*)s.Z.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;

            // Row pass
            for (int y = 0; y < h; y++)
            {
                int rowStart = y * w;
                for (int x = 0; x < w; x++)
                    temp[x] = dist2Ptr[rowStart + x];

                Transform1D(temp, temp, v, z, w);

                for (int x = 0; x < w; x++)
                    dist2Ptr[rowStart + x] = temp[x];
            }

            // Column pass
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                    temp[y] = dist2Ptr[y * w + x];

                Transform1D(temp, temp, v, z, h);

                for (int y = 0; y < h; y++)
                    dist2Ptr[y * w + x] = temp[y];
            }
        }

        [BurstCompile]
        public static void ToDistance(NativeArray<float> dist2, NativeArray<float> dist)
        {
            float* d2 = (float*)dist2.GetUnsafeReadOnlyPtr();
            float* d = (float*)dist.GetUnsafePtr();
            int len = dist2.Length;
            for (int i = 0; i < len; i++)
                d[i] = math.sqrt(d2[i]);
        }

        public static void Dispose(ref EdtState s)
        {
            if (s.Temp.IsCreated) s.Temp.Dispose();
            if (s.V.IsCreated) s.V.Dispose();
            if (s.Z.IsCreated) s.Z.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Intersect(int q1, int q2, float f1, float f2)
        {
            return ((f2 + q2 * q2) - (f1 + q1 * q1)) / (2f * (q2 - q1));
        }
    }
}
