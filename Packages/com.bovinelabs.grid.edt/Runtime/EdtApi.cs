using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace BovineLabs.Grid.Edt
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EdtState
    {
        public Grid2D Grid;
        public float* Temp;
        public int* V;
        public float* Z;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class EdtApi
    {
        public static bool TryCreate(int width, int height, Allocator allocator, out EdtState result)
        {
            if (!Grid2D.TryCreate(width, height, out var grid))
            {
                result = default;
                return false;
            }

            result = new EdtState
            {
                Allocator = allocator,
                Grid = grid,
                Temp = (float*)Unity.Collections.AllocatorManager.Allocate(allocator, sizeof(float), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<float>(), width * height),
                V = (int*)Unity.Collections.AllocatorManager.Allocate(allocator, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), math.max(width, height)),
                Z = (float*)Unity.Collections.AllocatorManager.Allocate(allocator, sizeof(float), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<float>(), math.max(width, height) + 1)
            };
            return true;
        }

        [BurstCompile]
        public static void InitFromBlocked(in NativeArray<byte> blocked, ref NativeArray<float> dist2)
        {
            var b = (byte*)blocked.GetUnsafeReadOnlyPtr();
            var d = (float*)dist2.GetUnsafePtr();
            var len = blocked.Length;
            for (var i = 0; i < len; i++)
                d[i] = b[i] != 0 ? 0f : float.PositiveInfinity;
        }

        public static void Transform1D(
            in NativeArray<float> f,
            ref NativeArray<float> output,
            ref NativeArray<int> v,
            ref NativeArray<float> z,
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

            var k = 0;
            v[0] = 0;
            z[0] = float.NegativeInfinity;
            z[1] = float.PositiveInfinity;

            for (var q = 1; q < length; q++)
            {
                var s = Intersect(v[k], q, f[v[k]], f[q]);
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
            for (var q = 0; q < length; q++)
            {
                while (z[k + 1] < q)
                    k++;
                var dq = q - v[k];
                output[q] = f[v[k]] + dq * dq;
            }
        }

        [BurstCompile]
        public static bool TryBuild(ref EdtState s, in NativeArray<byte> blocked, ref NativeArray<float> dist2)
        {
            InitFromBlocked(in blocked, ref dist2);

            var dist2Ptr = (float*)dist2.GetUnsafePtr();
            var temp = s.Temp;
            var v = s.V;
            var z = s.Z;
            var w = s.Grid.Width;
            var h = s.Grid.Height;

            for (var y = 0; y < h; y++)
            {
                var rowStart = y * w;

                for (var x = 0; x < w; x++)
                    temp[x] = dist2Ptr[rowStart + x];

                Transform1D(temp, temp, v, z, w);

                var dst = dist2Ptr + rowStart;
                for (var x = 0; x < w; x++)
                    dst[x] = temp[x];
            }

            for (var x = 0; x < w; x++)
            {
                var src = dist2Ptr + x;
                for (var y = 0; y < h; y++)
                    temp[y] = src[y * w];

                Transform1D(temp, temp, v, z, h);

                for (var y = 0; y < h; y++)
                    src[y * w] = temp[y];
            }

            return true;
        }

        [BurstCompile]
        public static JobHandle TryBuildScheduled(
            ref EdtState s,
            in NativeArray<byte> blocked,
            ref NativeArray<float> dist2,
            int maxDim,
            JobHandle dependency = default)
        {
            InitFromBlocked(in blocked, ref dist2);

            var w = s.Grid.Width;
            var h = s.Grid.Height;

            var rowJob = new EdtRowJob
            {
                Dist2 = (float*)dist2.GetUnsafePtr(),
                Width = w,
                MaxDim = maxDim
            };

            var colJob = new EdtColJob
            {
                Dist2 = (float*)dist2.GetUnsafePtr(),
                Width = w,
                Height = h,
                MaxDim = maxDim
            };

            var rowHandle = rowJob.ScheduleParallel(h, 1, dependency);

            var colHandle = colJob.ScheduleParallel(w, 1, rowHandle);

            return colHandle;
        }

        [BurstCompile]
        public static bool TryBuildParallel(
            ref EdtState s,
            in NativeArray<byte> blocked,
            ref NativeArray<float> dist2,
            int maxDim)
        {
            var handle = TryBuildScheduled(ref s, in blocked, ref dist2, maxDim);
            handle.Complete();
            return true;
        }

        [BurstCompile]
        public static void ToDistance(in NativeArray<float> dist2, ref NativeArray<float> dist)
        {
            var d2 = (float*)dist2.GetUnsafeReadOnlyPtr();
            var d = (float*)dist.GetUnsafePtr();
            var len = dist2.Length;
            for (var i = 0; i < len; i++)
                d[i] = math.sqrt(d2[i]);
        }

        public static void Dispose(ref EdtState s)
        {
            if (s.Temp != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Temp); s.Temp = null; }
            if (s.V != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.V); s.V = null; }
            if (s.Z != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Z); s.Z = null; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Intersect(int q1, int q2, float f1, float f2)
        {
            return (f2 + q2 * q2 - (f1 + q1 * q1)) / (2f * (q2 - q1));
        }
    }

    [BurstCompile]
    public unsafe struct EdtRowJob : IJobFor
    {
        [NativeDisableUnsafePtrRestriction] public float* Dist2;
        public int Width;
        public int MaxDim;

        public unsafe void Execute(int rowIndex)
        {
            var v = (int*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<int>() * MaxDim, UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            var z = (float*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<float>() * (MaxDim + 1), UnsafeUtility.AlignOf<float>(), Allocator.Temp);

            var row = Dist2 + rowIndex * Width;
            EdtApi.Transform1D(row, row, v, z, Width);

            UnsafeUtility.Free(v, Allocator.Temp);
            UnsafeUtility.Free(z, Allocator.Temp);
        }
    }

    [BurstCompile]
    public unsafe struct EdtColJob : IJobFor
    {
        [NativeDisableUnsafePtrRestriction] public float* Dist2;
        public int Width;
        public int Height;
        public int MaxDim;

        public unsafe void Execute(int colIndex)
        {
            var v = (int*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<int>() * MaxDim, UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            var z = (float*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<float>() * (MaxDim + 1), UnsafeUtility.AlignOf<float>(), Allocator.Temp);
            var col = (float*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<float>() * Height, UnsafeUtility.AlignOf<float>(), Allocator.Temp);

            var src = Dist2 + colIndex;
            for (var y = 0; y < Height; y++)
                col[y] = src[y * Width];

            EdtApi.Transform1D(col, col, v, z, Height);

            for (var y = 0; y < Height; y++)
                src[y * Width] = col[y];

            UnsafeUtility.Free(col, Allocator.Temp);
            UnsafeUtility.Free(v, Allocator.Temp);
            UnsafeUtility.Free(z, Allocator.Temp);
        }
    }
}