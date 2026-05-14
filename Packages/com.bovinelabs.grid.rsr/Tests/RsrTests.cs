using BovineLabs.Grid.Rsr;
using NUnit.Framework;
using Unity.Collections;

public class RsrTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(RsrApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        Assert.AreEqual(25, s.Grid.Length);
        RsrApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Build_OpenGrid()
    {
        Assert.IsTrue(RsrApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        for (var i = 0; i < blocked.Length; i++) blocked[i] = 0;
        Assert.IsTrue(RsrApi.TryBuild(ref s, in blocked));
        Assert.AreEqual(1, s.Rects.Length);
        Assert.AreEqual(0, s.Rects[0].Min.x);
        Assert.AreEqual(4, s.Rects[0].Max.x);
        RsrApi.Dispose(ref s);
        blocked.Dispose();
    }

    [Test]
    public unsafe void Build_WithWall()
    {
        Assert.IsTrue(RsrApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        for (var i = 0; i < blocked.Length; i++) blocked[i] = 0;
        blocked[s.Grid.ToIndex(2, 2)] = 1;
        Assert.IsTrue(RsrApi.TryBuild(ref s, in blocked));
        Assert.Greater(s.Rects.Length, 1);
        RsrApi.Dispose(ref s);
        blocked.Dispose();
    }

    [Test]
    public unsafe void GetSuccessors_Interior()
    {
        Assert.IsTrue(RsrApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        for (var i = 0; i < blocked.Length; i++) blocked[i] = 0;
        Assert.IsTrue(RsrApi.TryBuild(ref s, in blocked));
        var succ = new NativeList<int>(Allocator.Temp);

        RsrApi.GetSuccessors(ref s, s.Grid.ToIndex(2, 2), in blocked, ref succ);

        Assert.Greater(succ.Length, 0);
        RsrApi.Dispose(ref s);
        blocked.Dispose();
        succ.Dispose();
    }

    [Test]
    public unsafe void GetSuccessors_Perimeter()
    {
        Assert.IsTrue(RsrApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        for (var i = 0; i < blocked.Length; i++) blocked[i] = 0;
        Assert.IsTrue(RsrApi.TryBuild(ref s, in blocked));
        var succ = new NativeList<int>(Allocator.Temp);

        RsrApi.GetSuccessors(ref s, s.Grid.ToIndex(0, 0), in blocked, ref succ);
        Assert.Greater(succ.Length, 0);
        RsrApi.Dispose(ref s);
        blocked.Dispose();
        succ.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(RsrApi.TryCreate(3, 3, 10, Allocator.Temp, out var s));
        RsrApi.Dispose(ref s);
        RsrApi.Dispose(ref s);
    }
}