using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;

public class GridNeighborsTests
{
    [Test] public void Center_4Neighbors()
    {
        var g = Grid2D.Create(5, 5); var b = new NativeArray<byte>(g.Length, Allocator.Temp); var n = new NativeArray<int>(4, Allocator.Temp);
        b.Fill((byte)0);
        Assert.AreEqual(4, GridNeighbors.GetNeighbors4(g, g.ToIndex(2, 2), n, b));
        b.Dispose(); n.Dispose();
    }

    [Test] public void Corner_2Neighbors()
    {
        var g = Grid2D.Create(5, 5); var b = new NativeArray<byte>(g.Length, Allocator.Temp); var n = new NativeArray<int>(4, Allocator.Temp);
        b.Fill((byte)0);
        Assert.AreEqual(2, GridNeighbors.GetNeighbors4(g, g.ToIndex(0, 0), n, b));
        b.Dispose(); n.Dispose();
    }

    [Test] public void Edge_3Neighbors()
    {
        var g = Grid2D.Create(5, 5); var b = new NativeArray<byte>(g.Length, Allocator.Temp); var n = new NativeArray<int>(4, Allocator.Temp);
        b.Fill((byte)0);
        Assert.AreEqual(3, GridNeighbors.GetNeighbors4(g, g.ToIndex(0, 2), n, b));
        b.Dispose(); n.Dispose();
    }

    [Test] public void Blocked_Skipped()
    {
        var g = Grid2D.Create(3, 3); var b = new NativeArray<byte>(g.Length, Allocator.Temp); var n = new NativeArray<int>(4, Allocator.Temp);
        b.Fill((byte)0); b[g.ToIndex(2, 1)] = 1;
        Assert.AreEqual(3, GridNeighbors.GetNeighbors4(g, g.ToIndex(1, 1), n, b));
        b.Dispose(); n.Dispose();
    }

    [Test] public void AllBlocked_0Neighbors()
    {
        var g = Grid2D.Create(3, 3); var b = new NativeArray<byte>(g.Length, Allocator.Temp); var n = new NativeArray<int>(4, Allocator.Temp);
        b.Fill((byte)1); b[g.ToIndex(1, 1)] = 0;
        Assert.AreEqual(0, GridNeighbors.GetNeighbors4(g, g.ToIndex(1, 1), n, b));
        b.Dispose(); n.Dispose();
    }

    [Test] public void Center_8Neighbors()
    {
        var g = Grid2D.Create(5, 5); var b = new NativeArray<byte>(g.Length, Allocator.Temp); var n = new NativeArray<int>(8, Allocator.Temp);
        b.Fill((byte)0);
        Assert.AreEqual(8, GridNeighbors.GetNeighbors8(g, g.ToIndex(2, 2), n, b));
        b.Dispose(); n.Dispose();
    }

    [Test] public void Corner_3DiagNeighbors()
    {
        var g = Grid2D.Create(5, 5); var b = new NativeArray<byte>(g.Length, Allocator.Temp); var n = new NativeArray<int>(8, Allocator.Temp);
        b.Fill((byte)0);
        Assert.AreEqual(3, GridNeighbors.GetNeighbors8(g, g.ToIndex(0, 0), n, b));
        b.Dispose(); n.Dispose();
    }

    [Test] public void DiagonalPassable_BothFree()
    {
        var g = Grid2D.Create(5, 5); var b = new NativeArray<byte>(g.Length, Allocator.Temp); b.Fill((byte)0);
        Assert.IsTrue(GridNeighbors.IsDiagonalPassable(g, new int2(2, 2), new int2(1, 1), b));
        b.Dispose();
    }

    [Test] public void DiagonalPassable_OneBlocked()
    {
        var g = Grid2D.Create(5, 5); var b = new NativeArray<byte>(g.Length, Allocator.Temp); b.Fill((byte)0); b[g.ToIndex(3, 2)] = 1;
        Assert.IsFalse(GridNeighbors.IsDiagonalPassable(g, new int2(2, 2), new int2(1, 1), b));
        b.Dispose();
    }

    [Test] public void DiagonalPassable_OutOfBounds()
    {
        var g = Grid2D.Create(3, 3); var b = new NativeArray<byte>(g.Length, Allocator.Temp); b.Fill((byte)0);
        Assert.IsFalse(GridNeighbors.IsDiagonalPassable(g, new int2(0, 0), new int2(-1, -1), b));
        b.Dispose();
    }

    [Test] public void _1x1_NoNeighbors()
    {
        var g = Grid2D.Create(1, 1); var b = new NativeArray<byte>(1, Allocator.Temp); var n = new NativeArray<int>(4, Allocator.Temp);
        Assert.AreEqual(0, GridNeighbors.GetNeighbors4(g, 0, n, b));
        b.Dispose(); n.Dispose();
    }
}
