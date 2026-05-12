using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Cpd;

public class CpdTests
{
    [Test] public void Create_Dimensions()
    { var s = CpdApi.Create(5, 5, 1000, Allocator.Temp); Assert.AreEqual(25, s.Grid.Length); CpdApi.Dispose(ref s); }

    [Test] public void Build_OpenGrid()
    {
        var s = CpdApi.Create(3, 3, 1000, Allocator.Temp);
        var blocked = new NativeArray<byte>(9, Allocator.Temp); blocked.Fill((byte)0);
        CpdApi.Build(ref s, blocked);
        Assert.Greater(s.Runs.Length, 0);
        CpdApi.Dispose(ref s); blocked.Dispose();
    }

    [Test] public void TryGetFirstMove()
    {
        var s = CpdApi.Create(3, 3, 1000, Allocator.Temp);
        var blocked = new NativeArray<byte>(9, Allocator.Temp); blocked.Fill((byte)0);
        CpdApi.Build(ref s, blocked);
        Assert.IsTrue(CpdApi.TryGetFirstMove(ref s, 0, 8, out byte move));
        Assert.Less(move, 4);
        CpdApi.Dispose(ref s); blocked.Dispose();
    }

    [Test] public void ExtractPath()
    {
        var s = CpdApi.Create(3, 3, 1000, Allocator.Temp);
        var blocked = new NativeArray<byte>(9, Allocator.Temp); blocked.Fill((byte)0);
        CpdApi.Build(ref s, blocked);
        var path = new NativeList<int>(Allocator.Temp);
        CpdApi.ExtractPath(ref s, 0, 8, path);
        Assert.Greater(path.Length, 0);
        Assert.AreEqual(0, path[0]);
        Assert.AreEqual(8, path[path.Length - 1]);
        CpdApi.Dispose(ref s); blocked.Dispose(); path.Dispose();
    }

    [Test] public void Dispose_Double() { var s = CpdApi.Create(3, 3, 10, Allocator.Temp); CpdApi.Dispose(ref s); CpdApi.Dispose(ref s); }
}
