using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BovineLabs.Grid.Morse
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CriticalPoint
    {
        public int Cell;
        public byte Type;
        public float Value;
        public int Pair;
        public float Persistence;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MorseState
    {
        public Grid2D Grid;
        public int* Ascending;
        public int* Descending;
        public UnsafeList<CriticalPoint> Critical;
        public int* Component;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }

    [BurstCompile]
    public static unsafe class MorseApi
    {
        public static bool TryCreate(int width, int height, int maxCritical, Allocator a, out MorseState result)
        {
            if (!Grid2D.TryCreate(width, height, out var g))
            {
                result = default;
                return false;
            }

            result = new MorseState
            {
                Allocator = a,
                Grid = g,
                Ascending = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length),
                Descending = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length),
                Critical = new UnsafeList<CriticalPoint>(maxCritical, a),
                Component = (int*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(int), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<int>(), g.Length)
            };
            return true;
        }

        [BurstCompile]
        public static bool TryBuildGradient(ref MorseState s, in NativeArray<float> scalar)
        {
            s.Critical.Clear();
            var sc = (float*)scalar.GetUnsafeReadOnlyPtr();
            var asc = s.Ascending;
            var desc = s.Descending;
            var w = s.Grid.Width;
            var h = s.Grid.Height;

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                var v = sc[i];

                int lower = -1, upper = -1;
                float lowerVal = v, upperVal = v;
                bool hasLower = false, hasUpper = false;

                for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) continue;
                    var ni = ny * w + nx;
                    var nv = sc[ni];
                    if (nv < v)
                    {
                        hasLower = true;
                        if (nv < lowerVal || lower < 0)
                        {
                            lowerVal = nv;
                            lower = ni;
                        }
                    }

                    if (nv > v)
                    {
                        hasUpper = true;
                        if (nv > upperVal || upper < 0)
                        {
                            upperVal = nv;
                            upper = ni;
                        }
                    }
                }

                asc[i] = hasUpper ? upper : -1;
                desc[i] = hasLower ? lower : -1;

                if (!hasLower)
                    s.Critical.Add(new CriticalPoint { Cell = i, Type = 0, Value = v, Pair = -1 });
                else if (!hasUpper)
                    s.Critical.Add(new CriticalPoint { Cell = i, Type = 2, Value = v, Pair = -1 });
            }

            return true;
        }

        [BurstCompile]
        public static void TraceManifolds(ref MorseState s)
        {
            TryTraceManifolds(ref s);
        }

        [BurstCompile]
        public static bool TryTraceManifolds(ref MorseState s)
        {
            var comp = s.Component;
            var desc = s.Descending;
            var len = s.Grid.Length;
            for (var i = 0; i < len; i++) comp[i] = -1;

            var visited = new UnsafeList<int>(64, Allocator.Temp);

            for (var i = 0; i < len; i++)
            {
                if (comp[i] >= 0) continue;

                var cur = i;
                while (desc[cur] >= 0 && comp[cur] < 0)
                    cur = desc[cur];

                var component = comp[cur] >= 0 ? comp[cur] : cur;

                cur = i;
                visited.Clear();
                while (comp[cur] < 0)
                {
                    visited.Add(cur);
                    if (desc[cur] < 0) break;
                    cur = desc[cur];
                }

                if (comp[cur] >= 0) component = comp[cur];
                for (var v = 0; v < visited.Length; v++)
                    comp[visited[v]] = component;
                if (comp[cur] < 0) comp[cur] = component;
            }

            visited.Dispose();
            return true;
        }

        [BurstCompile]
        public static bool TryPairByPersistence(ref MorseState s, in NativeArray<float> scalar)
        {
            var sc = (float*)scalar.GetUnsafeReadOnlyPtr();
            var desc = s.Descending;
            var len = s.Grid.Length;
            var crit = s.Critical.Ptr;

            for (var i = 0; i < s.Critical.Length; i++)
            {
                if (crit[i].Pair >= 0 || crit[i].Type != 2) continue;

                var cur = crit[i].Cell;
                var steps = 0;
                while (desc[cur] >= 0 && steps < len)
                {
                    cur = desc[cur];
                    steps++;
                }

                var persistence = math.abs(sc[crit[i].Cell] - sc[cur]);
                crit[i].Pair = cur;
                crit[i].Persistence = persistence;
            }

            return true;
        }

        [BurstCompile]
        public static bool TrySimplify(ref MorseState s, in NativeArray<float> scalar, float threshold)
        {
            var desc = s.Descending;
            var crit = s.Critical.Ptr;
            var len = s.Grid.Length;

            for (var i = 0; i < s.Critical.Length; i++)
                if (crit[i].Persistence < threshold && crit[i].Persistence > 0)
                {
                    var cell = crit[i].Cell;
                    var pair = crit[i].Pair;
                    if (pair >= 0 && pair < len)
                        desc[cell] = desc[pair];
                }

            return true;
        }

        public static void Dispose(ref MorseState s)
        {
            if (s.Ascending != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Ascending); s.Ascending = null; }
            if (s.Descending != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Descending); s.Descending = null; }
            if (s.Critical.IsCreated) s.Critical.Dispose();
            if (s.Component != null) { Unity.Collections.AllocatorManager.Free(s.Allocator, s.Component); s.Component = null; }
        }
    }
}