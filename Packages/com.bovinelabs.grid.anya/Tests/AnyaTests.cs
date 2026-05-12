using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Anya;

public class AnyaTests
{
    [Test] public void Create_Dimensions()
    { var s = AnyaApi.Create(10, 10, 100, Allocator.Temp); Assert.AreEqual(100, s.Grid.Length); AnyaApi.Dispose(ref s); }

    [Test] public void Search_DirectLine()
    {
        var s = AnyaApi.Create(10, 10, 1000, Allocator.Temp);
        var blocked = new NativeArray<byte>(100, Allocator.Temp); blocked.Fill((byte)0);
        var path = new NativeList<int2>(Allocator.Temp);
        { int2 startV=new int2(0, 0); int2 goalV=new int2(5, 0); Assert.IsTrue(AnyaApi.Search(ref s, blocked, ref startV, ref goalV, ref path)); }
        Assert.AreEqual(new int2(0, 0), path[0]);
        Assert.AreEqual(new int2(5, 0), path[path.Length - 1]);
        AnyaApi.Dispose(ref s); blocked.Dispose(); path.Dispose();
    }

    [Test] public void Search_BlockedGoal()
    {
        var s = AnyaApi.Create(10, 10, 1000, Allocator.Temp);
        var blocked = new NativeArray<byte>(100, Allocator.Temp); blocked.Fill((byte)0); blocked[0] = 1;
        var path = new NativeList<int2>(Allocator.Temp);
        { int2 startV=new int2(0, 0); int2 goalV=new int2(5, 5); Assert.IsFalse(AnyaApi.Search(ref s, blocked, ref startV, ref goalV, ref path)); }
        AnyaApi.Dispose(ref s); blocked.Dispose(); path.Dispose();
    }

    [Test] public void Search_WithWall()
    {
        var s = AnyaApi.Create(10, 10, 2000, Allocator.Temp);
        var blocked = new NativeArray<byte>(100, Allocator.Temp); blocked.Fill((byte)0);
        // Wall with gap
        for (int y = 0; y < 8; y++) blocked[s.Grid.ToIndex(5, y)] = 1;
        var path = new NativeList<int2>(Allocator.Temp);
        { int2 startV=new int2(0, 0); int2 goalV=new int2(9, 9); Assert.IsTrue(AnyaApi.Search(ref s, blocked, ref startV, ref goalV, ref path)); }
        AnyaApi.Dispose(ref s); blocked.Dispose(); path.Dispose();
    }

    [Test] public void Dispose_Double() { var s = AnyaApi.Create(5, 5, 100, Allocator.Temp); AnyaApi.Dispose(ref s); AnyaApi.Dispose(ref s); }
}
