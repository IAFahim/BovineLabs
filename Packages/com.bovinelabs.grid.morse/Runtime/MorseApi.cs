using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Morse
{
    public struct CriticalPoint
    {
        public int Cell;
        public byte Type; // 0=min, 1=saddle, 2=max
        public float Value;
        public int Pair;
        public float Persistence;
    }

    public struct MorseState
    {
        public Grid2D Grid;
        public NativeArray<int> Ascending;
        public NativeArray<int> Descending;
        public NativeList<CriticalPoint> Critical;
        public NativeArray<int> Component;
    }

    public static class MorseApi
    {
        public static MorseState Create(int width, int height, int maxCritical, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new MorseState
            {
                Grid = g,
                Ascending = new NativeArray<int>(g.Length, a),
                Descending = new NativeArray<int>(g.Length, a),
                Critical = new NativeList<CriticalPoint>(maxCritical, a),
                Component = new NativeArray<int>(g.Length, a),
            };
        }

        public static void BuildGradient(ref MorseState s, NativeArray<float> scalar)
        {
            s.Critical.Clear();

            for (int i = 0; i < s.Grid.Length; i++)
            {
                int2 p = s.Grid.ToCoord(i);
                float v = scalar[i];

                int lower = -1, upper = -1;
                float lowerVal = v, upperVal = v;
                bool hasLower = false, hasUpper = false;

                for (int d = 0; d < 8; d++)
                {
                    int2 np = p + Grid2D.Directions8[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    float nv = scalar[ni];

                    if (nv < v) { hasLower = true; if (nv < lowerVal || lower < 0) { lowerVal = nv; lower = ni; } }
                    if (nv > v) { hasUpper = true; if (nv > upperVal || upper < 0) { upperVal = nv; upper = ni; } }
                }

                s.Ascending[i] = hasUpper ? upper : -1;
                s.Descending[i] = hasLower ? lower : -1;

                if (!hasLower && !hasUpper)
                {
                    // Flat plateau — could be saddle-like, skip for now
                }
                else if (!hasLower)
                {
                    s.Critical.Add(new CriticalPoint { Cell = i, Type = 0, Value = v, Pair = -1 });
                }
                else if (!hasUpper)
                {
                    s.Critical.Add(new CriticalPoint { Cell = i, Type = 2, Value = v, Pair = -1 });
                }
            }
        }

        public static void TraceManifolds(ref MorseState s)
        {
            s.Component.Fill(-1);

            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.Component[i] >= 0) continue;

                int cur = i;
                int steps = 0;
                int maxSteps = s.Grid.Length;

                // Follow descending gradient to a minimum
                while (s.Descending[cur] >= 0 && steps < maxSteps)
                {
                    int next = s.Descending[cur];
                    if (s.Component[next] >= 0)
                    {
                        // Already traced — inherit component
                        cur = s.Component[next];
                        break;
                    }
                    cur = next;
                    steps++;
                }

                // cur is now a minimum (or traced cell) — assign component
                int component = cur;

                // Re-walk to set component for all visited cells
                cur = i;
                steps = 0;
                var visited = new NativeList<int>(Allocator.Temp);
                while (s.Component[cur] < 0 && steps < maxSteps)
                {
                    visited.Add(cur);
                    if (s.Descending[cur] < 0) break;
                    cur = s.Descending[cur];
                    steps++;
                }
                if (s.Component[cur] >= 0) component = s.Component[cur];
                for (int v = 0; v < visited.Length; v++)
                    s.Component[visited[v]] = component;
                if (s.Component[cur] < 0) s.Component[cur] = component;

                visited.Dispose();
            }
        }

        public static void PairByPersistence(ref MorseState s, NativeArray<float> scalar)
        {
            // Sort critical points by persistence potential: pair maxima with their descending minimum
            for (int i = 0; i < s.Critical.Length; i++)
            {
                if (s.Critical[i].Pair >= 0) continue;
                if (s.Critical[i].Type != 2) continue; // only pair maxima

                int cur = s.Critical[i].Cell;
                int steps = 0;
                while (s.Descending[cur] >= 0 && steps < s.Grid.Length)
                {
                    cur = s.Descending[cur];
                    steps++;
                }

                float persistence = math.abs(scalar[s.Critical[i].Cell] - scalar[cur]);
                s.Critical[i] = new CriticalPoint
                {
                    Cell = s.Critical[i].Cell,
                    Type = s.Critical[i].Type,
                    Value = s.Critical[i].Value,
                    Pair = cur,
                    Persistence = persistence,
                };
            }
        }

        public static void Simplify(ref MorseState s, NativeArray<float> scalar, float threshold)
        {
            // Remove critical points with persistence below threshold by redirecting gradient
            for (int i = 0; i < s.Critical.Length; i++)
            {
                if (s.Critical[i].Persistence < threshold && s.Critical[i].Persistence > 0)
                {
                    // Cancel this pair: redirect descending from the critical point to its pair's descending
                    int cell = s.Critical[i].Cell;
                    int pair = s.Critical[i].Pair;
                    if (pair >= 0 && pair < s.Grid.Length)
                    {
                        // Point the critical cell to its pair's descent
                        s.Descending[cell] = s.Descending[pair];
                    }
                }
            }
        }

        public static void Dispose(ref MorseState s)
        {
            if (s.Ascending.IsCreated) s.Ascending.Dispose();
            if (s.Descending.IsCreated) s.Descending.Dispose();
            if (s.Critical.IsCreated) s.Critical.Dispose();
            if (s.Component.IsCreated) s.Component.Dispose();
        }
    }
}
