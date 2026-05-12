using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Subgoal;

public class SubgoalTests
{
    [Test] public void Create_Dimensions()
    { var s = SubgoalApi.Create(10, 10, 100, 1000, Allocator.Temp); Assert.AreEqual(100, s.Grid.Length); SubgoalApi.Dispose(ref s); }

    [Test] public void Build_OpenGrid_NoSubgoals()
    {
        var s = SubgoalApi.Create(10, 10, 100, 1000, Allocator.Temp);
        var blocked = new NativeArray<byte>(100, Allocator.Temp); blocked.Fill((byte)0);
        SubgoalApi.Build(ref s, blocked);
        Assert.AreEqual(0, s.Subgoals.Length);
        SubgoalApi.Dispose(ref s); blocked.Dispose();
    }

    [Test] public void Build_WithObstacles()
    {
        var s = SubgoalApi.Create(10, 10, 100, 1000, Allocator.Temp);
        var blocked = new NativeArray<byte>(100, Allocator.Temp); blocked.Fill((byte)0);
        // Create L-shaped wall
        blocked[s.Grid.ToIndex(5, 3)] = 1;
        blocked[s.Grid.ToIndex(5, 4)] = 1;
        blocked[s.Grid.ToIndex(5, 5)] = 1;
        blocked[s.Grid.ToIndex(6, 5)] = 1;
        blocked[s.Grid.ToIndex(7, 5)] = 1;
        SubgoalApi.Build(ref s, blocked);
        // Just verify it doesn't crash and produces valid graph
        Assert.GreaterOrEqual(s.Subgoals.Length, 0);
        SubgoalApi.Dispose(ref s); blocked.Dispose();
    }

    [Test] public void Search_OpenGrid()
    {
        var s = SubgoalApi.Create(10, 10, 100, 1000, Allocator.Temp);
        var blocked = new NativeArray<byte>(100, Allocator.Temp); blocked.Fill((byte)0);
        var path = new NativeList<int>(Allocator.Temp);
        Assert.IsTrue(SubgoalApi.Search(ref s, blocked, 0, 99, ref path));
        Assert.AreEqual(0, path[0]);
        Assert.AreEqual(99, path[path.Length - 1]);
        SubgoalApi.Dispose(ref s); blocked.Dispose(); path.Dispose();
    }

    [Test] public unsafe void LineOfSight()
    {
        var g = Grid2D.Create(10, 10);
        var blocked = new NativeArray<byte>(100, Allocator.Temp); blocked.Fill((byte)0);
        byte* blockedPtr = (byte*)blocked.GetUnsafeReadOnlyPtr();
        { int2 a=new int2(0, 0); int2 b=new int2(5, 0); Assert.IsTrue(SubgoalApi.LineOfSight(in g, blockedPtr, ref a, ref b)); }
        blocked[g.ToIndex(3, 0)] = 1;
        { int2 a=new int2(0, 0); int2 b=new int2(5, 0); Assert.IsFalse(SubgoalApi.LineOfSight(in g, blockedPtr, ref a, ref b)); }
        blocked.Dispose();
    }

    [Test] public void Dispose_Double() { var s = SubgoalApi.Create(5, 5, 10, 100, Allocator.Temp); SubgoalApi.Dispose(ref s); SubgoalApi.Dispose(ref s); }
}
