using BovineLabs.Grid;
using BovineLabs.Grid.Cpd;
using NUnit.Framework;
using Unity.Collections;

public class CpdTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(CpdApi.TryCreate(5, 5, 1000, Allocator.Temp, out var s));
        Assert.AreEqual(25, s.Grid.Length);
        CpdApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Build_OpenGrid()
    {
        Assert.IsTrue(CpdApi.TryCreate(3, 3, 1000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(9, Allocator.Temp);
        blocked.Fill((byte)0);
        Assert.IsTrue(CpdApi.TryBuild(ref s, in blocked));
        Assert.Greater(s.Runs.Length, 0);
        CpdApi.Dispose(ref s);
        blocked.Dispose();
    }

    [Test]
    public unsafe void TryGetFirstMove()
    {
        Assert.IsTrue(CpdApi.TryCreate(3, 3, 1000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(9, Allocator.Temp);
        blocked.Fill((byte)0);
        Assert.IsTrue(CpdApi.TryBuild(ref s, in blocked));
        Assert.IsTrue(CpdApi.TryGetFirstMove(ref s, 0, 8, out var move));
        Assert.Less(move, 4);
        CpdApi.Dispose(ref s);
        blocked.Dispose();
    }

    [Test]
    public unsafe void ExtractPath()
    {
        Assert.IsTrue(CpdApi.TryCreate(3, 3, 1000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(9, Allocator.Temp);
        blocked.Fill((byte)0);
        Assert.IsTrue(CpdApi.TryBuild(ref s, in blocked));
        var path = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue(CpdApi.TryExtractPath(ref s, 0, 8, ref path));
        Assert.Greater(path.Length, 0);
        Assert.AreEqual(0, path[0]);
        Assert.AreEqual(8, path[path.Length - 1]);
        CpdApi.Dispose(ref s);
        blocked.Dispose();
        path.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(CpdApi.TryCreate(3, 3, 10, Allocator.Temp, out var s));
        CpdApi.Dispose(ref s);
        CpdApi.Dispose(ref s);
    }
}