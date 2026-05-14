using BovineLabs.Grid.Anya;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

public unsafe partial class AnyaValidationTests
{
    [Test]
    [Category("Validation")]
    [Category("Oracle")]
    public void RandomSmallGrids_MatchVisibilityGraphOracle()
    {
        const int Seeds = 200;
        var rng = new Random(123456);

        for (var seed = 0; seed < Seeds; seed++)
        {
            var w = rng.NextInt(3, 8);
            var h = rng.NextInt(3, 8);

            Assert.IsTrue(AnyaApi.TryCreate(w, h, 20000, Allocator.Temp, out var s));
            var blocked = NewEmptyBlocked(w, h, Allocator.Temp);
            var path = new NativeList<int2>(Allocator.Temp);

            try
            {
                for (var i = 0; i < blocked.Length; i++)
                    blocked[i] = rng.NextDouble() < 0.25 ? (byte)1 : (byte)0;

                var start = new int2(rng.NextInt(0, w), rng.NextInt(0, h));
                var goal = new int2(rng.NextInt(0, w), rng.NextInt(0, h));
                blocked[start.y * w + start.x] = 0;
                blocked[goal.y * w + goal.x] = 0;

                var oracleCost = VisibilityGraphOracleCost(w, h, blocked, start, goal);
                var found = AnyaApi.TrySearch(ref s, blocked, ref start, ref goal, ref path);

                if (double.IsPositiveInfinity(oracleCost))
                {
                    Assert.IsFalse(found, $"Seed {seed}: Anya found a path but oracle says none exists.");
                    Assert.AreEqual(0, path.Length);
                }
                else
                {
                    Assert.IsTrue(found, $"Seed {seed}: Anya failed but oracle found cost {oracleCost}.");
                    AssertPathValidAndVisible(s.Grid, blocked, path, start, goal);

                    var anyaCost = PathCost(path);
                    Assert.AreEqual(
                        oracleCost,
                        anyaCost,
                        1e-4,
                        $"Seed {seed}: Anya cost differs from visibility-graph oracle. Grid {w}x{h}");
                }
            }
            finally
            {
                blocked.Dispose();
                path.Dispose();
                AnyaApi.Dispose(ref s);
            }
        }
    }
}
