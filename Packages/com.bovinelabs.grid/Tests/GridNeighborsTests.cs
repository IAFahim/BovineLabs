using BovineLabs.Grid;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public class GridNeighborsTests
{
    [Test]
    public unsafe void Center_4Neighbors()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        var b = new NativeArray<byte>(g.Length, Allocator.Temp);
        var n = new NativeArray<int>(4, Allocator.Temp);
        b.Fill((byte)0);
        Assert.AreEqual(4, GridNeighbors.GetNeighbors4(g, g.ToIndex(2, 2), n, b));
        b.Dispose();
        n.Dispose();
    }

    [Test]
    public unsafe void Corner_2Neighbors()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        var b = new NativeArray<byte>(g.Length, Allocator.Temp);
        var n = new NativeArray<int>(4, Allocator.Temp);
        b.Fill((byte)0);
        Assert.AreEqual(2, GridNeighbors.GetNeighbors4(g, g.ToIndex(0, 0), n, b));
        b.Dispose();
        n.Dispose();
    }

    [Test]
    public unsafe void Edge_3Neighbors()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        var b = new NativeArray<byte>(g.Length, Allocator.Temp);
        var n = new NativeArray<int>(4, Allocator.Temp);
        b.Fill((byte)0);
        Assert.AreEqual(3, GridNeighbors.GetNeighbors4(g, g.ToIndex(0, 2), n, b));
        b.Dispose();
        n.Dispose();
    }

    [Test]
    public unsafe void Blocked_Skipped()
    {
        Assert.IsTrue(Grid2D.TryCreate(3, 3, out var g));
        var b = new NativeArray<byte>(g.Length, Allocator.Temp);
        var n = new NativeArray<int>(4, Allocator.Temp);
        b.Fill((byte)0);
        b[g.ToIndex(2, 1)] = 1;
        Assert.AreEqual(3, GridNeighbors.GetNeighbors4(g, g.ToIndex(1, 1), n, b));
        b.Dispose();
        n.Dispose();
    }

    [Test]
    public unsafe void AllBlocked_0Neighbors()
    {
        Assert.IsTrue(Grid2D.TryCreate(3, 3, out var g));
        var b = new NativeArray<byte>(g.Length, Allocator.Temp);
        var n = new NativeArray<int>(4, Allocator.Temp);
        b.Fill((byte)1);
        b[g.ToIndex(1, 1)] = 0;
        Assert.AreEqual(0, GridNeighbors.GetNeighbors4(g, g.ToIndex(1, 1), n, b));
        b.Dispose();
        n.Dispose();
    }

    [Test]
    public unsafe void Center_8Neighbors()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        var b = new NativeArray<byte>(g.Length, Allocator.Temp);
        var n = new NativeArray<int>(8, Allocator.Temp);
        b.Fill((byte)0);
        Assert.AreEqual(8, GridNeighbors.GetNeighbors8(g, g.ToIndex(2, 2), n, b));
        b.Dispose();
        n.Dispose();
    }

    [Test]
    public unsafe void Corner_3DiagNeighbors()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        var b = new NativeArray<byte>(g.Length, Allocator.Temp);
        var n = new NativeArray<int>(8, Allocator.Temp);
        b.Fill((byte)0);
        Assert.AreEqual(3, GridNeighbors.GetNeighbors8(g, g.ToIndex(0, 0), n, b));
        b.Dispose();
        n.Dispose();
    }

    [Test]
    public unsafe void DiagonalPassable_BothFree()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        var b = new NativeArray<byte>(g.Length, Allocator.Temp);
        b.Fill((byte)0);
        Assert.IsTrue(GridNeighbors.IsDiagonalPassable(g, new int2(2, 2), new int2(1, 1), b));
        b.Dispose();
    }

    [Test]
    public unsafe void DiagonalPassable_OneBlocked()
    {
        Assert.IsTrue(Grid2D.TryCreate(5, 5, out var g));
        var b = new NativeArray<byte>(g.Length, Allocator.Temp);
        b.Fill((byte)0);
        b[g.ToIndex(3, 2)] = 1;
        Assert.IsFalse(GridNeighbors.IsDiagonalPassable(g, new int2(2, 2), new int2(1, 1), b));
        b.Dispose();
    }

    [Test]
    public unsafe void DiagonalPassable_OutOfBounds()
    {
        Assert.IsTrue(Grid2D.TryCreate(3, 3, out var g));
        var b = new NativeArray<byte>(g.Length, Allocator.Temp);
        b.Fill((byte)0);
        Assert.IsFalse(GridNeighbors.IsDiagonalPassable(g, new int2(0, 0), new int2(-1, -1), b));
        b.Dispose();
    }

    [Test]
    public unsafe void _1x1_NoNeighbors()
    {
        Assert.IsTrue(Grid2D.TryCreate(1, 1, out var g));
        var b = new NativeArray<byte>(1, Allocator.Temp);
        var n = new NativeArray<int>(4, Allocator.Temp);
        Assert.AreEqual(0, GridNeighbors.GetNeighbors4(g, 0, n, b));
        b.Dispose();
        n.Dispose();
    }
}