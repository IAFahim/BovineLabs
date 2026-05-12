using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.Cftp
{
    public struct CftpUpdate
    {
        public int Cell;
        public uint RandomBits;
    }

    public struct CftpState
    {
        public Grid2D Grid;
        public NativeArray<byte> Low;
        public NativeArray<byte> High;
        public NativeList<CftpUpdate> Updates;
    }

    public static class CftpApi
    {
        public static CftpState Create(int width, int height, int maxUpdates, Allocator a)
        {
            var g = Grid2D.Create(width, height);
            return new CftpState
            {
                Grid = g,
                Low = new NativeArray<byte>(g.Length, a),
                High = new NativeArray<byte>(g.Length, a),
                Updates = new NativeList<CftpUpdate>(maxUpdates, a),
            };
        }

        public static void InitializeExtremes(ref CftpState s)
        {
            s.Low.Fill((byte)0);   // all-dead (bottom of lattice)
            s.High.Fill((byte)1);  // all-alive (top of lattice)
        }

        public static void GeneratePastUpdates(ref CftpState s, ref Unity.Mathematics.Random rng, int count)
        {
            s.Updates.Clear();
            for (int t = 0; t < count; t++)
            {
                for (int i = 0; i < s.Grid.Length; i++)
                {
                    s.Updates.Add(new CftpUpdate
                    {
                        Cell = i,
                        RandomBits = rng.NextUInt(),
                    });
                }
            }
        }

        /// <summary>
        /// Apply monotone coupling: both chains receive the SAME random bit.
        /// A cell becomes alive if bit=1 and at least 2 neighbors are alive (birth/survival).
        /// Dead stays dead if bit=0 or fewer than 2 alive neighbors.
        /// This preserves the monotone order: if Low[i] <= High[i] before, it holds after.
        /// </summary>
        public static void Replay(ref CftpState s)
        {
            InitializeExtremes(ref s);

            for (int i = 0; i < s.Updates.Length; i++)
            {
                var u = s.Updates[i];
                byte bit = (byte)(u.RandomBits & 1);

                // Count alive neighbors for low and high chains
                int2 p = s.Grid.ToCoord(u.Cell);
                int lowNeighbors = 0, highNeighbors = 0;
                for (int d = 0; d < 4; d++)
                {
                    int2 np = p + Grid2D.Directions4[d];
                    if (!s.Grid.InBounds(np)) continue;
                    int ni = s.Grid.ToIndex(np);
                    lowNeighbors += s.Low[ni];
                    highNeighbors += s.High[ni];
                }

                // Monotone transition: same random bit for both chains
                // Alive if bit=1 AND enough neighbors alive
                // Low chain: uses low neighbor count (fewer alive → harder to become alive)
                s.Low[u.Cell] = (byte)((bit == 1 && lowNeighbors >= 2) ? 1 : 0);
                // High chain: uses high neighbor count (more alive → easier to become alive)
                s.High[u.Cell] = (byte)((bit == 1 && highNeighbors >= 2) ? 1 : 0);

                // Monotonicity preserved: Low[u.Cell] <= High[u.Cell] because lowNeighbors <= highNeighbors
            }
        }

        public static bool Coalesced(ref CftpState s)
        {
            for (int i = 0; i < s.Grid.Length; i++)
            {
                if (s.Low[i] != s.High[i])
                    return false;
            }
            return true;
        }

        public static bool SampleExact(ref CftpState s, ref Unity.Mathematics.Random rng, NativeArray<byte> sample)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                GeneratePastUpdates(ref s, ref rng, 1 << attempt);
                Replay(ref s);
                if (Coalesced(ref s))
                {
                    NativeArray<byte>.Copy(s.Low, sample);
                    return true;
                }
            }
            return false;
        }

        public static void Dispose(ref CftpState s)
        {
            if (s.Low.IsCreated) s.Low.Dispose();
            if (s.High.IsCreated) s.High.Dispose();
            if (s.Updates.IsCreated) s.Updates.Dispose();
        }
    }
}
