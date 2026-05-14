using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Cftp
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CftpUpdate
    {
        public int Cell;
        public uint RandomBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CftpState
    {
        public Grid2D Grid;
        public byte* Low;
        public byte* High;
        public UnsafeList<CftpUpdate> Updates;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class CftpApi
    {
        public static bool TryCreate(int width, int height, int maxUpdates, Allocator a, out CftpState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new CftpState
            {
                Allocator = a,
                Grid = g,
                Low = (byte*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), g.Length),
                High = (byte*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), g.Length),
                Updates = new UnsafeList<CftpUpdate>(maxUpdates, a)
            };
            return true;
        }

        [BurstCompile]
        public static void InitializeExtremes(ref CftpState s)
        {
            var low = s.Low;
            var high = s.High;
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++)
            {
                low[i] = 0;
                high[i] = 1;
            }
        }

        [BurstCompile]
        public static bool TryGeneratePastUpdates(ref CftpState s, ref Random rng, int count)
        {
            s.Updates.Clear();
            var len = s.Grid.Length;
            var total = count * len;
            if (s.Updates.Capacity < total)
                s.Updates.SetCapacity(total);

            for (var t = 0; t < count; t++)
            for (var i = 0; i < len; i++)
                s.Updates.Add(new CftpUpdate
                {
                    Cell = i,
                    RandomBits = rng.NextUInt()
                });

            return true;
        }

        [BurstCompile]
        public static void Replay(ref CftpState s)
        {
            InitializeExtremes(ref s);

            var low = s.Low;
            var high = s.High;
            var w = s.Grid.Width;
            var h = s.Grid.Height;
            var len = s.Grid.Length;

            for (var i = 0; i < s.Updates.Length; i++)
            {
                var u = s.Updates[i];
                var bit = (byte)(u.RandomBits & 1);
                var cell = u.Cell;
                var cx = cell % w;
                var cy = cell / w;

                int lowN = 0, highN = 0;
                if (cx + 1 < w)
                {
                    lowN += low[cell + 1];
                    highN += high[cell + 1];
                }

                if (cy + 1 < h)
                {
                    lowN += low[cell + w];
                    highN += high[cell + w];
                }

                if (cx > 0)
                {
                    lowN += low[cell - 1];
                    highN += high[cell - 1];
                }

                if (cy > 0)
                {
                    lowN += low[cell - w];
                    highN += high[cell - w];
                }

                low[cell] = (byte)(bit & math.select(0, 1, lowN >= 2) & 1);
                high[cell] = (byte)(bit & math.select(0, 1, highN >= 2) & 1);
            }
        }

        [BurstCompile]
        public static bool Coalesced(ref CftpState s)
        {
            var low = s.Low;
            var high = s.High;
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++)
                if (low[i] != high[i])
                    return false;
            return true;
        }

        [BurstCompile]
        public static bool TrySampleExact(ref CftpState s, ref Random rng, ref NativeArray<byte> sample)
        {
            if (!sample.IsCreated || sample.Length < s.Grid.Length) return false;

            for (var attempt = 0; attempt < 20; attempt++)
            {
                TryGeneratePastUpdates(ref s, ref rng, 1 << attempt);
                Replay(ref s);
                if (Coalesced(ref s))
                {
                    UnsafeUtility.MemCpy(sample.GetUnsafePtr(), s.Low, s.Grid.Length);
                    return true;
                }
            }

            return false;
        }

        public static void Dispose(ref CftpState s)
        {
            if (s.Low != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Low); s.Low = null; }
            if (s.High != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.High); s.High = null; }
            if (s.Updates.IsCreated) s.Updates.Dispose();
        }
    }
}