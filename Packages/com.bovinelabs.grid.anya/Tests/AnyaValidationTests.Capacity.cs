using BovineLabs.Grid.Anya;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public unsafe partial class AnyaValidationTests
{
    [Test]
    [Category("Capacity")]
    public void Search_CapacityTooSmall_ReturnsFalseWithoutStalePath()
    {
        Assert.IsTrue(AnyaApi.TryCreate(20, 20, 1, Allocator.Temp, out var s));
        var blocked = NewEmptyBlocked(20, 20, Allocator.Temp);
        var path = new NativeList<int2>(Allocator.Temp);
        path.Add(new int2(99, 99));

        try
        {
            for (var y = 0; y < 20; y++)
                if (y != 10)
                    blocked[10 + y * 20] = 1;

            var start = new int2(1, 1);
            var goal = new int2(18, 18);

            Assert.IsFalse(AnyaApi.TrySearch(ref s, blocked, ref start, ref goal, ref path));
            Assert.AreEqual(0, path.Length, "Capacity failure must not return partial/stale path.");
        }
        finally
        {
            blocked.Dispose();
            path.Dispose();
            AnyaApi.Dispose(ref s);
        }
    }
}
