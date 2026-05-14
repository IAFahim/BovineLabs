using BovineLabs.Grid;
using BovineLabs.Grid.Anya;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public class AnyaTests
{
    [Test]
    public unsafe void Create_Dimensions()
    {
        Assert.IsTrue(AnyaApi.TryCreate(10, 10, 100, Allocator.Temp, out var s));
        Assert.AreEqual(100, s.Grid.Length);
        AnyaApi.Dispose(ref s);
    }

    [Test]
    public unsafe void Search_DirectLine()
    {
        Assert.IsTrue(AnyaApi.TryCreate(10, 10, 1000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(100, Allocator.Temp);
        blocked.Fill((byte)0);
        var path = new NativeList<int2>(Allocator.Temp);
        {
            var startV = new int2(0, 0);
            var goalV = new int2(5, 0);
            Assert.IsTrue(AnyaApi.TrySearch(ref s, blocked, ref startV, ref goalV, ref path));
        }
        Assert.AreEqual(new int2(0, 0), path[0]);
        Assert.AreEqual(new int2(5, 0), path[path.Length - 1]);
        AnyaApi.Dispose(ref s);
        blocked.Dispose();
        path.Dispose();
    }

    [Test]
    public unsafe void Search_BlockedGoal()
    {
        Assert.IsTrue(AnyaApi.TryCreate(10, 10, 1000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(100, Allocator.Temp);
        blocked.Fill((byte)0);
        blocked[0] = 1;
        var path = new NativeList<int2>(Allocator.Temp);
        {
            var startV = new int2(0, 0);
            var goalV = new int2(5, 5);
            Assert.IsFalse(AnyaApi.TrySearch(ref s, blocked, ref startV, ref goalV, ref path));
        }
        AnyaApi.Dispose(ref s);
        blocked.Dispose();
        path.Dispose();
    }

    [Test]
    public unsafe void Search_WithWall()
    {
        Assert.IsTrue(AnyaApi.TryCreate(10, 10, 10000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(100, Allocator.Temp);
        blocked.Fill((byte)0);
        blocked[s.Grid.ToIndex(5, 0)] = 1;
        var path = new NativeList<int2>(Allocator.Temp);
        var startV = new int2(0, 0);
        var goalV = new int2(9, 0);
        Assert.IsTrue(AnyaApi.TrySearch(ref s, blocked, ref startV, ref goalV, ref path));
        Assert.AreEqual(startV, path[0]);
        Assert.AreEqual(goalV, path[path.Length - 1]);
        AnyaApi.Dispose(ref s);
        blocked.Dispose();
        path.Dispose();
    }

    [Test]
    public unsafe void Search_EuclideanCost_OpenGrid()
    {
        Assert.IsTrue(AnyaApi.TryCreate(10, 10, 4000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(100, Allocator.Temp);
        blocked.Fill((byte)0);
        var path = new NativeList<int2>(Allocator.Temp);
        var startV = new int2(0, 0);
        var goalV = new int2(9, 5);
        Assert.IsTrue(AnyaApi.TrySearch(ref s, blocked, ref startV, ref goalV, ref path));
        Assert.GreaterOrEqual(path.Length, 2);
        Assert.AreEqual(startV, path[0]);
        Assert.AreEqual(goalV, path[path.Length - 1]);
        double cost = 0;
        for (var i = 0; i < path.Length - 1; i++)
            cost += math.distance(new double2(path[i].x, path[i].y), new double2(path[i + 1].x, path[i + 1].y));
        var optimal = math.distance(new double2(0, 0), new double2(9, 5));
        Assert.AreEqual(optimal, cost, 0.01, "Anya path cost must match Euclidean distance in open grid");
        AnyaApi.Dispose(ref s);
        blocked.Dispose();
        path.Dispose();
    }

    [Test]
    public unsafe void Search_CornerHugging()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 4000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        blocked.Fill((byte)0);
        blocked[s.Grid.ToIndex(2, 2)] = 1;
        var path = new NativeList<int2>(Allocator.Temp);
        var startV = new int2(0, 0);
        var goalV = new int2(4, 0);
        Assert.IsTrue(AnyaApi.TrySearch(ref s, blocked, ref startV, ref goalV, ref path));
        Assert.AreEqual(startV, path[0]);
        Assert.AreEqual(goalV, path[path.Length - 1]);
        AnyaApi.Dispose(ref s);
        blocked.Dispose();
        path.Dispose();
    }

    [Test]
    public unsafe void Search_FullyBlocked_NoPath()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 1000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        blocked.Fill((byte)1);
        var path = new NativeList<int2>(Allocator.Temp);
        int2 startV = new int2(0, 0);
        int2 goalV = new int2(4, 4);
        Assert.IsFalse(AnyaApi.TrySearch(ref s, blocked, ref startV, ref goalV, ref path));
        AnyaApi.Dispose(ref s); blocked.Dispose(); path.Dispose();
    }

    [Test]
    public unsafe void Search_OutOfBounds_NoPath()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 1000, Allocator.Temp, out var s));
        var blocked = new NativeArray<byte>(25, Allocator.Temp);
        blocked.Fill((byte)0);
        var path = new NativeList<int2>(Allocator.Temp);
        int2 startV = new int2(-1, 0);
        int2 goalV = new int2(4, 4);
        Assert.IsFalse(AnyaApi.TrySearch(ref s, blocked, ref startV, ref goalV, ref path));
        AnyaApi.Dispose(ref s); blocked.Dispose(); path.Dispose();
    }

    [Test]
    public unsafe void Dispose_Double()
    {
        Assert.IsTrue(AnyaApi.TryCreate(5, 5, 100, Allocator.Temp, out var s));
        AnyaApi.Dispose(ref s);
        AnyaApi.Dispose(ref s);
    }
}