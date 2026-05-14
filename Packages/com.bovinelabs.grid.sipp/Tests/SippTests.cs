using BovineLabs.Grid;
using BovineLabs.Grid.Sipp;
using NUnit.Framework;
using Unity.Collections;

public class SippTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(SippApi.TryCreate(5, 5, 100, 100, Allocator.Temp, out var s));
        Assert.AreEqual(25, s.Grid.Length);
        SippApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Search_StartEqualsGoal()
    {
        Assert.IsTrue(SippApi.TryCreate(5, 5, 100, 100, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        blocked.Fill((byte)0);
        var path = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue(SippApi.TrySearch(ref s, blocked, 12, 12, 0f, ref path));
        Assert.AreEqual(1, path.Length);
        SippApi.Dispose(ref s);
        blocked.Dispose();
        path.Dispose();
    }

    [Test]
    public unsafe void Search_Adjacent()
    {
        Assert.IsTrue(SippApi.TryCreate(5, 5, 100, 100, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        blocked.Fill((byte)0);
        var path = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue(SippApi.TrySearch(ref s, blocked, 12, 13, 0f, ref path));
        Assert.Greater(path.Length, 0);
        Assert.AreEqual(12, path[0]);
        Assert.AreEqual(13, path[path.Length - 1]);
        SippApi.Dispose(ref s);
        blocked.Dispose();
        path.Dispose();
    }

    [Test]
    public unsafe void Search_BlockedGoal()
    {
        Assert.IsTrue(SippApi.TryCreate(3, 3, 50, 100, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(9, Allocator.Temp);
        blocked.Fill((byte)0);
        blocked[8] = 1;
        var path = new NativeList<int>(Allocator.Temp);
        Assert.IsFalse(SippApi.TrySearch(ref s, blocked, 0, 8, 0f, ref path));
        SippApi.Dispose(ref s);
        blocked.Dispose();
        path.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(SippApi.TryCreate(3, 3, 10, 10, Allocator.Temp, out var s));
        SippApi.Dispose(ref s);
        SippApi.Dispose(ref s);
    }
}