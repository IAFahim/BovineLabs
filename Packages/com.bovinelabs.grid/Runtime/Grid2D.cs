using Unity.Mathematics;

namespace BovineLabs.Grid
{
    public struct Grid2D
    {
        public int Width;
        public int Height;
        public int Length;

        public void Setup(int width, int height)
        {
            Width = width;
            Height = height;
            Length = width * height;
        }

        public static Grid2D Create(int width, int height)
        {
            var g = new Grid2D();
            g.Setup(width, height);
            return g;
        }

        public int ToIndex(int2 p)
        {
            return p.y * Width + p.x;
        }

        public int ToIndex(int x, int y)
        {
            return y * Width + x;
        }

        public int2 ToCoord(int index)
        {
            return new int2(index % Width, index / Width);
        }

        public bool InBounds(int2 p)
        {
            return (uint)p.x < (uint)Width && (uint)p.y < (uint)Height;
        }

        public bool InBounds(int index)
        {
            return (uint)index < (uint)Length;
        }

        public bool TryIndex(int2 p, out int index)
        {
            index = p.y * Width + p.x;
            return (uint)p.x < (uint)Width && (uint)p.y < (uint)Height;
        }

        // Burst-compatible direction constants — use inlined instead of managed arrays.
        // Right, Down, Left, Up
        public const int Dir4Count = 4;
        // E, SE, S, SW, W, NW, N, NE
        public const int Dir8Count = 8;

        /// <summary>4-connected neighbor offsets (right, down, left, up). Burst-compatible via fixed struct fields.</summary>
        public static int2 Dir4(int d)
        {
            switch (d)
            {
                case 0: return new int2(1, 0);
                case 1: return new int2(0, 1);
                case 2: return new int2(-1, 0);
                default: return new int2(0, -1);
            }
        }

        /// <summary>8-connected neighbor offsets. Burst-compatible via fixed struct fields.</summary>
        public static int2 Dir8(int d)
        {
            switch (d)
            {
                case 0: return new int2(1, 0);
                case 1: return new int2(1, 1);
                case 2: return new int2(0, 1);
                case 3: return new int2(-1, 1);
                case 4: return new int2(-1, 0);
                case 5: return new int2(-1, -1);
                case 6: return new int2(0, -1);
                default: return new int2(1, -1);
            }
        }

        /// <summary>Manhattan distance between two cells.</summary>
        public static float HeuristicManhattan(int2 a, int2 b)
        {
            return math.abs(a.x - b.x) + math.abs(a.y - b.y);
        }

        /// <summary>Euclidean distance between two cells.</summary>
        public static float HeuristicEuclidean(int2 a, int2 b)
        {
            return math.length(new float2(a.x - b.x, a.y - b.y));
        }

        /// <summary>Octile distance (8-dir grid heuristic).</summary>
        public static float HeuristicOctile(int2 a, int2 b)
        {
            int dx = math.abs(a.x - b.x);
            int dy = math.abs(a.y - b.y);
            return dx + dy + (1.4142135f - 2f) * math.min(dx, dy);
        }
    }
}
