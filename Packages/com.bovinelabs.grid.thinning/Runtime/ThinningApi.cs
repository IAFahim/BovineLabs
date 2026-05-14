using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BovineLabs.Grid.Thinning
{
    public unsafe struct ThinningState
    {
        public Grid2D Grid;
        public byte* Mark;
        public UnsafeList<int> Frontier;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class ThinningApi
    {
        public static bool TryCreate(int width, int height, Allocator a, out ThinningState s)
        {
            s = default;
            if (!Grid2D.TryCreate(width, height, out var g)) return false;
            s = new ThinningState
            {
                Grid = g,
                Mark = (byte*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(byte), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<byte>(), g.Length),
                Frontier = new UnsafeList<int>(g.Length, a)
            };
            return true;
        }

        [BurstCompile]
        public static bool TryIterate(ref ThinningState s, ref NativeArray<byte> solid, out int changed)
        {
            changed = 0;
            if (!TrySubIterate(ref s, ref solid, 0, out var c1)) return false;
            if (!TrySubIterate(ref s, ref solid, 1, out var c2)) return false;
            changed = c1 + c2;
            return true;
        }

        private static bool TrySubIterate(ref ThinningState s, ref NativeArray<byte> solid, int step, out int changed)
        {
            changed = 0;
            var width = s.Grid.Width;
            var height = s.Grid.Height;
            var solidPtr = (byte*)solid.GetUnsafePtr();
            var markPtr = s.Mark;
            UnsafeUtility.MemSet(s.Mark, 0, s.Grid.Length * sizeof(byte));

            var toDelete = new UnsafeList<int>(s.Grid.Length, Allocator.Temp);

            for (var y = 1; y < height - 1; y++)
            for (var x = 1; x < width - 1; x++)
            {
                var i = y * width + x;
                if (solidPtr[i] == 0) continue;

                var p2 = solidPtr[(y - 1) * width + x];
                var p3 = solidPtr[(y - 1) * width + x + 1];
                var p4 = solidPtr[y * width + x + 1];
                var p5 = solidPtr[(y + 1) * width + x + 1];
                var p6 = solidPtr[(y + 1) * width + x];
                var p7 = solidPtr[(y + 1) * width + (x - 1)];
                var p8 = solidPtr[y * width + (x - 1)];
                var p9 = solidPtr[(y - 1) * width + (x - 1)];

                var b = p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9;
                if (b < 2 || b > 6) continue;

                var a = 0;
                if (p2 == 0 && p3 == 1) a++;
                if (p3 == 0 && p4 == 1) a++;
                if (p4 == 0 && p5 == 1) a++;
                if (p5 == 0 && p6 == 1) a++;
                if (p6 == 0 && p7 == 1) a++;
                if (p7 == 0 && p8 == 1) a++;
                if (p8 == 0 && p9 == 1) a++;
                if (p9 == 0 && p2 == 1) a++;

                if (a != 1) continue;

                if (step == 0)
                {
                    if (p2 * p4 * p6 != 0) continue;
                    if (p4 * p6 * p8 != 0) continue;
                }
                else
                {
                    if (p2 * p4 * p8 != 0) continue;
                    if (p2 * p6 * p8 != 0) continue;
                }

                markPtr[i] = 1;
                toDelete.Add(i);
            }

            for (var i = 0; i < toDelete.Length; i++) solidPtr[toDelete[i]] = 0;

            changed = toDelete.Length;
            toDelete.Dispose();
            return true;
        }

        public static void Dispose(ref ThinningState s)
        {
            if (s.Mark != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Mark); s.Mark = null; }
            s.Frontier.Dispose();
        }
    }
}