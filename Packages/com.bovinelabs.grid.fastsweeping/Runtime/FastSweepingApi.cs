using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.FastSweeping
{
    public struct FastSweepingState
    {
        public Grid2D Grid;
        public NativeArray<float> T;
    }

    public static class FastSweepingApi
    {
        public static FastSweepingState Create(int width, int height, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new FastSweepingState
            {
                Grid = g,
                T = new NativeArray<float>(g.Length, a),
            };
        }

        public static void Initialize(ref FastSweepingState s, NativeArray<int> sources)
        {
            s.T.Fill(float.PositiveInfinity);
            for (int i = 0; i < sources.Length; i++)
                s.T[sources[i]] = 0f;
        }

        public static void SweepAllDirections(ref FastSweepingState s, NativeArray<float> speed, int rounds)
        {
            for (int r = 0; r < rounds; r++)
            {
                Sweep(s, speed, 1, 1);  // x+, y+
                Sweep(s, speed, -1, 1); // x-, y+
                Sweep(s, speed, 1, -1); // x+, y-
                Sweep(s, speed, -1, -1);// x-, y-
            }
        }

        private static void Sweep(FastSweepingState s, NativeArray<float> speed, int dx, int dy)
        {
            int xStart = dx > 0 ? 0 : s.Grid.Width - 1;
            int yStart = dy > 0 ? 0 : s.Grid.Height - 1;

            for (int yi = 0; yi < s.Grid.Height; yi++)
            {
                int y = yStart + yi * dy;
                for (int xi = 0; xi < s.Grid.Width; xi++)
                {
                    int x = xStart + xi * dx;
                    RelaxCell(s, speed, s.Grid.ToIndex(x, y));
                }
            }
        }

        public static void RelaxCell(FastSweepingState s, NativeArray<float> speed, int cell)
        {
            float sp = speed[cell];
            if (sp <= 0f) return;

            int2 p = s.Grid.ToCoord(cell);
            float invSpeed = 1f / sp;

            float tx = float.PositiveInfinity;
            float ty = float.PositiveInfinity;

            if (p.x > 0) tx = math.min(tx, s.T[s.Grid.ToIndex(p.x - 1, p.y)]);
            if (p.x < s.Grid.Width - 1) tx = math.min(tx, s.T[s.Grid.ToIndex(p.x + 1, p.y)]);
            if (p.y > 0) ty = math.min(ty, s.T[s.Grid.ToIndex(p.x, p.y - 1)]);
            if (p.y < s.Grid.Height - 1) ty = math.min(ty, s.T[s.Grid.ToIndex(p.x, p.y + 1)]);

            float tNew;
            if (float.IsPositiveInfinity(tx)) tNew = ty + invSpeed;
            else if (float.IsPositiveInfinity(ty)) tNew = tx + invSpeed;
            else
            {
                float diff = math.abs(tx - ty);
                if (diff < invSpeed)
                    tNew = (tx + ty + math.sqrt(2f * invSpeed * invSpeed - diff * diff)) * 0.5f;
                else
                    tNew = math.min(tx, ty) + invSpeed;
            }

            if (tNew < s.T[cell])
                s.T[cell] = tNew;
        }

        public static void Dispose(ref FastSweepingState s)
        {
            if (s.T.IsCreated) s.T.Dispose();
        }
    }
}
