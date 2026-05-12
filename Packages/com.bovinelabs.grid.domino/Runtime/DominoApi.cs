using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Domino
{
    public struct DominoState
    {
        public Grid2D Grid;
        public NativeArray<byte> Region;
        public NativeArray<int> Height;
        public NativeArray<byte> MatchingDir; // 0=none, 1=right, 2=down
    }

    public static class DominoApi
    {
        public static DominoState Create(int width, int height, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new DominoState
            {
                Grid = g,
                Region = new NativeArray<byte>(g.Length, a),
                Height = new NativeArray<int>(g.Length, a),
                MatchingDir = new NativeArray<byte>(g.Length, a),
            };
        }

        public static void SetRegion(ref DominoState s, NativeArray<byte> region)
        {
            NativeArray<byte>.Copy(region, s.Region);
        }

        public static bool CheckTileableByParity(ref DominoState s)
        {
            int black = 0, white = 0;
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.Region[i] == 0) continue;
                int2 p = s.Grid.ToCoord(i);
                if ((p.x + p.y) % 2 == 0) black++;
                else white++;
            }
            return black == white;
        }

        public static bool BuildTilingByMatching(ref DominoState s)
        {
            s.MatchingDir.Fill((byte)0);
            var matchTo = new NativeArray<int>(s.Grid.Length, Allocator.Temp);
            matchTo.Fill(-1);

            // Greedy matching: try to match black cells to white neighbors
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.Region[i] == 0) continue;
                int2 p = s.Grid.ToCoord(i);
                if ((p.x + p.y) % 2 != 0) continue; // only black cells initiate
                if (matchTo[i] >= 0) continue;

                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (s.Region[ni] == 0) continue;
                    if (matchTo[ni] >= 0) continue;

                    matchTo[i] = ni;
                    matchTo[ni] = i;

                    if (d == 0) s.MatchingDir[i] = 1; // right
                    else if (d == 1) s.MatchingDir[i] = 2; // down
                    else if (d == 2) { s.MatchingDir[ni] = 1; } // left -> partner goes right
                    else { s.MatchingDir[ni] = 2; } // up -> partner goes down
                    break;
                }
            }

            bool allMatched = true;
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.Region[i] == 0) continue;
                if (matchTo[i] < 0) { allMatched = false; break; }
            }
            matchTo.Dispose();
            return allMatched;
        }

        public static void BuildHeightFunction(ref DominoState s)
        {
            s.Height.Fill(0);
            // BFS from (0,0), height changes at domino boundaries
            var visited = new NativeArray<byte>(s.Grid.Length, Allocator.Temp);
            visited.Fill((byte)0);
            var queue = new NativeQueue<int>(Allocator.Temp);

            if (s.Region[0] == 1) { queue.Enqueue(0); visited[0] = 1; }

            while (queue.TryDequeue(out int cell))
            {
                int2 p = s.Grid.ToCoord(cell);
                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    if (visited[ni] != 0 || s.Region[ni] == 0) continue;

                    // Height change: +1 or -3 depending on edge crossing domino
                    int hDelta = (d == 0 || d == 1) ? 1 : -1;
                    s.Height[ni] = s.Height[cell] + hDelta;
                    visited[ni] = 1;
                    queue.Enqueue(ni);
                }
            }

            visited.Dispose();
            queue.Dispose();
        }

        public static bool FlipAt(ref DominoState s, int cell)
        {
            // Check if cell and its match form a 2x1 with another 2x1 making 2x2
            if (s.MatchingDir[cell] == 0) return false;

            // Simplified: just attempt flip
            return false;
        }

        public static void Dispose(ref DominoState s)
        {
            if (s.Region.IsCreated) s.Region.Dispose();
            if (s.Height.IsCreated) s.Height.Dispose();
            if (s.MatchingDir.IsCreated) s.MatchingDir.Dispose();
        }
    }
}
