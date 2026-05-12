using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Sipp;

public class SippTests
{
    [Test] public void Create_Dimensions()
    { var s = SippApi.Create(5, 5, 100, 100, Allocator.Temp); Assert.AreEqual(25, s.Grid.Length); SippApi.Dispose(ref s); }

    [Test] public void Search_StartEqualsGoal()
    {
        var s = SippApi.Create(5, 5, 100, 100, Allocator.Temp);
        var blocked = new NativeArray<byte>(25, Allocator.Temp); blocked.Fill((byte)0);
        var path = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue(SippApi.Search(ref s, blocked, 12, 12, 0f, ref path));
        Assert.AreEqual(1, path.Length);
        SippApi.Dispose(ref s); blocked.Dispose(); path.Dispose();
    }

    [Test] public void Search_Adjacent()
    {
        var s = SippApi.Create(5, 5, 100, 100, Allocator.Temp);
        var blocked = new NativeArray<byte>(25, Allocator.Temp); blocked.Fill((byte)0);
        var path = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue(SippApi.Search(ref s, blocked, 12, 13, 0f, ref path));
        Assert.Greater(path.Length, 0);
        Assert.AreEqual(12, path[0]);
        Assert.AreEqual(13, path[path.Length - 1]);
        SippApi.Dispose(ref s); blocked.Dispose(); path.Dispose();
    }

    [Test] public void Search_BlockedGoal()
    {
        var s = SippApi.Create(3, 3, 50, 100, Allocator.Temp);
        var blocked = new NativeArray<byte>(9, Allocator.Temp); blocked.Fill((byte)0); blocked[8] = 1;
        var path = new NativeList<int>(Allocator.Temp);
        Assert.IsFalse(SippApi.Search(ref s, blocked, 0, 8, 0f, ref path));
        SippApi.Dispose(ref s); blocked.Dispose(); path.Dispose();
    }

    [Test] public void Dispose_Double() { var s = SippApi.Create(3, 3, 10, 10, Allocator.Temp); SippApi.Dispose(ref s); SippApi.Dispose(ref s); }
}
