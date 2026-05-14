using BovineLabs.Grid.Anya;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public unsafe partial class AnyaValidationTests
{
    [Test]
    [Category("Correctness")]
    public void Search_StartEqualsGoal_ReturnsSinglePoint()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = NewEmptyBlocked(5, 5, Allocator.Temp);
        var path = new NativeList<int2>(Allocator.Temp);

        try
        {
            var start = new int2(2, 3);
            var goal = new int2(2, 3);

            Assert.IsTrue(AnyaApi.TrySearch(ref s, blocked, ref start, ref goal, ref path));
            Assert.AreEqual(1, path.Length);
            Assert.AreEqual(start, path[0]);
        }
        finally
        {
            blocked.Dispose();
            path.Dispose();
            AnyaApi.Dispose(ref s);
        }
    }

    [Test]
    [Category("Correctness")]
    public void Search_DirectLine_ReturnsTwoPointEuclideanPath()
    {
        Assert.IsTrue(AnyaApi.TryCreate(10, 10, 100, Allocator.Temp, out var s));
        var blocked = NewEmptyBlocked(10, 10, Allocator.Temp);
        var path = new NativeList<int2>(Allocator.Temp);

        try
        {
            var start = new int2(0, 0);
            var goal = new int2(9, 5);

            Assert.IsTrue(AnyaApi.TrySearch(ref s, blocked, ref start, ref goal, ref path));

            AssertPathValidAndVisible(s.Grid, blocked, path, start, goal);
            Assert.AreEqual(2, path.Length, "Open-grid direct line should not add unnecessary waypoints.");

            var expected = math.distance(new double2(start.x, start.y), new double2(goal.x, goal.y));
            Assert.AreEqual(expected, PathCost(path), CostTolerance);
        }
        finally
        {
            blocked.Dispose();
            path.Dispose();
            AnyaApi.Dispose(ref s);
        }
    }

    [Test]
    [Category("Correctness")]
    public void Search_ReusedOutputPath_DoesNotContainOldResults()
    {
        Assert.IsTrue(AnyaApi.TryCreate(8, 8, 1000, Allocator.Temp, out var s));
        var blocked = NewEmptyBlocked(8, 8, Allocator.Temp);
        var path = new NativeList<int2>(Allocator.Temp);

        try
        {
            var startA = new int2(0, 0);
            var goalA = new int2(7, 0);
            Assert.IsTrue(AnyaApi.TrySearch(ref s, blocked, ref startA, ref goalA, ref path));
            Assert.Greater(path.Length, 0);

            var startB = new int2(0, 7);
            var goalB = new int2(7, 7);
            Assert.IsTrue(AnyaApi.TrySearch(ref s, blocked, ref startB, ref goalB, ref path));

            Assert.AreEqual(startB, path[0]);
            Assert.AreEqual(goalB, path[path.Length - 1]);

            for (var i = 0; i < path.Length; i++)
                Assert.AreNotEqual(goalA, path[i], "Second path contains stale waypoint from first path.");
        }
        finally
        {
            blocked.Dispose();
            path.Dispose();
            AnyaApi.Dispose(ref s);
        }
    }

    [Test]
    [Category("Correctness")]
    public void Search_WithObstacle_ReturnsVisibleUnblockedPath()
    {
        Assert.IsTrue(AnyaApi.TryCreate(10, 10, 5000, Allocator.Temp, out var s));
        var blocked = NewEmptyBlocked(10, 10, Allocator.Temp);
        var path = new NativeList<int2>(Allocator.Temp);

        try
        {
            for (var y = 0; y < 10; y++)
                if (y != 5)
                    blocked[s.Grid.ToIndex(5, y)] = 1;

            var start = new int2(1, 1);
            var goal = new int2(8, 8);

            Assert.IsTrue(AnyaApi.TrySearch(ref s, blocked, ref start, ref goal, ref path));
            AssertPathValidAndVisible(s.Grid, blocked, path, start, goal);
        }
        finally
        {
            blocked.Dispose();
            path.Dispose();
            AnyaApi.Dispose(ref s);
        }
    }

    [Test]
    [Category("Correctness")]
    public void Search_NoPossiblePath_ReturnsFalseAndEmptyPath()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 500, Allocator.Temp, out var s));
        var blocked = NewEmptyBlocked(5, 5, Allocator.Temp);
        var path = new NativeList<int2>(Allocator.Temp);

        try
        {
            for (var y = 0; y < 5; y++)
                blocked[s.Grid.ToIndex(2, y)] = 1;

            var start = new int2(0, 2);
            var goal = new int2(4, 2);

            Assert.IsFalse(AnyaApi.TrySearch(ref s, blocked, ref start, ref goal, ref path));
            Assert.AreEqual(0, path.Length);
        }
        finally
        {
            blocked.Dispose();
            path.Dispose();
            AnyaApi.Dispose(ref s);
        }
    }
}
