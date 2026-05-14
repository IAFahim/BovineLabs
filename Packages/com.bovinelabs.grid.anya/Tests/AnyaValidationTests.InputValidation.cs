using BovineLabs.Grid.Anya;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public unsafe partial class AnyaValidationTests
{
    [Test]
    [Category("InputValidation")]
    public void Search_UncreatedBlockedArray_ReturnsFalseAndClearsPath()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = default(NativeArray<byte>);
        var path = new NativeList<int2>(Allocator.Temp);
        path.Add(new int2(99, 99));

        try
        {
            var start = new int2(0, 0);
            var goal = new int2(4, 4);

            Assert.IsFalse(AnyaApi.TrySearch(ref s, blocked, ref start, ref goal, ref path));
            Assert.AreEqual(0, path.Length, "Failed search must not leave stale path data.");
        }
        finally
        {
            path.Dispose();
            AnyaApi.Dispose(ref s);
        }
    }

    [Test]
    [Category("InputValidation")]
    public void Search_ShortBlockedArray_ReturnsFalseAndClearsPath()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(24, Allocator.Temp);
        var path = new NativeList<int2>(Allocator.Temp);
        path.Add(new int2(99, 99));

        try
        {
            var start = new int2(0, 0);
            var goal = new int2(4, 4);

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

    [Test]
    [Category("InputValidation")]
    public void Search_UncreatedOutputPath_ReturnsFalse()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = NewEmptyBlocked(5, 5, Allocator.Temp);
        var path = default(NativeList<int2>);

        try
        {
            var start = new int2(0, 0);
            var goal = new int2(4, 4);

            Assert.IsFalse(AnyaApi.TrySearch(ref s, blocked, ref start, ref goal, ref path));
        }
        finally
        {
            blocked.Dispose();
            AnyaApi.Dispose(ref s);
        }
    }

    [Test]
    [Category("InputValidation")]
    public void Search_OutOfBoundsStart_ReturnsFalseAndClearsPath()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = NewEmptyBlocked(5, 5, Allocator.Temp);
        var path = new NativeList<int2>(Allocator.Temp);
        path.Add(new int2(99, 99));

        try
        {
            var start = new int2(-1, 0);
            var goal = new int2(4, 4);

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

    [Test]
    [Category("InputValidation")]
    public void Search_OutOfBoundsGoal_ReturnsFalseAndClearsPath()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = NewEmptyBlocked(5, 5, Allocator.Temp);
        var path = new NativeList<int2>(Allocator.Temp);
        path.Add(new int2(99, 99));

        try
        {
            var start = new int2(0, 0);
            var goal = new int2(5, 4);

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

    [Test]
    [Category("InputValidation")]
    public void Search_BlockedStart_ReturnsFalseAndEmptyPath()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = NewEmptyBlocked(5, 5, Allocator.Temp);
        var path = new NativeList<int2>(Allocator.Temp);

        try
        {
            var start = new int2(0, 0);
            var goal = new int2(4, 4);
            blocked[s.Grid.ToIndex(start.x, start.y)] = 1;

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

    [Test]
    [Category("InputValidation")]
    public void Search_BlockedGoal_ReturnsFalseAndEmptyPath()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = NewEmptyBlocked(5, 5, Allocator.Temp);
        var path = new NativeList<int2>(Allocator.Temp);

        try
        {
            var start = new int2(0, 0);
            var goal = new int2(4, 4);
            blocked[s.Grid.ToIndex(goal.x, goal.y)] = 1;

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
