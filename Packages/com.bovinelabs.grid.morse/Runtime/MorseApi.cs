using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

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
    public struct MorseState
    {
        public Grid2D Grid;
        public NativeArray<int> Ascending;
        public NativeArray<int> Descending;
        public UnsafeList<CriticalPoint> Critical;
        public NativeArray<int> Component;
    }

    [BurstCompile]
    public unsafe static class MorseApi
    {
        public static MorseState Create(int width, int height, int maxCritical, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new MorseState
            {
                Grid = g,
                Ascending = new NativeArray<int>(g.Length, a),
                Descending = new NativeArray<int>(g.Length, a),
                Critical = new UnsafeList<CriticalPoint>(maxCritical, a),
                Component = new NativeArray<int>(g.Length, a),
            };
        }

        [BurstCompile]
        public static void BuildGradient(ref MorseState s, in NativeArray<float> scalar)
        {
            s.Critical.Clear();
            float* sc = (float*)scalar.GetUnsafeReadOnlyPtr();
            int* asc = (int*)s.Ascending.GetUnsafePtr();
            int* desc = (int*)s.Descending.GetUnsafePtr();
            int w = s.Grid.Width;
            int h = s.Grid.Height;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    float v = sc[i];

                    int lower = -1, upper = -1;
                    float lowerVal = v, upperVal = v;
                    bool hasLower = false, hasUpper = false;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx, ny = y + dy;
                            if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) continue;
                            int ni = ny * w + nx;
                            float nv = sc[ni];
                            if (nv < v) { hasLower = true; if (nv < lowerVal || lower < 0) { lowerVal = nv; lower = ni; } }
                            if (nv > v) { hasUpper = true; if (nv > upperVal || upper < 0) { upperVal = nv; upper = ni; } }
                        }
                    }

                    asc[i] = hasUpper ? upper : -1;
                    desc[i] = hasLower ? lower : -1;

                    if (!hasLower)
                        s.Critical.Add(new CriticalPoint { Cell = i, Type = 0, Value = v, Pair = -1 });
                    else if (!hasUpper)
                        s.Critical.Add(new CriticalPoint { Cell = i, Type = 2, Value = v, Pair = -1 });
                }
            }
        }

        [BurstCompile]
        public static void TraceManifolds(ref MorseState s)
        {
            int* comp = (int*)s.Component.GetUnsafePtr();
            int* desc = (int*)s.Descending.GetUnsafePtr();
            int len = s.Grid.Length;
            for (int i = 0; i < len; i++) comp[i] = -1;

            var visited = new UnsafeList<int>(64, Allocator.Temp);

            for (int i = 0; i < len; i++)
            {
                if (comp[i] >= 0) continue;

                // Follow descending to min
                int cur = i;
                while (desc[cur] >= 0 && comp[cur] < 0)
                    cur = desc[cur];

                int component = comp[cur] >= 0 ? comp[cur] : cur;

                // Re-walk assigning component
                cur = i;
                visited.Clear();
                while (comp[cur] < 0)
                {
                    visited.Add(cur);
                    if (desc[cur] < 0) break;
                    cur = desc[cur];
                }
                if (comp[cur] >= 0) component = comp[cur];
                for (int v = 0; v < visited.Length; v++)
                    comp[visited[v]] = component;
                if (comp[cur] < 0) comp[cur] = component;
            }

            visited.Dispose();
        }

        [BurstCompile]
        public static void PairByPersistence(ref MorseState s, in NativeArray<float> scalar)
        {
            float* sc = (float*)scalar.GetUnsafeReadOnlyPtr();
            int* desc = (int*)s.Descending.GetUnsafePtr();
            int len = s.Grid.Length;
            CriticalPoint* crit = (CriticalPoint*)s.Critical.Ptr;

            for (int i = 0; i < s.Critical.Length; i++)
            {
                if (crit[i].Pair >= 0 || crit[i].Type != 2) continue;

                int cur = crit[i].Cell;
                int steps = 0;
                while (desc[cur] >= 0 && steps < len) { cur = desc[cur]; steps++; }

                float persistence = math.abs(sc[crit[i].Cell] - sc[cur]);
                crit[i].Pair = cur;
                crit[i].Persistence = persistence;
            }
        }

        [BurstCompile]
        public static void Simplify(ref MorseState s, in NativeArray<float> scalar, float threshold)
        {
            int* desc = (int*)s.Descending.GetUnsafePtr();
            CriticalPoint* crit = (CriticalPoint*)s.Critical.Ptr;
            int len = s.Grid.Length;

            for (int i = 0; i < s.Critical.Length; i++)
            {
                if (crit[i].Persistence < threshold && crit[i].Persistence > 0)
                {
                    int cell = crit[i].Cell;
                    int pair = crit[i].Pair;
                    if (pair >= 0 && pair < len)
                        desc[cell] = desc[pair];
                }
            }
        }

        public static void Dispose(ref MorseState s)
        {
            if (s.Ascending.IsCreated) s.Ascending.Dispose();
            if (s.Descending.IsCreated) s.Descending.Dispose();
            s.Critical.Dispose();
            if (s.Component.IsCreated) s.Component.Dispose();
        }
    }
}
